using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Tcp.Common;

namespace HttpServer
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	class Program
	{
		private static Socket _serverSocket;
		static void Main()
		{
			
			Logger.LogFileName = "http_server_log.txt";
			Logger.Create();

			//string path = @"c:\praveendata\test.txt";
			//byte[] data = Encoding.ASCII.GetBytes("hello how are you ? I am good, what about you. Are you coming for the movie.");
			//int count = 0;
			//int it = 50000 / data.Length;
			//FileStream fs = File.Create(path);
			//for (int i = 0; i < it + 2; i++)
			//{
			//	fs.Write(data, 0, data.Length);
			//	count += data.Length;
			//}
			//fs.Close();
			//Debug.WriteLine("count = " + count);

			//GET / home HTTP / 1.1
			//Host: 127.0.0.1:8888
			//Connection: keep - alive
			//Upgrade - Insecure - Requests: 1
			//User - Agent: Mozilla / 5.0(Windows NT 10.0; WOW64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 51.0.2704.84 Safari / 537.36
			//Accept: text / html,application / xhtml + xml,application / xml; q = 0.9,image / webp,*/*;q=0.8
			//Accept-Encoding: gzip, deflate, sdch
			//Accept-Language: en-GB,en-US;q=0.8,en;q=0.6
			//Cookie: _ga=GA1.4.772899385.1465210155
			//string str = "GET /home HTTP / 1.1";
			//string[] ss = str.Split(' ');
			try
			{
				//C:\PraveenData\develop\csharptests\HttpServer\bin\Debug\HttpServer.exe
				string assembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				assembly = Directory.GetParent(assembly).Parent.FullName;
				HttpRequest.ROOT_FOLDER = Path.Combine(assembly, "wwwroot");
				HttpRequest.DATABASE_FOLDER = Path.Combine(assembly, "database");

				string databaseFileName = Path.Combine(HttpRequest.DATABASE_FOLDER, "users.xml");
				DbManager.GetInstance().LoadUsersFromFile(databaseFileName);
				_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				EndPoint endPoint = new IPEndPoint(IPAddress.Any, TcpCommon.HTTP_SERVER_PORT);
				_serverSocket.Bind(endPoint);
				_serverSocket.Listen(5);

				HttpRequest.ServerIP = Utils.GetIpAddress();
				Console.WriteLine($"http server is running on: {Utils.GetIpAddresses()}:{TcpCommon.HTTP_SERVER_PORT}");
				Console.WriteLine("!!! press 'q' to exit !!!");
				_serverSocket.BeginAccept(CallBackBeginAccept, _serverSocket);
			}
			catch (SocketException exception)
			{
				if (exception.ErrorCode == 10048)
				{
					Console.WriteLine("server is already running");
				}
			}
			catch (Exception)
			{
				Console.WriteLine("ERROR");
			}
			string q = Console.ReadLine();
			while (q != null && !q.Equals("q"))
			{
				Console.WriteLine($"you entered '{q}', press 'q' if you want to exit.");
				q = Console.ReadLine();
			}
		}
		private static void CallBackBeginAccept(IAsyncResult ar)
		{
			Socket serverSocket = ar.AsyncState as Socket;
			if (serverSocket != null)
			{
				Socket clientSocket = serverSocket.EndAccept(ar);
				ThreadPool.QueueUserWorkItem(ServeHttpClient, clientSocket);
				// go back and listen for another client
				serverSocket.BeginAccept(CallBackBeginAccept, serverSocket);
			}
		}
		private static void ServeHttpClient(object state)
		{
			bool isWebSocket = false;
			Socket socket = state as Socket;
			try
			{
				if (socket.IsSocketConnected())
				{
					try
					{
						string msg = TcpCommon.ReadLine(socket);
						HttpRequest httpRequest = new HttpRequest();
						msg.ParseHttpCommand(httpRequest);
						while (!string.IsNullOrEmpty(msg))
						{
							Console.WriteLine(msg);
							msg = TcpCommon.ReadLine(socket);
							msg.AddRequestHeader(httpRequest);
						}
						if (httpRequest.HttpVerb == HttpRequest.HttpVerbID.POST)
						{
							int contentLength = httpRequest.GetContentLength();
							if (contentLength > 0)
							{
								//ReadFromSocket(socket, postDataBytes);
								//httpRequest.PostData = Encoding.ASCII.GetString(postDataBytes, 0, postDataBytes.Length);
								if (httpRequest.URL.Contains("entry_data") && httpRequest.GetHeaderValue("Content-Type").Trim() == "multipart/form-data")
								{
									httpRequest.HandleUserEntryData(socket);
									//string boundary = ReadLine(socket);
									//string line = boundary;
									//while (line != boundary + "--")
									//{
									//	if (line.Contains("Content-Type"))
									//	{
									//		line = ReadLine(socket);
									//		FileStream fs = new FileStream(@"c:\userdata\data.jpg", FileMode.OpenOrCreate);
									//		while (line != boundary && line != boundary + "--")
									//		{
									//			line = ReadLine(socket);
									//			byte[] data = Encoding.UTF8.GetBytes(line);
									//			fs.Write(data, 0, data.Length);
									//		}
									//		fs.Close();

									//	}
									//	line = ReadLine(socket);
									//}
									//httpRequest.PostData = Encoding.ASCII.GetString(postDataBytes, 0, postDataBytes.Length);
									//}
									//if (!httpRequest.URL.Contains("entry_data") && postDataBytes.Length < 2 * 1024 * 1024)
									//{
									//for (int i = 0; i < postDataBytes.Length; i++)
									//{
									//	Debug.Write((char)postDataBytes[i]);
									//}
								}
								else if (httpRequest.URL.Contains("profile_data")
									&& httpRequest.GetHeaderValue("Content-Type").Trim() == "multipart/form-data")
								{
									httpRequest.HandleUserProfileData(socket);
								}
								else
								{
									byte[] postDataBytes = new byte[contentLength];
									httpRequest.PostDataBytes = postDataBytes;
									ReadFromSocket(socket, postDataBytes);
									httpRequest.PostData = Encoding.ASCII.GetString(postDataBytes, 0, postDataBytes.Length);
								}
							}
						}
						if (httpRequest.GetHeaderValue("Upgrade") == "websocket")
						{
							// websocket detected
							httpRequest.SendWebSocketHandshake(socket);
							isWebSocket = true;
							string remoteAddress = socket.RemoteEndPoint.ToString().Split(':')[0];
							DictUsers[remoteAddress] = new UserData()
							{
								RefSocket = socket,
								RemoteAddress = remoteAddress
							};
						}
						else
						{
							httpRequest.ServeHttp(socket);
						}
					}
					catch (TimeoutException)
					{
					}
				}
			}
			finally
			{
				if (!isWebSocket)
				{
					socket.CloseSocket();
				}
			}
		}

		public static Dictionary<string, UserData> DictUsers = new Dictionary<string, UserData>();

		public static void UpdateUser(string remoteClient, string userName)
		{
			UserData userData;
			if (DictUsers.TryGetValue(remoteClient, out userData))
			{
				userData.UserName = userName;
			}
		}
		public static void SendResponse(string msg, string userName)
		{

		}
		public static void ReadFromSocket(Socket clientSocket, byte[] data)
		{
			int recLength = 0;
			while (clientSocket.IsSocketConnected() && recLength < data.Length)
			{
				recLength += clientSocket.Receive(data);
			}
		}
	}
}
