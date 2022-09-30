using E3Core.Processors;
using MonoCore;
using NetMQ.Sockets;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MQServerClient
{
    class Program
    {
        static void Main(string[] args)
        {



            E3.MQ = new ClientMQ();
            E3._log = new Logging(E3.MQ);

            while(true)
            {

                E3.Process();

                System.Threading.Thread.Sleep(1000);
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
        Int32 _payloadLength=0;

        public NetMQMQ()
        {
            _requestSocket = new DealerSocket();
            _requestSocket.Options.Identity = Guid.NewGuid().ToByteArray();
            _requestSocket.Options.SendHighWatermark = 100;
            _requestSocket.Connect("tcp://127.0.0.1:12346");
        }
        public void Broadcast(string query)
        {
            query = @"bc " + query;
            Cmd(query);
        }

        public void Cmd(string query)
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
            _requestMsg.InitPool(_payloadLength+8);

           

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

        public void Delay(int value)
        {

            System.Threading.Thread.Sleep(value);

        }

        public bool Delay(int maxTimeToWait, string Condition)
        {
            Int64 startingTime = Core._stopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core._stopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                System.Threading.Thread.Sleep(10);
            }
            return true;
        }

        public T Query<T>(string query)
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

            string mqReturnValue = response.Content;
            if (typeof(T) == typeof(Int32))
            {
                Int32 value;
                if (Int32.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
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
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
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
            var request = new RestRequest("write/" + query, Method.Get);
            RestResponse response = client.Execute(request);
            Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {query}");
        }
    }

    public class ClientMQ : MonoCore.IMQ
    {
        RestClient client = new RestClient("http://192.168.1.191:12345");
        public void Broadcast(string query)
        {
            query = @"bc " + query;
            Cmd(query);    
        }

        public void Cmd(string query)
        {
            if(query.StartsWith(@"/"))
            {
                query = query.Substring(1, query.Length - 1);
            }
            var request = new RestRequest("command/" + query, Method.Get);
            RestResponse response = client.Execute(request);
            client.Execute(request);
            Console.WriteLine("CMD:" + query);
            //do work
        }

        public void Delay(int value)
        {

            System.Threading.Thread.Sleep(value);
           
        }

        public bool Delay(int maxTimeToWait, string Condition)
        {
            Int64 startingTime = Core._stopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core._stopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                System.Threading.Thread.Sleep(10);
            }
            return true;
        }

        public T Query<T>(string query)
        {
            Console.WriteLine("<Query>:" + query);
            var request = new RestRequest("/TLO/"+query, Method.Get);
            RestResponse response = client.Execute(request);

            string mqReturnValue = response.Content;
            if (typeof(T) == typeof(Int32))
            {
                Int32 value;
                if (Int32.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
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
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
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
            var request = new RestRequest("write/" + query, Method.Get);
            RestResponse response = client.Execute(request);
            Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {query}");
        }
    }
}
