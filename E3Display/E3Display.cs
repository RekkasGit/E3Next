using MonoCore;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace E3Display
{
    public static class MyCode
    {
        private static bool _isInit = false;
        private static readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private const int MaxMessages = 20;
        public static volatile bool _uiInitDone = false;
        public static string ConnectionStatus = "Not connected";
        private static DealerSocket _dealer;
        private static string _routerAddress = string.Empty; // e.g., tcp://127.0.0.1:51712
        private static char[] _scratch = new char[512];

        public static void RegisterCommandBindings()
        {
            try
            {
                // Ensure MQ knows these commands (names without leading slash)
                Core.mq_AddCommand("e3display");
                Core.mq_AddCommand("e3display-connect");
                Core.mq_AddCommand("e3display-disconnect");
                Core.mq_AddCommand("e3display-status");
                Core.mq_AddCommand("e3display-autoconnect");

                // Register handlers (match unslashed command names)
                EventProcessor.RegisterCommand("e3display", (x) =>
                {
                    if (x.args.Count == 0)
                    {
                        Core.imgui_Begin_OpenFlagSet("E3Display", true);
                        Core.mq_Echo("E3Display: window opened.");
                        return;
                    }
                    var cmd = x.args[0];
                    if (string.Equals(cmd, "connect", StringComparison.OrdinalIgnoreCase) && x.args.Count >= 2)
                    {
                        var portStr = x.args[1];
                        int port;
                        if (int.TryParse(portStr, out port))
                        {
                            ConnectDealer("127.0.0.1", port);
                        }
                        else
                        {
                            Core.mq_Echo("E3Display: invalid port");
                        }
                    }
                    else if (string.Equals(cmd, "disconnect", StringComparison.OrdinalIgnoreCase))
                    {
                        DisconnectDealer();
                    }
                    else if (string.Equals(cmd, "status", StringComparison.OrdinalIgnoreCase))
                    {
                        Core.mq_Echo("E3Display: " + ConnectionStatus);
                    }
                    else
                    {
                        Core.mq_Echo("Usage: /e3display [connect <port>|disconnect|status]");
                    }
                }, "Open window or manage Dealer connection");

                // Direct command aliases
                EventProcessor.RegisterCommand("e3display-connect", (x) =>
                {
                    if (x.args.Count >= 1 && int.TryParse(x.args[0], out var port))
                    {
                        ConnectDealer("127.0.0.1", port);
                    }
                    else Core.mq_Echo("Usage: /e3display-connect <port>");
                });
                EventProcessor.RegisterCommand("e3display-disconnect", (x) =>
                {
                    DisconnectDealer();
                });
                EventProcessor.RegisterCommand("e3display-status", (x) =>
                {
                    Core.mq_Echo("E3Display: " + ConnectionStatus);
                });

                EventProcessor.RegisterCommand("e3display-autoconnect", (x) =>
                {
                    try
                    {
                        var cfgPath = Core.mq_ParseTLO("${MacroQuest.Path[config]}");
                        var macroPath = Core.mq_ParseTLO("${MacroQuest.Path[macros]}");
                        var me = Core.mq_ParseTLO("${Me.CleanName}");
                        if ((string.IsNullOrWhiteSpace(cfgPath) && string.IsNullOrWhiteSpace(macroPath)) || string.IsNullOrWhiteSpace(me) || string.Equals(me, "NULL", StringComparison.OrdinalIgnoreCase))
                        {
                            Core.mq_Echo("E3Display: not in game or MQ paths unavailable");
                            return;
                        }
                        
                        // Check both config and macro paths for the pubsubport file
                        string[] files = Array.Empty<string>();
                        string sharedDir = string.Empty;
                        
                        // First try config path
                        if (!string.IsNullOrWhiteSpace(cfgPath))
                        {
                            sharedDir = System.IO.Path.Combine(cfgPath, "e3 Macro Inis", "SharedData");
                            files = System.IO.Directory.Exists(sharedDir)
                                ? System.IO.Directory.GetFiles(sharedDir, me + "_*_pubsubport.txt")
                                : Array.Empty<string>();
                        }
                        
                        // If not found in config, try macro path
                        if (files.Length == 0 && !string.IsNullOrWhiteSpace(macroPath))
                        {
                            sharedDir = System.IO.Path.Combine(macroPath, "e3 Macro Inis", "SharedData");
                            files = System.IO.Directory.Exists(sharedDir)
                                ? System.IO.Directory.GetFiles(sharedDir, me + "_*_pubsubport.txt")
                                : Array.Empty<string>();
                        }
                        
                        if (files.Length == 0)
                        {
                            Core.mq_Echo("E3Display: pubsub port file not found");
                            return;
                        }
                        var text = System.IO.File.ReadAllText(files[0]).Trim();
                        var parts = text.Split(',');
                        if (parts.Length == 0)
                        {
                            Core.mq_Echo("E3Display: invalid pubsubport file");
                            return;
                        }
                        if (!int.TryParse(parts[0], out var pubPort))
                        {
                            Core.mq_Echo("E3Display: invalid pub port");
                            return;
                        }
                        // Heuristic scan near pub port to find router
                        int found = -1;
                        for (int delta = -10; delta <= 10; delta++)
                        {
                            int candidate = pubPort + delta;
                            if (candidate == pubPort || candidate <= 0) continue;
                            ConnectDealer("127.0.0.1", candidate);
                            var resp = RequestRouter("${Me.Name}");
                            if (!string.IsNullOrEmpty(resp) && !resp.StartsWith("Recv timeout") && !resp.StartsWith("Send failed") && !resp.StartsWith("Not connected") && !resp.StartsWith("Invalid response") && !resp.StartsWith("Request error"))
                            {
                                found = candidate;
                                ConnectionStatus = "Dealer connected: tcp://127.0.0.1:" + candidate;
                                Core.mq_Echo("E3Display: Auto-connected RouterPort " + candidate);
                                break;
                            }
                            DisconnectDealer();
                        }
                        if (found < 0)
                        {
                            Core.mq_Echo("E3Display: could not auto-discover RouterPort near pub port " + pubPort);
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.mq_Echo("E3Display: autoconnect error: " + ex.Message);
                    }
                });
            }
            catch { }
        }

        public static void Process()
        {
            if (!_isInit) Init();
        }

        private static void Init()
        {
            if (_isInit) return;
            _isInit = true;
            MainProcessor.ApplicationName = "E3Display";
            MainProcessor.ProcessDelay = 200;

            // No background sockets by default; connect via "/e3display connect <port>"
        }

        public static void Stop()
        {
            try
            {
                DisconnectDealer();
            }
            catch { }
        }

        private static void ConnectDealer(string ip, int port)
        {
            try
            {
                DisconnectDealer();
                AsyncIO.ForceDotNet.Force();
                _dealer = new DealerSocket();
                _dealer.Options.Identity = Guid.NewGuid().ToByteArray();
                _dealer.Options.SendHighWatermark = 50000;
                _routerAddress = $"tcp://{ip}:{port}";
                _dealer.Connect(_routerAddress);
                ConnectionStatus = "Dealer connected: " + _routerAddress;
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Dealer connect failed: " + ex.Message;
            }
        }

        private static void DisconnectDealer()
        {
            try { _dealer?.Dispose(); } catch { }
            _dealer = null;
            _routerAddress = string.Empty;
            ConnectionStatus = "Dealer disconnected";
        }

        private static string RequestRouter(string query, int sendTimeoutMs = 200, int recvTimeoutMs = 500)
        {
            if (_dealer == null) return "Not connected";
            try
            {
                var sendTimeout = TimeSpan.FromMilliseconds(sendTimeoutMs);
                var recvTimeout = TimeSpan.FromMilliseconds(recvTimeoutMs);

                // Build payload frame: [int32 cmd=1][int32 len][bytes]
                var payload = System.Text.Encoding.Default.GetBytes(query);
                var frame = new byte[8 + payload.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(1), 0, frame, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, frame, 4, 4);
                Buffer.BlockCopy(payload, 0, frame, 8, payload.Length);

                // Send multipart: empty frame + data frame
                var outgoing = new NetMQMessage();
                outgoing.AppendEmptyFrame();
                outgoing.Append(frame);
                if (!_dealer.TrySendMultipartMessage(sendTimeout, outgoing)) return "Send failed";

                // Receive exactly two frames
                NetMQMessage incoming = null;
                if (!_dealer.TryReceiveMultipartMessage(recvTimeout, ref incoming, 2)) return "Recv timeout";
                if (incoming == null || incoming.FrameCount < 2) return "Invalid response";
                var data = incoming[1].ToByteArray();
                return System.Text.Encoding.Default.GetString(data);
            }
            catch (Exception ex)
            {
                return "Request error: " + ex.Message;
            }
        }

        // Safe wrapper for Core caller
        public static string MyCode_SafeRequest(string query)
        {
            try { return RequestRouter(query); }
            catch (Exception ex) { return "Request error: " + ex.Message; }
        }

        public static void EnqueueMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _messages.Enqueue(msg);
            while (_messages.Count > MaxMessages && _messages.TryDequeue(out _)) { }
        }

        public static System.Collections.Generic.List<string> GetRecentMessagesSnapshot()
        {
            return _messages.ToList();
        }

        public static string SanitizeForImGui(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            int maxIn = Math.Min(input.Length, 500);
            // Ensure buffer can handle %% expansion (worst-case double length)
            int needed = Math.Min(maxIn * 2, 1000);
            if (_scratch.Length < needed) _scratch = new char[needed];
            int w = 0;
            for (int i = 0; i < maxIn && w < _scratch.Length; i++)
            {
                char c = input[i];
                // normalize whitespace
                if (c == '\t' || c == '\n' || c == '\r') c = ' ';
                // keep printable ASCII
                if (c >= 32 && c < 127)
                {
                    if (c == '%')
                    {
                        // Escape ImGui printf-style formatting: use %%
                        if (w + 1 < _scratch.Length)
                        {
                            _scratch[w++] = '%';
                            _scratch[w++] = '%';
                        }
                        else
                        {
                            _scratch[w++] = '%';
                        }
                    }
                    else
                    {
                        _scratch[w++] = c;
                    }
                }
                // else: drop non-printables
            }
            return new string(_scratch, 0, w);
        }
    }
}
