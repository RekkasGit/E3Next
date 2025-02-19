using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace E3NextProxy
{
	public class Proxy_Manual
	{
		private static volatile bool _isRunning = false;
		private ConcurrentQueue<Action> _actionsToprocess = new ConcurrentQueue<Action>();
		private SubscriberSocket _localSubscriberSocket = null;
		private PublisherSocket _publisherSocket = null;
		private PublisherSocket _externalPublisherSocket = null;
		private SubscriberSocket _externalProxySubscriberSocket = null;
		private Task _ProcessSubscriptionsTask=null;
		private ConcurrentDictionary<string, string> _localUseres = new ConcurrentDictionary<string, string>();
		private Int32 _externalProxySubscriberCount = 0;
		public void AddExteranlProxySubBinding(string connectionString)
		{
			_actionsToprocess.Enqueue(new Action(() => {

				if (_isRunning)
				{
					Console.WriteLine($"External Subscriber socket connecting...{connectionString}");
					_externalProxySubscriberSocket.Connect(connectionString);
					_externalProxySubscriberCount++;

				}
			}));
		}

		public void AddLocalSubBinding(string user,string connectionString)
		{
			_actionsToprocess.Enqueue(new Action(() => {

				if (_isRunning)
				{
					Console.WriteLine($"Subscriber socket connecting...user:[{user}] {connectionString}");
					_localUseres.TryAdd(user, connectionString);
					_localSubscriberSocket.Connect(connectionString);

				}
			}));
		}
		public void RemoveExternalProxySubBinding(string user, string connectionString)
		{
			_actionsToprocess.Enqueue(new Action(() => {

				if (_isRunning)
				{
					Console.WriteLine($"External Subscriber socket disconnecting...{connectionString}");
					_externalProxySubscriberSocket.Disconnect(connectionString);
					_externalProxySubscriberCount--;
				}
			}));
		}
		public void RemoveSubBinding(string user,string connectionString)
		{
			_actionsToprocess.Enqueue(new Action(() => {

				if (_isRunning)
				{
					Console.WriteLine($"Subscriber socket disconnecting...{connectionString}");
					_localUseres.TryRemove(user,out var cs);
					_localSubscriberSocket.Disconnect(connectionString);
				}
			}));
		}


		public Proxy_Manual()
		{
		}

		/// <summary>
		/// Start proxying messages between the front and back ends. Blocks, unless using an external <see cref="NetMQPoller"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">The proxy has already been started.</exception>
		public void Start(Int32 pubPort)
		{

			if(!_isRunning)
			{
				_isRunning = true;
				_ProcessSubscriptionsTask = Task.Factory.StartNew(() =>
				{
					ProcessSubs(pubPort);
				});
			}
		}
		
		/// <summary>
		/// Stops the proxy, blocking until the underlying <see cref="NetMQPoller"/> has completed.
		/// </summary>
		/// <exception cref="InvalidOperationException">The proxy has not been started.</exception>
		public void Stop()
		{
			//volatile stat of stopping everything
			_isRunning = false;
			_ProcessSubscriptionsTask = null;
		}
	
		private void ProcessSubs(Int32 PubPort)
		{
			//TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 1);
			TimeSpan recieveTimeout = TimeSpan.Zero;

			using (_localSubscriberSocket = new SubscriberSocket())
			{
				using (_externalProxySubscriberSocket = new SubscriberSocket())
				{
					using (_publisherSocket = new PublisherSocket())
					{
						using(_externalPublisherSocket = new PublisherSocket())
						{
							_localSubscriberSocket.Options.ReceiveHighWatermark = 50000;
							_localSubscriberSocket.SubscribeToAnyTopic();

							_externalProxySubscriberSocket.Options.ReceiveHighWatermark = 50000;
							_externalProxySubscriberSocket.SubscribeToAnyTopic();


							_publisherSocket.Options.SendHighWatermark = 50000;
							string publishConnectionString = "tcp://0.0.0.0:" + PubPort.ToString();
							_publisherSocket.Bind(publishConnectionString);
							string externalPublishConnectionString = "tcp://0.0.0.0:" + (PubPort+1).ToString();
							_externalPublisherSocket.Bind(externalPublishConnectionString);

							Console.WriteLine($"Publishing on:{publishConnectionString}");
							Console.WriteLine($"External Publishing on:{externalPublishConnectionString}");
							Stopwatch sw = Stopwatch.StartNew();
							sw.Start();
							Int64 timeSinceLastMessage = 0;


							while (_isRunning)
							{

								if (_actionsToprocess.Count > 0)
								{
									while (_actionsToprocess.Count > 0)
									{
										if (_actionsToprocess.TryDequeue(out var action))
										{
											action();
										}

									}
								}
								//local proxy
								string messageTopicReceived = string.Empty;
								if (_localSubscriberSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
								{

									//we got something, reset!
									timeSinceLastMessage = sw.ElapsedMilliseconds;

									string messageReceived = _localSubscriberSocket.ReceiveFrameString();
									try
									{
										//write out to our one publisher
										_publisherSocket.SendMoreFrame(messageTopicReceived).SendFrame(messageReceived);
										if(_externalProxySubscriberCount > 0) _externalPublisherSocket.SendMoreFrame(messageTopicReceived).SendFrame(messageReceived);
									}
									catch (Exception ex)
									{

									}
								}
								//exteranl proxy
								if (_externalProxySubscriberSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
								{
									//we got something, reset!
									timeSinceLastMessage = sw.ElapsedMilliseconds;
									//we check to see if we shoudl propagate the user info, to prevent infinate loops
									string messageReceived = _externalProxySubscriberSocket.ReceiveFrameString();
									_publisherSocket.SendMoreFrame(messageTopicReceived).SendFrame(messageReceived);
									
								}
								if (sw.ElapsedMilliseconds - timeSinceLastMessage > 5)
								{
									//been 15ms since the last check, sleep for a bit.
									//means we got no messages from either proxy, or local, sleep for 1ms
									System.Threading.Thread.Sleep(1);

								}
							}
						}
		
					}
				}
			}
		}

	}
}
