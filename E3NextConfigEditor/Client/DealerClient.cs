using NetMQ.Sockets;
using System;

namespace E3NextConfigEditor.Client
{
	public class DealerClient
	{
		DealerSocket _requestSocket;
		NetMQ.Msg _requestMsg = new NetMQ.Msg();
		public TimeSpan SendTimeout = new TimeSpan(0, 5, 5);
		public TimeSpan RecieveTimeout = new TimeSpan(0, 5, 30);
		byte[] _payload = new byte[1000 * 86];
		Int32 _payloadLength = 0;
		Int32 _port;
		public DealerClient(Int32 port)
		{
			_port = port;

		}

		private void ResetSocket()
		{
			if (_requestSocket != null)
			{
				_requestSocket.Dispose();
			}

			_requestSocket = new DealerSocket();
			_requestSocket.Options.Identity = Guid.NewGuid().ToByteArray();
			_requestSocket.Options.SendHighWatermark = 50000;
			_requestSocket.Connect("tcp://127.0.0.1:" + _port.ToString());
		}
		public byte[] RequestRawData(string query)
		{

			if (_requestSocket == null)
			{
				ResetSocket();
			}

		retry:
			try
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

				byte[] mqReturnValue = new byte[_requestMsg.Data.Length];
				Buffer.BlockCopy(_requestMsg.Data, 0, mqReturnValue, 0, mqReturnValue.Length);

				_requestMsg.Close();

				return mqReturnValue;
			}
			catch (Exception)
			{
				System.Threading.Thread.Sleep(1000);
				ResetSocket();
				goto retry;
			}
		}
		public string RequestData(string query)
		{

			if (_requestSocket == null)
			{
				ResetSocket();
			}

		retry:
			try
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
			catch (Exception)
			{
				System.Threading.Thread.Sleep(1000);
				ResetSocket();
				goto retry;
			}
		}
	}
}
