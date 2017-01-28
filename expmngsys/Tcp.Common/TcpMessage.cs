using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Tcp.Common
{
	public class TcpMessage
	{
		public enum MsgTypeId : ushort
		{
			Text,
			File,
			None,
			Login,
			Login_Ack
		}
		// 4 bytes for msg Length
		// 2 bytes for msg Type
		public const int MESSAGE_HEADER_LENGTH = 6;
		public const int MESSAGE_LENGTH = 4;
		public const int MESSAGE_TYPE_LENGTH = 2;

		public static MsgTypeId GetMsgType(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return (MsgTypeId)BitConverter.ToInt16(data, MESSAGE_LENGTH);
			}
			return MsgTypeId.None;
		}
		public static int GetMsgLength(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return BitConverter.ToInt32(data, 0);
			}
			return -1;
		}
		private static byte[] CreateMessage(string msg)
		{
			byte[] msgBytes = Encoding.Default.GetBytes(msg);
			byte[] dataBytes = new byte[MESSAGE_HEADER_LENGTH + msgBytes.Length];
			byte[] msgLengthBytes = BitConverter.GetBytes(msgBytes.Length);
			msgLengthBytes.CopyTo(dataBytes, 0);
			msgBytes.CopyTo(dataBytes, MESSAGE_HEADER_LENGTH);
			return dataBytes;
		}

		public static byte[] CreateMessage(string msg, MsgTypeId msgTypeId)
		{
			byte[] msgBytes = CreateMessage(msg);
			byte[] msgTypeData = BitConverter.GetBytes((ushort)msgTypeId);
			msgTypeData.CopyTo(msgBytes, MESSAGE_LENGTH);
			return msgBytes;
		}
		public static byte[] CreateMessage(byte[] msgBytes, MsgTypeId msgTypeId)
		{
			byte[] dataBytes = new byte[MESSAGE_HEADER_LENGTH + msgBytes.Length];
			byte[] msgLengthBytes = BitConverter.GetBytes(msgBytes.Length);
			msgLengthBytes.CopyTo(dataBytes, 0);

			byte[] msgTypeData = BitConverter.GetBytes((ushort)msgTypeId);
			msgTypeData.CopyTo(dataBytes, MESSAGE_LENGTH);

			msgBytes.CopyTo(dataBytes, MESSAGE_HEADER_LENGTH);

			return dataBytes;
		}
		private class ServerData
		{
			public ResponseType Response { get; set; }
			public AutoResetEvent Event { get; set; }
			public byte[] Data { get; set; }
		}
		public static ResponseType WaitFor_Login_Response(byte[] dataBytes, int timeoutMs)
		{
			AutoResetEvent signal = new AutoResetEvent(false);
			ServerData serverData = new ServerData() { Data = dataBytes, Event = signal };
			ThreadPool.QueueUserWorkItem(WaitInternal, serverData);
			bool timedOut = signal.WaitOne(timeoutMs);
			if (!timedOut)
			{
				serverData.Response = ResponseType.Timeout;
			}
			return serverData.Response;
		}

		private static void WaitInternal(object state)
		{
			ServerData data = state as ServerData;
			if (data.IsNotNull())
			{
				Socket sockServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					EndPoint endPoint = new IPEndPoint(IPAddress.Parse("10.0.0.10"), TcpCommon.LOGIN_SERVER_PORT);
					sockServer.Connect(endPoint);
					// all is well, send data
					sockServer.Send(data.Data);
					// wait for response
					byte[] response = new byte[64];
					int numOfBytesReceived = sockServer.Receive(response, SocketFlags.None);
					if (numOfBytesReceived >= MESSAGE_HEADER_LENGTH)
					{
						if (GetMsgType(response) == MsgTypeId.Login_Ack)
						{
							data.Response = GetResponseType(response);
						}
					}
				}
				catch (Exception)
				{
					data.Response = ResponseType.Error;
				}
				finally
				{
					data.Event.Set();
					sockServer.Shutdown(SocketShutdown.Both);
					sockServer.Close();
				}
			}
		}
		private static ResponseType GetResponseType(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH + 2)
			{
				return (ResponseType)BitConverter.ToUInt16(data, MESSAGE_HEADER_LENGTH);
			}
			return ResponseType.NOK;
		}
		public class SocketReadData
		{
			public Socket RefSocket { get; set; }
			public byte[] Data { get; set; }
			public EndPoint EndPoint;
			public AutoResetEvent Event { get; set; }
			public int NumOfBytesRead { get; set; }
		}
		public static SocketReadData ReadAsync(Socket server, EndPoint serverEndPoint, byte[] data, AutoResetEvent signal)
		{
			SocketReadData socketReadData = new SocketReadData()
			{
				Data = data,
				EndPoint = serverEndPoint,
				Event = signal,
				RefSocket = server,
				NumOfBytesRead = -1
			};

			ThreadPool.QueueUserWorkItem(ReadWithParam, socketReadData);
			return socketReadData;
		}
		private static void ReadWithParam(object state)
		{
			SocketReadData data = state as SocketReadData;
			if (data != null)
			{
				int numOfBytesRead;
				try
				{
					numOfBytesRead = data.RefSocket.ReceiveFrom(data.Data, ref data.EndPoint);
					while (numOfBytesRead < data.Data.Length)
					{
						numOfBytesRead += data.RefSocket.ReceiveFrom(data.Data, numOfBytesRead, data.Data.Length - numOfBytesRead, SocketFlags.None,
							ref data.EndPoint);
					}
				}
				catch (SocketException socEx)
				{
					Console.WriteLine($"error {socEx.Message}");
					numOfBytesRead = -1;
				}
				data.NumOfBytesRead = numOfBytesRead;
				data.Event.Set();
			}
		}
		public static int Read(Socket server, EndPoint serverEndPoint, byte[] data)
		{
			int numOfBytesRead = -1;
			try
			{
				numOfBytesRead = server.ReceiveFrom(data, ref serverEndPoint);
				while (numOfBytesRead < data.Length)
				{
					numOfBytesRead += server.ReceiveFrom(data, numOfBytesRead, data.Length - numOfBytesRead, SocketFlags.None, ref serverEndPoint);
				}
			}
			catch (SocketException socEx)
			{
				Console.WriteLine($"error {socEx.Message}");
				numOfBytesRead = -1;
			}
			return numOfBytesRead;
		}
	}
	public class SocketData
	{
		public Socket RefSocket { get; set; }
		public byte[] Data { get; set; }
		public bool IsMsgLengthRead { get; set; }
		public int MsgLength { get; set; }
		public int NumBytesRead { get; set; }
		public TcpMessage.MsgTypeId MsgType { get; set; }
	}
}
