using E3Core.Processors;
using MonoCore;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MQServerClient
{
    class Program
    {
        public static Stopwatch _stopWatch = new Stopwatch();
        static void Main(string[] args)
        {
			//need to do this so double parses work in other languages
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			AsyncIO.ForceDotNet.Force();
            MonoCore.Core._MQ2MonoVersion = 0.21m;
            MonoCore.Core.mqInstance = new NetMQMQ();
            MonoCore.Core.spawnInstance = new NetMQSpawns();
            MonoCore.Core.OnInit();

            NetMQOnIncomingChat _incChat = new NetMQOnIncomingChat();
            _incChat.Start();

            E3.MQ = Core.mqInstance;
            E3.Log = Core.logInstance;
            E3.Spawns = Core.spawnInstance;

            Core.mqInstance.Cmd("/remotedebugdelay 100");
            E3.Process();
            Core.mqInstance.Cmd("/remotedebugdelay 1");
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} Start of e3 scan loop");
                E3.Process();
                EventProcessor.ProcessEventsInQueues();
                //System.Threading.Thread.Sleep(1000);
            }

        }
    }
   

    public class NetMQSpawns : ISpawns
    {
        //special list so we can get rid of the non dirty values
        private static List<Spawn> _tmpSpawnList = new List<Spawn>();

        DealerSocket _requestSocket;
        NetMQ.Msg _requestMsg = new NetMQ.Msg();
        public TimeSpan SendTimeout = new TimeSpan(0, 5, 5);
        public TimeSpan RecieveTimeout = new TimeSpan(0, 5, 30);
        byte[] _payload = new byte[1000 * 86];
        Int32 _payloadLength = 0;
        public static Dictionary<string, Spawn> _spawnsByName = new Dictionary<string, Spawn>(2048, StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Int32, Spawn> _spawnsByID = new Dictionary<int, Spawn>(2048);

        public static List<Spawn> _spawns = new List<Spawn>(2048);
        public static Int64 _lastRefesh = 0;
        public static Int64 _refreshTimePeriodInMS = 1000;
        public NetMQSpawns()
        {
            _requestSocket = new DealerSocket();
            _requestSocket.Options.Identity = Guid.NewGuid().ToByteArray();
            _requestSocket.Options.SendHighWatermark = 10000;
            _requestSocket.Options.ReceiveHighWatermark = 10000;
            _requestSocket.Connect("tcp://127.0.0.1:" + RemoteDebugServerConfig.NetMQRouterPort.ToString());
        }
        public bool TryByID(Int32 id, out Spawn s)
        {
            RefreshListIfNeeded();
            return _spawnsByID.TryGetValue(id, out s);
        }
        public bool TryByName(string name, out Spawn s)
        {
            RefreshListIfNeeded();
            return _spawnsByName.TryGetValue(name, out s);
        }
        public Int32 GetIDByName(string name)
        {
            RefreshListIfNeeded();
            Spawn returnValue;
            if (_spawnsByName.TryGetValue(name, out returnValue))
            {
                return returnValue.ID;
            }
            return 0;
        }
        public bool Contains(string name)
        {
            RefreshListIfNeeded();
            return _spawnsByName.ContainsKey(name);
        }
        public bool Contains(Int32 id)
        {
            RefreshListIfNeeded();
            return _spawnsByID.ContainsKey(id);
        }
        public IEnumerable<Spawn> Get()
        {
            if (Core.StopWatch.ElapsedMilliseconds - _lastRefesh > _refreshTimePeriodInMS)
            {
                RefreshList();
            }
            return _spawns;
        }
        private void RefreshListIfNeeded()
        {
            if (Core.StopWatch.ElapsedMilliseconds - _lastRefesh > _refreshTimePeriodInMS)
            {
                RefreshList();
            }
        }
        public void EmptyLists()
        {

        }
        public void RefreshList()
        {
            foreach (var spawn in _spawns)
            {
                spawn.isDirty = false;
            }
         
            //_spawns should have fresh data now!
            _lastRefesh = Core.StopWatch.ElapsedMilliseconds;
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();
            //send empty frame
            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength =0;

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 7;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);


            _requestMsg.Close();
          
            bool finishedResposne = false;
            List<Spawn> returnList = new List<Spawn>();
            while(!finishedResposne)
            {
                _requestMsg.InitEmpty();
                //recieve the empty frame
                while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
                {
                    //wait for the message to come back
                }
                _requestMsg.Close();
                _requestMsg.InitEmpty();
                //recieve the data
                while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
                {
                    //wait for the message to come back
                }
                if(_requestMsg.Size==0)
                {
                    finishedResposne = true;
                }
                else
                {
                    Int32 ID = BitConverter.ToInt32(_requestMsg.Data, 0);
                    Spawn s;
                    if (_spawnsByID.TryGetValue(ID, out s))
                    {
                        //just update the value
                        s.Init(_requestMsg.Data, _requestMsg.Size);
                    }
                    else
                    {
                        var spawn = Spawn.Aquire();
                        spawn.Init(_requestMsg.Data, _requestMsg.Size);
                        _spawns.Add(spawn);
                    }
                   
                }
                _requestMsg.Close();

            }

            
            //spawns has new/updated data, get rid of the non dirty stuff.
            //can use the other dictionaries to help
            _spawnsByName.Clear();
            _spawnsByID.Clear();
            foreach (var spawn in _spawns)
            {
                if (spawn.isDirty)
                {
                    _tmpSpawnList.Add(spawn);
                    if (spawn.TypeDesc == "PC")
                    {
                        if (!_spawnsByName.ContainsKey(spawn.Name))
                        {
                            _spawnsByName.Add(spawn.Name, spawn);
                        }

                    }
                    _spawnsByID.Add(spawn.ID, spawn);
                }
                else
                {
                    spawn.Dispose();
                }
            }
            //swap the collections
            _spawns.Clear();
            List<Spawn> tmpPtr = _spawns;
            _spawns = _tmpSpawnList;
            _tmpSpawnList = tmpPtr;

            //_spawns should have fresh data now!
            _lastRefesh = Core.StopWatch.ElapsedMilliseconds;

        }
    }

    public static class RemoteDebugServerConfig
    {
        //used for browser queries
        public static Int32 HTTPPort = 12345;
        //used for remote debugging
        public static Int32 NetMQRouterPort = 12346;
        //used for remote debugging events
        public static Int32 NetMQPubPort = 12347;
        //how long we wait once a command has been seen
        //use this to tweak how much of heartbeat 'lag' you wish. careful this will
        //directly impact query performance if set too low. 
        public readonly static Double CurrentWaitTimeWhenRequestFound = 100;
    }
    public class NetMQOnIncomingChat
    {

        Task _serverThread;
        public void Start()
        {
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }

        public void Process()
        {

            using (var subSocket = new SubscriberSocket())
            {
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://localhost:"+ RemoteDebugServerConfig.NetMQPubPort.ToString());
                subSocket.Subscribe("OnIncomingChat");
                subSocket.Subscribe("OnWriteChatColor");
                subSocket.Subscribe("OnCommand");
                Console.WriteLine("Subscriber socket connecting...");
                while (true)
                {
                    string messageTopicReceived = subSocket.ReceiveFrameString();
                    string messageReceived = subSocket.ReceiveFrameString();
                    Console.WriteLine(messageReceived);
                    if(messageTopicReceived== "OnWriteChatColor")
                    {
                        EventProcessor.ProcessMQEvent(messageReceived);
                    }
                    else if(messageTopicReceived == "OnIncomingChat")
                    {
                        EventProcessor.ProcessEvent(messageReceived);
                    }
                    else if (messageTopicReceived == "OnCommand")
                    {
                        EventProcessor.ProcessMQCommand(messageReceived);
                    }
                }
            }
        }
    }


    public class NetMQMQ : MonoCore.IMQ
    {
        DealerSocket _requestSocket;
        NetMQ.Msg _requestMsg = new NetMQ.Msg();
        public TimeSpan SendTimeout = new TimeSpan(0, 5, 5);
        public TimeSpan RecieveTimeout = new TimeSpan(0, 5, 30);
        byte[] _payload = new byte[1000 * 86];
        Int32 _payloadLength = 0;

        public NetMQMQ()
        {
           
            _requestSocket = new DealerSocket();
            _requestSocket.Options.Identity = Guid.NewGuid().ToByteArray();
            _requestSocket.Options.SendHighWatermark = 100;
            _requestSocket.Connect("tcp://127.0.0.1:" + RemoteDebugServerConfig.NetMQRouterPort.ToString());
        }
        public void Broadcast(string query)
        {
            query = @"bc " + query;
            Cmd(query);
        }

        public void Cmd(string query, bool delayed = false)
        {
            //send empty frame over
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 2;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);
            _requestMsg.Close();

            Console.WriteLine("CMD:" + query);
            //do work
        }
        public void Cmd(string query, Int32 delay, bool delayed = false)
        {
            Cmd(query, delayed);
            Delay(delay);

        }
        public void Delay(int value)
        {
            //set so we don't lock up the screen
            if (value < 150)
            {
                value = 150;
            }
            System.Threading.Thread.Sleep(value);
        }

        public bool Delay(int maxTimeToWait, string Condition)
        {
            Condition = $"${{If[{Condition},TRUE,FALSE]}}";
            Int64 startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                System.Threading.Thread.Sleep(200);
            }
            return true;
        }
        public bool Delay(int maxTimeToWait, Func<bool> methodToCheck)
        {
            Int64 startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!methodToCheck.Invoke())
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                System.Threading.Thread.Sleep(200);
            }
            return true;
        }

        public T Query<T>(string query)
        {
            // Console.WriteLine(query);
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();
            //send empty frame
            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 1;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);


            _requestMsg.Close();
            _requestMsg.InitEmpty();

            //recieve the empty frame
            while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
            {
                //wait for the message to come back
            }
            _requestMsg.Close();
            _requestMsg.InitEmpty();
            while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
            {
                //wait for the message to come back
            }



            //data is back, lets parse out the data

            string mqReturnValue = System.Text.Encoding.Default.GetString(_requestMsg.Data, 0, _requestMsg.Data.Length);

            _requestMsg.Close();





            if (typeof(T) == typeof(Int32))
            {
                if (!mqReturnValue.Contains("."))
                {
                    Int32 value;
                    if (Int32.TryParse(mqReturnValue, out value))
                    {
                        return (T)(object)value;
                    }
                    else { return (T)(object)-1; }
                }
                else
                {
                    Decimal value;
                    if (decimal.TryParse(mqReturnValue, out value))
                    {
                        return (T)(object)value;
                    }
                    else { return (T)(object)-1; }

                }
            }
            else if (typeof(T) == typeof(Boolean))
            {
                Boolean booleanValue;
                if (Boolean.TryParse(mqReturnValue, out booleanValue))
                {
                    return (T)(object)booleanValue;
                }
                if (mqReturnValue == "NULL")
                {
                    return (T)(object)false;
                }
                if (mqReturnValue == "!FALSE")
                {
                    return (T)(object)true;
                }
                if (mqReturnValue == "!TRUE")
                {
                    return (T)(object)false;
                }
                Int32 intValue;
                if (Int32.TryParse(mqReturnValue, out intValue))
                {
                    if (intValue > 0)
                    {
                        return (T)(object)true;
                    }
                    return (T)(object)false;
                }
                if (string.IsNullOrWhiteSpace(mqReturnValue))
                {
                    return (T)(object)false;
                }

                return (T)(object)true;


            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)mqReturnValue;
            }
            else if (typeof(T) == typeof(decimal))
            {
                Decimal value;
                if (Decimal.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
                else { return (T)(object)-1M; }
            }
            else if (typeof(T) == typeof(double))
            {
                double value;
                if (double.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
                else { return (T)(object)-1D; }
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
                else { return (T)(object)-1L; }
            }


            return default(T);

        }

        public void TraceStart(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"|- {methodName} ==>");
        }
        public void TraceEnd(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"<== {methodName} -|");
        }

        public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            query = $"[{MainProcessor.ApplicationName}][{System.DateTime.Now.ToString("HH:mm:ss")}] {query}";
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();
            //send empty frame
            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);

            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 3;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }
            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);
        }

        public bool AddCommand(string query)
        {
            //send empty frame over
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 4;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);
            _requestMsg.Close();

            Console.WriteLine("AddCommand:" + query);
            return true;
        }
        public void ClearCommands()
        {
            string query = "clear";
            //send empty frame over
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 5;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);
            _requestMsg.Close();

            Console.WriteLine("ClearCommands: Issued");
        }
        public void RemoveCommand(string commandName)
        {
            //send empty frame over
            if (_requestMsg.IsInitialised)
            {
                _requestMsg.Close();
            }
            _requestMsg.InitEmpty();

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

            _payloadLength = System.Text.Encoding.Default.GetBytes(commandName, 0, commandName.Length, _payload, 0);

            _requestMsg.Close();

            //include command+ length in payload
            _requestMsg.InitPool(_payloadLength + 8);



            unsafe
            {
                fixed (byte* src = _payload)
                {

                    fixed (byte* dest = _requestMsg.Data)
                    {   //4 bytes = commandtype
                        //4 bytes = length
                        //N-bytes = payload
                        byte* tPtr = dest;
                        *((Int32*)tPtr) = 6;
                        tPtr += 4;
                        *(Int32*)tPtr = _payloadLength; //init/modify
                        tPtr += 4;
                        Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
                    }

                }
            }

            _requestSocket.TrySend(ref _requestMsg, SendTimeout, false);
            _requestMsg.Close();

            Console.WriteLine("RemoveCommand:" + commandName);
        }

        public bool FeatureEnabled(MQFeature feature)
        {
            return true;
        }

		public string GetFocusedWindowName()
		{
            return "NULL";
		}

		public void WriteDelayed(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
		{
			Write(query, memberName, fileName, lineNumber);
		}

		public string SpellDataGetLine(string query, int line)
		{
			if (_requestMsg.IsInitialised)
			{
				_requestMsg.Close();
			}
			_requestMsg.InitEmpty();
			//send empty frame

			//create query+parm buffer
			string pramQuery = query + "," + line;

			_requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

			_payloadLength = System.Text.Encoding.Default.GetBytes(pramQuery, 0, pramQuery.Length, _payload, 0);

			_requestMsg.Close();

			//include command+ length in payload
			_requestMsg.InitPool(_payloadLength + 8);



			unsafe
			{
				fixed (byte* src = _payload)
				{

					fixed (byte* dest = _requestMsg.Data)
					{   //4 bytes = commandtype
						//4 bytes = length
						//N-bytes = payload
						byte* tPtr = dest;
						*((Int32*)tPtr) = 9;
						tPtr += 4;
						*(Int32*)tPtr = _payloadLength; //init/modify
						tPtr += 4;
						Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
					}

				}
			}

			_requestSocket.TrySend(ref _requestMsg, SendTimeout, false);


			_requestMsg.Close();
			_requestMsg.InitEmpty();

			//recieve the empty frame
			while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
			{
				//wait for the message to come back
			}
			_requestMsg.Close();
			_requestMsg.InitEmpty();
			while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
			{
				//wait for the message to come back
			}

			//data is back, lets parse out the data

			string mqReturnValue = System.Text.Encoding.Default.GetString(_requestMsg.Data, 0, _requestMsg.Data.Length);

			_requestMsg.Close();

			return mqReturnValue;
		}

		public int SpellDataGetLineCount(string query)
		{
			if (_requestMsg.IsInitialised)
			{
				_requestMsg.Close();
			}
			_requestMsg.InitEmpty();
			//send empty frame
			_requestSocket.TrySend(ref _requestMsg, SendTimeout, true);

			_payloadLength = System.Text.Encoding.Default.GetBytes(query, 0, query.Length, _payload, 0);

			_requestMsg.Close();

			//include command+ length in payload
			_requestMsg.InitPool(_payloadLength + 8);



			unsafe
			{
				fixed (byte* src = _payload)
				{

					fixed (byte* dest = _requestMsg.Data)
					{   //4 bytes = commandtype
						//4 bytes = length
						//N-bytes = payload
						byte* tPtr = dest;
						*((Int32*)tPtr) = 8;
						tPtr += 4;
						*(Int32*)tPtr = _payloadLength; //init/modify
						tPtr += 4;
						Buffer.MemoryCopy(src, tPtr, _requestMsg.Data.Length, _payloadLength);
					}

				}
			}

			_requestSocket.TrySend(ref _requestMsg, SendTimeout, false);


			_requestMsg.Close();
			_requestMsg.InitEmpty();

			//recieve the empty frame
			while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
			{
				//wait for the message to come back
			}
			_requestMsg.Close();
			_requestMsg.InitEmpty();
			while (!_requestSocket.TryReceive(ref _requestMsg, RecieveTimeout))
			{
				//wait for the message to come back
			}

			//data is back, lets parse out the data

			string mqReturnValue = System.Text.Encoding.Default.GetString(_requestMsg.Data, 0, _requestMsg.Data.Length);

			_requestMsg.Close();

			return Int32.Parse(mqReturnValue);
		}

		public string GetHoverWindowName()
		{
			return "NULL";
		}
	}

}
