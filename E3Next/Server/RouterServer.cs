using E3Core.Processors;
using MonoCore;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E3Core.Server
{
    public class RouterMessage : IDisposable
    {
        public byte[] identity = new byte[1024 * 86]; //86k to get on the LOH
        public Int32 identiyLength = 0;
        public Int32 commandType = 0;
        public byte[] payload = new byte[1024 * 86];
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
    }
    public class RouterServer
    {
        private static IMQ MQ = E3.Mq;
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
            while (_tloRequets.Count > 0)
            {
                RouterMessage message;
                _tloRequets.TryDequeue(out message);
                //lets pull out the string
                string query = System.Text.Encoding.Default.GetString(message.payload, 0, message.payloadLength);
                string response = MQ.Query<string>(query);

                message.payloadLength = System.Text.Encoding.Default.GetBytes(response, 0, response.Length, message.payload, 0);
                _tloResposne.Enqueue(message);
            }
        }

        private void Process()
        {
            AsyncIO.ForceDotNet.Force();
            _rpcRouter = new RouterSocket();
            _rpcRouter.Options.SendHighWatermark = 10000;
            _rpcRouter.Options.ReceiveHighWatermark = 10000;
            _rpcRouter.Bind("tcp://127.0.0.1:" + RouterPort.ToString());
            //_rpcRouter.Bind("tcp://127.0.0.1:12346");
            routerMessage.InitEmpty();
            try
            {

                while (Core._isProcessing)
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


        }

    }
}
