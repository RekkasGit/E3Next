using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E3NextProxy
{
	public class Proxy
	{
		private readonly NetMQSocket m_frontend;
		private readonly NetMQSocket m_backend;
		private readonly NetMQSocket m_controlIn;
		private readonly NetMQSocket m_controlOut;
		private INetMQPoller m_poller;
		private readonly bool m_externalPoller;

		private int m_state = StateStopped;

		private const int StateStopped = 0;
		private const int StateStarting = 1;
		private const int StateStarted = 2;
		private const int StateStopping = 3;
		public void AddSubBinding(string connectionString)
		{
			NetMQPoller tPoller = (NetMQPoller)m_poller;
			tPoller.Run(() =>
			{
				m_frontend.Connect(connectionString);
			});
		}
		public void RemoveSubBinding(string connectionString)
		{
			NetMQPoller tPoller = (NetMQPoller)m_poller;
			tPoller.Run(() =>
			{
				m_frontend.Disconnect(connectionString);
			});
		}

		/// <summary>
		/// Create a new instance of a Proxy (NetMQ.Proxy)
		/// with the given sockets to serve as a front-end, a back-end, and a control socket.
		/// </summary>
		/// <param name="frontend">the socket that messages will be forwarded from</param>
		/// <param name="backend">the socket that messages will be forwarded to</param>
		/// <param name="controlIn">this socket will have incoming messages also sent to it - you can set this to null if not needed</param>
		/// <param name="controlOut">this socket will have outgoing messages also sent to it - you can set this to null if not needed</param>
		/// <param name="poller">an optional external poller to use within this proxy</param>
		public Proxy(NetMQSocket frontend, NetMQSocket backend, NetMQSocket controlIn, NetMQSocket controlOut, INetMQPoller poller = null)
		{
			if (poller != null)
			{
				m_externalPoller = true;
				m_poller = poller;
			}

			m_frontend = frontend;
			m_backend = backend;
			m_controlIn = controlIn;
			m_controlOut = controlOut ?? controlIn;
		}

		/// <summary>
		/// Create a new instance of a Proxy (NetMQ.Proxy)
		/// with the given sockets to serve as a front-end, a back-end, and a control socket.
		/// </summary>
		/// <param name="frontend">the socket that messages will be forwarded from</param>
		/// <param name="backend">the socket that messages will be forwarded to</param>
		/// <param name="control">this socket will have messages also sent to it - you can set this to null if not needed</param>
		/// <param name="poller">an optional external poller to use within this proxy</param>
		/// <exception cref="InvalidOperationException"><paramref name="poller"/> is not <c>null</c> and either <paramref name="frontend"/> or <paramref name="backend"/> are not contained within it.</exception>
		public Proxy(NetMQSocket frontend, NetMQSocket backend, NetMQSocket control = null, INetMQPoller poller = null)
			: this(frontend, backend, control, null, poller)
		{ }

		/// <summary>
		/// Start proxying messages between the front and back ends. Blocks, unless using an external <see cref="NetMQPoller"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">The proxy has already been started.</exception>
		public void Start()
		{
			if (Interlocked.CompareExchange(ref m_state, StateStarting, StateStopped) != StateStopped)
				throw new InvalidOperationException("Proxy has already been started");

			m_frontend.ReceiveReady += OnFrontendReady;
			m_backend.ReceiveReady += OnBackendReady;

			if (m_externalPoller)
			{
				m_state = StateStarted;
			}
			else
			{
				m_poller = new NetMQPoller { m_frontend, m_backend };
				m_state = StateStarted;
				m_poller.Run();
			}
		}
		public void StartAsync()
		{
			if (Interlocked.CompareExchange(ref m_state, StateStarting, StateStopped) != StateStopped)
				throw new InvalidOperationException("Proxy has already been started");

			m_frontend.ReceiveReady += OnFrontendReady;
			m_backend.ReceiveReady += OnBackendReady;

			if (m_externalPoller)
			{
				m_state = StateStarted;
			}
			else
			{
				m_poller = new NetMQPoller { m_frontend, m_backend };
				m_state = StateStarted;
				m_poller.RunAsync();
			}
		}
		/// <summary>
		/// Stops the proxy, blocking until the underlying <see cref="NetMQPoller"/> has completed.
		/// </summary>
		/// <exception cref="InvalidOperationException">The proxy has not been started.</exception>
		public void Stop()
		{
			if (Interlocked.CompareExchange(ref m_state, StateStopping, StateStarted) != StateStarted)
				throw new InvalidOperationException("Proxy has not been started");

			if (!m_externalPoller)
			{
			
				m_poller.Stop();
				m_poller.Dispose();
				m_poller = null;
			}

			m_frontend.ReceiveReady -= OnFrontendReady;
			m_backend.ReceiveReady -= OnBackendReady;

			m_state = StateStopped;
		}

		private void OnFrontendReady(object sender, NetMQSocketEventArgs e) => ProxyBetween(m_frontend, m_backend, m_controlIn);
		private void OnBackendReady(object sender, NetMQSocketEventArgs e) => ProxyBetween(m_backend, m_frontend, m_controlOut);

		private static void ProxyBetween(IReceivingSocket from, IOutgoingSocket to, IOutgoingSocket control)
		{
			var msg = new Msg();
			msg.InitEmpty();

			var copy = new Msg();
			copy.InitEmpty();

			while (true)
			{
				from.Receive(ref msg);
				var more = msg.HasMore;

				if (control != null)
				{
					copy.Copy(ref msg);

					control.Send(ref copy, more);
				}

				to.Send(ref msg, more);

				if (!more)
					break;
			}

			copy.Close();
			msg.Close();

		}
	}
}
