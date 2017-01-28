using System;
using System.Net.Sockets;

namespace SrvCommon
{
	public static class Extensions
	{
		public static bool IsConnected(this Socket socket)
		{
			if (socket != null)
			{
				bool isDisconnected = socket.Poll(10, SelectMode.SelectRead) && socket.Available == 0;
				return !isDisconnected;
			}
			return false;
		}

		public static void DoClose(this Socket socket)
		{
			try
			{
				if (socket != null)
				{
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
			}
			catch { }
		}

		public static void SendBytes(this Socket socket, byte[] buffer, int offset, int size)
		{
			try
			{
				if (socket.IsConnected())
				{
					int bytesToSend = size;
					int bytesSent = 0;
					while (bytesSent < bytesToSend)
					{
						bytesSent += socket.Send(buffer, (bytesSent + offset), size - bytesSent, SocketFlags.None);
					}
				}
			}
			catch { }
		}

		public static void SendBytes(this Socket socket, byte[] buffer)
		{
			socket.SendBytes(buffer, 0, buffer.Length);
		}

		public static void SendBytes(this Socket socket, byte[] buffer, int size)
		{
			socket.SendBytes(buffer, 0, size);
		}
	}
}
