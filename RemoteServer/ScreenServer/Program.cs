using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SrvCommon;

namespace SrvRemote
{
	class Program
	{
		private const int BACKLOG_CONNECTIONS = 5;
		private static Socket _serverSocket;
		private static readonly AutoResetEvent _mainThreadWaitSignal = new AutoResetEvent(false);
		private static readonly ILogger Logger = new ConsoleLogger();

		static void Main(string[] args)
		{
			EndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, Util.SERVER_PORT);
			_serverSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_serverSocket.Bind(serverEndPoint);
			_serverSocket.Listen(BACKLOG_CONNECTIONS);
			Console.WriteLine(string.Format("server is listening for connections at: {0} {1}", Util.GetIPAddress(), Util.SERVER_PORT));
			ThreadPool.QueueUserWorkItem(StartListening);
			_mainThreadWaitSignal.WaitOne();
		}

		private static void StartListening(object state)
		{
			while (true)
			{
				AcceptSocketToken acceptSocketToken = new AcceptSocketToken()
													 {
														 Event = new AutoResetEvent(false)
													 };
				IAsyncResult result = _serverSocket.BeginAccept(CallbackAccept, acceptSocketToken);
				result.AsyncWaitHandle.WaitOne();
				acceptSocketToken.Event.WaitOne();
				// got socket, give it to request handler
				if (acceptSocketToken.Socket != null)
				{
					SocketRequestHandler requestHandler = new SocketRequestHandler(acceptSocketToken.Socket, Logger);
					ThreadPool.QueueUserWorkItem(requestHandler.HandleConnection);
				}
			}
		}

		private static void CallbackAccept(IAsyncResult ar)
		{
			AcceptSocketToken acceptSocketToken = ar.AsyncState as AcceptSocketToken;
			if (acceptSocketToken != null)
			{
				try
				{
					Socket clientSocket = _serverSocket.EndAccept(ar);
					// set this sockt into the token
					acceptSocketToken.Socket = clientSocket;
				}
				catch (SocketException socketException)
				{
					acceptSocketToken.Socket = null;
					Logger.Log("CallbackAccept- " + socketException);
				}
				catch (Exception exception)
				{
					acceptSocketToken.Socket = null;
					Logger.Log("CallbackAccept- " + exception);
				}
				finally
				{
					acceptSocketToken.Event.Set();
				}
			}

		}
	}
}
