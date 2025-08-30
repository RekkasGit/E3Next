using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using E3Core.Processors;
using MonoCore;
using E3Core.Server;
using E3Core.Server; // for InventoryMmfPublisher reference
using E3Core.Server; // ensure namespace available (InventoryMmfPublisher is in E3Core.Server)

namespace E3Core.Server
{
    /// <summary>
    /// Named pipe server for sharing inventory data with C++ plugins
    /// Implements the E3Next Inventory Named Pipe Protocol v2
    /// </summary>
    public class InventoryNamedPipeServer_v2
    {
        private static IMQ MQ = E3.MQ;
        
        // Constants from protocol
        private const string PIPE_NAME = "E3Next_Inventory";
        private const uint MAGIC_NUMBER = 0x12345678;
        private const uint END_MARKER = 0x87654321;
        
        // Server state
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private bool _isRunning = false;
        private readonly object _serverLock = new object();
        
        // Inventory data storage - topic name to base64 data
        private static readonly ConcurrentDictionary<string, string> _inventoryTopics = new ConcurrentDictionary<string, string>();
        
        // Flag to indicate when inventory data has been updated
        private static volatile bool _inventoryUpdated = false;
        
        /// <summary>
        /// Starts the named pipe server
        /// </summary>
        public void Start()
        {
            lock (_serverLock)
            {
                if (_isRunning) return;
                
                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _serverTask = Task.Run(ServerLoopAsync, _cancellationTokenSource.Token);
                    _isRunning = true;
                    //MQ.Write("Inventory named pipe server started.");
                }
                catch (Exception ex)
                {
                    //MQ.Write($"Error starting inventory named pipe server: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Stops the named pipe server
        /// </summary>
        public void Stop()
        {
            lock (_serverLock)
            {
                if (!_isRunning) return;

                try
                {
                    if (_cancellationTokenSource != null)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                    }

                    // Wait for the server task to complete to ensure graceful shutdown
                    _serverTask?.Wait(1000); // Wait for a second

                    _isRunning = false;
                    //MQ.Write("Inventory named pipe server stopped.");
                }
                catch (Exception ex)
                {
                    //MQ.Write($"Error stopping inventory named pipe server: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Updates inventory data for a specific topic
        /// </summary>
        /// <param name="topicName">Topic name (e.g., "equipped", "bags.1-4", "CharacterName.equipped")</param>
        /// <param name="base64Data">Base64-encoded binary inventory data</param>
        public static void UpdateInventoryData(string topicName, string base64Data)
        {
            _inventoryTopics.AddOrUpdate(topicName, base64Data, (key, oldValue) => base64Data);
            _inventoryUpdated = true;

            // Immediately publish to MMF so C++ UI stays up-to-date even without a pipe client connected
            try
            {
                // Build the same framed payload as SendInventoryDataAsync does
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                    {
                        writer.Write(MAGIC_NUMBER);
                        writer.Write(_inventoryTopics.Count);
                        foreach (var topic in _inventoryTopics)
                        {
                            // reuse WriteString for consistent format
                            // Note: calling instance method requires a small helper. Duplicate length-prefixed writes inline here.
                            if (string.IsNullOrEmpty(topic.Key))
                            {
                                writer.Write((uint)0);
                            }
                            else
                            {
                                var kBytes = Encoding.UTF8.GetBytes(topic.Key);
                                writer.Write((uint)kBytes.Length);
                                writer.Write(kBytes);
                            }

                            var value = topic.Value ?? string.Empty;
                            if (value.Length == 0)
                            {
                                writer.Write((uint)0);
                            }
                            else
                            {
                                var vBytes = Encoding.UTF8.GetBytes(value);
                                writer.Write((uint)vBytes.Length);
                                writer.Write(vBytes);
                            }
                        }
                        writer.Write(END_MARKER);
                        writer.Flush();
                    }
                    var buffer = memoryStream.ToArray();
                    E3Core.Server.InventoryMmfPublisher.Instance.Publish(buffer);
                }
            }
            catch
            {
                // swallow: MMF publish failures must not impact caller
            }
        }
        
        /// <summary>
        /// Gets all current inventory topics
        /// </summary>
        /// <returns>Dictionary of topic names to base64 data</returns>
        public static ConcurrentDictionary<string, string> GetInventoryTopics()
        {
            return _inventoryTopics;
        }

        /// <summary>
        /// Main server loop that handles client connections asynchronously
        /// Improved version that maintains persistent connections and doesn't crash clients
        /// </summary>
        private async Task ServerLoopAsync()
        {
            try
            {
                // Allow multiple clients and use bidirectional pipe for better compatibility
                // Pipes disabled: do not create a pipe server. We keep the loop to allow future re-enable if needed.
                NamedPipeServerStream pipeServer = null;
                {
                    //MQ.Write("Inventory named pipe server waiting for client connections...");
                    
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Always wait for connections - don't require inventory updates first
                            // Pipes are disabled. We only use MMF publishing which happens on UpdateInventoryData().
                            // Maintain a small delay to avoid tight loop.
                            await Task.Delay(200, _cancellationTokenSource.Token);
                        }
                        catch (IOException ex)
                        {
                            // Handle pipe disconnection gracefully
                            //MQ.Write($"Named pipe connection lost: {ex.Message}");
                            if (pipeServer.IsConnected)
                            {
                                pipeServer.Disconnect();
                            }
                            await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before reconnecting
                        }
                        catch (Exception ex)
                        {
                            //MQ.Write($"Unexpected error in named pipe server: {ex.Message}");
                            await Task.Delay(1000, _cancellationTokenSource.Token);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when server is stopped
            }
            catch (Exception ex)
            {
                //MQ.Write($"Fatal error in inventory named pipe server loop: {ex.Message}");
            }
        }

        
        /// <summary>
        /// Sends all inventory data to connected client asynchronously
        /// Improved version with better error handling and logging
        /// </summary>
        private async Task SendInventoryDataAsync(NamedPipeServerStream pipeServer)
        {
            try
            {
                // Pipes are disabled; do not attempt to send over pipe.
                return;

                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                    {
                        // Write magic number
                        writer.Write(MAGIC_NUMBER);
                        
                        // Write topic count
                        writer.Write(_inventoryTopics.Count);
                        
                        // Write each topic
                        foreach (var topic in _inventoryTopics)
                        {
                            WriteString(writer, topic.Key);
                            WriteString(writer, topic.Value);
                        }
                        
                        // Write end marker
                        writer.Write(END_MARKER);
                        
                        // Flush the data to ensure it's sent
                        writer.Flush();
                    }
                    
                    // Prepare payload for both transports (MMF + optional pipe)
                    var buffer = memoryStream.ToArray();

                    // Publish to Memory-Mapped File for C++ UI reader
                    try
                    {
                        E3Core.Server.InventoryMmfPublisher.Instance.Publish(buffer);
                    }
                    catch
                    {
                        // swallow: MMF publish should never crash the server path
                    }

                    // Pipes are disabled; skip writing to pipe.
                    return;
                }
            }
            catch (IOException)
            {
                // Client likely disconnected
                throw; // Re-throw to trigger disconnect handling
            }
            catch (Exception)
            {
                throw; // Re-throw to trigger disconnect handling
            }
        }

        /// <summary>
        /// Writes data to pipe with proper async handling
        /// </summary>
        private async Task WriteWithTimeoutAsync(NamedPipeServerStream pipeServer, byte[] buffer)
        {
            await pipeServer.WriteAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
            await pipeServer.FlushAsync(_cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Writes a string to the binary writer with length prefix
        /// </summary>
        /// <param name="writer">Binary writer</param>
        /// <param name="value">String value to write</param>
        private void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write((uint)0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                writer.Write((uint)bytes.Length);
                writer.Write(bytes);
            }
        }
    }
}
