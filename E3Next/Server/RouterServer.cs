using E3Core.Processors;
using E3Core.Utility;
using Google.Protobuf;
using MonoCore;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E3Core.Server
{
    /// <summary>
    /// handles request/reply from clients asking for TLO data
    /// </summary>
    public class RouterMessage : IDisposable
    {
        public byte[] identity = new byte[1024 * 86]; //86k to get on the LOH
        public Int32 identiyLength = 0;
        public Int32 commandType = 0;
        public byte[] payload = new byte[1024 * 1024];
        public Int32 payloadLength = 0;
        public IEnumerable<Spawn> spawns;
        public static RouterMessage Aquire()
        {
            RouterMessage obj;
            if (!StaticObjectPool.TryPop<RouterMessage>(out obj))
            {
                obj = new RouterMessage();
            }

            return obj;
        }
        public void Dispose()
        {
            payloadLength = 0;
            identiyLength = 0;
            spawns = null;
            StaticObjectPool.Push<RouterMessage>(this);
        }
        ~RouterMessage()
        {
            //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
            //if this is called, it will cause the domain to hang in the GC when shuttind down
            //This is only here to warn you

        }

    }
    public class RouterServer
    {
        private static IMQ MQ = E3.MQ;
        RouterSocket _rpcRouter = null;
        Task _serverThread = null;
        NetMQ.Msg routerResponse = new NetMQ.Msg();
        TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 1);
        NetMQ.Msg routerMessage = new NetMQ.Msg();
        Int64 counter = 0;
        static TimeSpan timeout = new TimeSpan(0, 0, 0, 5);

        public static ConcurrentQueue<RouterMessage> _tloRequets = new ConcurrentQueue<RouterMessage>();
        public static ConcurrentQueue<RouterMessage> _tloResposne = new ConcurrentQueue<RouterMessage>();
        

        public Int32 RouterPort = 0;

        public void Start(Int32 port)
        {
            RouterPort = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        //called by the main C# thread
        public static void ProcessRequests()
        {
            bool _inBulkMode = false;
            while (_tloRequets.Count > 0 )
            {
                RouterMessage message;
                _tloRequets.TryDequeue(out message);
                //lets pull out the string
                string query = System.Text.Encoding.Default.GetString(message.payload, 0, message.payloadLength);
                string response = String.Empty;

                if (query =="${IniServerName}")
                {
                    response = Setup._serverNameForIni;                    
                }
                else if(String.Equals(query,"${E3.AA.ListAll}", StringComparison.OrdinalIgnoreCase))
                {
					List<Data.Spell> aas = e3util.ListAllActiveAA();

                    SpellDataList spellDatas = new SpellDataList();
                    foreach(var aa in aas)
                    {
                        spellDatas.Data.Add(aa.ToProto());
                    }


                    byte[] bytes = spellDatas.ToByteArray();
					message.payloadLength = bytes.Length;
                    Buffer.BlockCopy(bytes, 0, message.payload, 0, message.payloadLength);
                   
				}
				else if (String.Equals(query, "${E3.SpellBook.ListAll}", StringComparison.OrdinalIgnoreCase))
				{
					List<Data.Spell> spells = e3util.ListAllBookSpells();

					SpellDataList spellDatas = new SpellDataList();
					foreach (var spell in spells)
					{
						spellDatas.Data.Add(spell.ToProto());
					}
					byte[] bytes = spellDatas.ToByteArray();
					message.payloadLength = bytes.Length;
					Buffer.BlockCopy(bytes, 0, message.payload, 0, message.payloadLength);

				}
				else if (String.Equals(query, "${E3.Discs.ListAll}", StringComparison.OrdinalIgnoreCase))
				{
					List<Data.Spell> spells = e3util.ListAllDiscData();

					SpellDataList spellDatas = new SpellDataList();
					foreach (var spell in spells)
					{
						spellDatas.Data.Add(spell.ToProto());
					}
					byte[] bytes = spellDatas.ToByteArray();
					message.payloadLength = bytes.Length;
					Buffer.BlockCopy(bytes, 0, message.payload, 0, message.payloadLength);

				}
				else if (String.Equals(query, "${E3.Skills.ListAll}", StringComparison.OrdinalIgnoreCase))
				{
					List<Data.Spell> spells = e3util.ListAllActiveSkills();

					SpellDataList spellDatas = new SpellDataList();
					foreach (var spell in spells)
					{
						spellDatas.Data.Add(spell.ToProto());
					}
					byte[] bytes = spellDatas.ToByteArray();
					message.payloadLength = bytes.Length;
					Buffer.BlockCopy(bytes, 0, message.payload, 0, message.payloadLength);

				}
				else
                {
                    //string return types
					if (String.Equals(query, "${E3.TLO.BulkBegin}", StringComparison.OrdinalIgnoreCase))
					{
						//we are about to get a bunch of TLO requests, stay in this loop until we get a BulkEnd
						//note this is not safe for multiple clients
						_inBulkMode = true;
						response = "TRUE";
					}
					else if (String.Equals(query, "${E3.TLO.BulkEnd}", StringComparison.OrdinalIgnoreCase))
					{
						//we are ending the TLO bulk mode
						//note, this isn't safe for mutlipe clients 
						_inBulkMode = false;
						response = "TRUE";
					}
					else
					{
						response = MQ.Query<string>(query);
					}
					message.payloadLength = System.Text.Encoding.Default.GetBytes(response, 0, response.Length, message.payload, 0);
				}
                _tloResposne.Enqueue(message);


                if(_inBulkMode)
                {

                    Int32 bulkSleepCounter = 0;
                    while(_tloRequets.Count==0 && bulkSleepCounter<1000)
                    {
                        //if bulk mode lasts too log without data, kick out of bulk mode after about 1 second
                        bulkSleepCounter++;
                        System.Threading.Thread.Sleep(1);
                    }
                }

            }
        }

        private void Process()
        {
			//need to do this so double parses work in other languages
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			AsyncIO.ForceDotNet.Force();
            _rpcRouter = new RouterSocket();
            _rpcRouter.Options.SendHighWatermark = 50000;
            _rpcRouter.Options.ReceiveHighWatermark = 50000;
            _rpcRouter.Bind("tcp://127.0.0.1:" + RouterPort.ToString());
            //_rpcRouter.Bind("tcp://127.0.0.1:12346");
            routerMessage.InitEmpty();
            try
            {

                while (Core.IsProcessing && E3.NetMQ_RouterServerThradRun)
                {

                    if (_rpcRouter.TryReceive(ref routerMessage, recieveTimeout))
                    {
                        RouterMessage message = RouterMessage.Aquire();
                        //first get identit identityJump:
                        unsafe
                        {
                            fixed (byte* src = routerMessage.Data)
                            {
                                fixed (byte* dest = message.identity)
                                {
                                    Buffer.MemoryCopy(src, dest, message.identity.Length, routerMessage.Size);
                                }
                            }
                        }
                        message.identiyLength = routerMessage.Size;
                        routerMessage.Close();
                        routerMessage.InitEmpty();

                        //next get empty frame

                        _rpcRouter.TryReceive(ref routerMessage, timeout);
                        routerMessage.Close();
                        routerMessage.InitEmpty();

                        //combined MethodTopicOptions 4 + method + 4 + topic + 4 + options
                        //next method frame
                        _rpcRouter.TryReceive(ref routerMessage, timeout);

                        //do something with the message
                        //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload

                        unsafe
                        {
                            fixed (byte* src = routerMessage.Data)
                            {

                                byte* tempPtr = src;

                                message.commandType = *(Int32*)(tempPtr);
                                tempPtr += 4;//move past the command

                                message.payloadLength = *(Int32*)(tempPtr);
                                tempPtr += 4;//move past the command

                                fixed (byte* dest = message.payload)
                                {
                                    //copy everything but the last bytes we have. 
                                    Buffer.MemoryCopy(tempPtr, dest, message.payload.Length, routerMessage.Size - 8);
                                }
                            }
                        }

                        if (message.commandType == 1)
                        {
                            _tloRequets.Enqueue(message);
                        }
                        else
                        {
                            message.Dispose();
                        }


                        //should not have more here, drain any remaining and close out.
                        while (routerMessage.HasMore)
                        {
                            routerMessage.Close();
                            routerMessage.InitEmpty();
                            //drain them

                            _rpcRouter.TryReceive(ref routerMessage, timeout);

                        }
                        //clean up memory after we are done with it
                        routerMessage.Close();
                        routerMessage.InitEmpty();
                        counter++;




                    }
                    //process all the responses that we have to give
                    while (_tloResposne.Count > 0)
                    {
                        RouterMessage message;
                        _tloResposne.TryDequeue(out message);
                        if (message != null)
                        {
                            try
                            {
                                routerResponse.InitPool(message.identiyLength);
                                Buffer.BlockCopy(message.identity, 0, routerResponse.Data, 0, message.identiyLength);
                                _rpcRouter.TrySend(ref routerResponse, timeout, true);
                                routerResponse.Close();
                                routerResponse.InitEmpty();
                                _rpcRouter.TrySend(ref routerResponse, timeout, true);
                                routerResponse.Close();
                                routerResponse.InitPool(message.payloadLength);
                                Buffer.BlockCopy(message.payload, 0, routerResponse.Data, 0, message.payloadLength);
                                _rpcRouter.TrySend(ref routerResponse, timeout, false);
                                routerResponse.Close();
                            }
                            finally
                            {
                                //put back into the object pool.
                                message.Dispose();
                            }
                        }
                    }
                    
                }
            }
            catch (Exception)
            {

            }


            _rpcRouter.Dispose();
            MQ.Write("Shutting down RouterServer Thread.");

        }

    }
}
