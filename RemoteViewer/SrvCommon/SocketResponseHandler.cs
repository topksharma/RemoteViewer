using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SrvCommon
{
	public class MsgData
	{
		public Message.MESSAGE_ID Id { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
	}
	public class SocketResponseHandler
	{
		private readonly Socket _socket;
		private readonly ILogger _logger;
		private const int SCREEN_BUFFER_SIZE = 1024 * 64;
		private readonly byte[] _screenBuffer = new byte[SCREEN_BUFFER_SIZE];
		private const string BMP_FILENAME = @"c:\PraveenData\ss.jpg";
		private static readonly object ObjScreenLock = new object();

		private readonly Queue<MsgData> _msgQueue = new Queue<MsgData>();
		private readonly object _objQueueLocker = new object();

		public event EventHandler<ResponseEventArgs> ResponseReceived;
		public SocketResponseHandler(Socket socket, ILogger logger)
		{
			_socket = socket;
			_logger = logger;
			// add default MSG
			_msgQueue.Enqueue(new MsgData { Id = Message.MESSAGE_ID.GET_SCREEN });
		}

		public void AddMessage(MsgData msg)
		{
			lock (_objQueueLocker)
			{
				_msgQueue.Enqueue(msg);
			}
		}

		public MsgData GetMessage()
		{
			lock (_objQueueLocker)
			{
				if (_msgQueue.Count > 0)
				{
					return _msgQueue.Dequeue();
				}
			}

			return null;
		}

		public void HandleCommunication(object obj)
		{
			if (_socket != null)
			{
				try
				{
					while (_socket.IsConnected())
					{
						MsgData msgData = GetMessage();
						if (msgData == null || msgData.Id == Message.MESSAGE_ID.NONE)
						{
							SendMessage(new MsgData {Id = Message.MESSAGE_ID.GET_SCREEN});
						}
						else
						{
							SendMessage(msgData);
						}

						// read header first
						byte[] headerBytes = new byte[Message.MSG_HEADER_LENGTH];
						ReadMessageToken readMessageToken = new ReadMessageToken
						{
							Buffer = headerBytes,
							Event = new AutoResetEvent(false),
							Socket = _socket
						};
						IAsyncResult result = _socket.BeginReceive(headerBytes, 0, headerBytes.Length, SocketFlags.None,
							CallbackReadHeader, readMessageToken);

						result?.AsyncWaitHandle.WaitOne();
						readMessageToken.Event.WaitOne();

						if (readMessageToken.Buffer != null)
						{
							Message.MESSAGE_ID msgType = Message.GetMessageType(readMessageToken.Buffer);
							if (msgType == Message.MESSAGE_ID.SCREEN)
							{
								HandleScreenMsg(readMessageToken.Buffer);
							}
						}
					}
				}
				catch (Exception exception)
				{
					_logger.Log("HandleCommunication- " + exception);
				}
				finally
				{
					_socket.DoClose();
					(obj as AutoResetEvent).Set();
				}
			}
		}

		private void SendMessage(MsgData msgData)
		{
			byte[] buffer = null;
			switch (msgData.Id)
			{
				case Message.MESSAGE_ID.BUTTON:
					buffer = Message.CreateMessageWithInts(Message.MESSAGE_ID.BUTTON, new int[] { msgData.X });
					_socket.SendBytes(buffer);
					break;
				case Message.MESSAGE_ID.MOUSE:
					buffer = Message.CreateMessageWithInts(Message.MESSAGE_ID.MOUSE, new int[] { msgData.X, msgData.Y });
					_socket.SendBytes(buffer);
					break;
				case Message.MESSAGE_ID.GET_STATUS:
					break;
				case Message.MESSAGE_ID.STATUS:
					break;
				case Message.MESSAGE_ID.GET_SCREEN:
					_socket.SendBytes(Message.Create(msgData.Id));
					break;
			}
		}

		private string _fileOne = @"c:\PraveenData\ss.jpg";
		private string _fileTwO = @"c:\PraveenData\ss2.jpg";
		//private void HandleScreenMsg(byte[] buffer)
		//{
		//	int msgLength = Message.GetMessageLength(buffer);
		//	bool error = false;
		//	if (msgLength > 0)
		//	{
		//		FileStream fs = null;
		//		bool ok = false;
		//		try
		//		{
		//			try
		//			{
		//				fs = new FileStream(_fileOne, FileMode.Create);
		//				ok = true;
		//			}
		//			catch { }
		//			if (!ok)
		//			{
		//				fs = new FileStream(_fileTwO, FileMode.Create);
		//			}
		//			int totalBytesRead = 0;
		//			while (totalBytesRead < msgLength)
		//			{
		//				int bytesRead = _socket.Receive(_screenBuffer);
		//				totalBytesRead += bytesRead;
		//				fs.Write(_screenBuffer, 0, bytesRead);
		//			}
		//		}
		//		catch (Exception exception)
		//		{
		//			error = true;
		//			_logger.Log("HandleScreenMsg-" + exception);
		//		}
		//		finally
		//		{
		//			fs?.Close();
		//		}

		//		if (!error)
		//		{
		//			ResponseReceived?.Invoke(this, new ResponseEventArgs { FileName = ok ? _fileOne : _fileTwO });
		//		}
		//	}
		//}

		private void HandleScreenMsg(byte[] buffer)
		{
			int msgLength = Message.GetMessageLength(buffer);
			bool error = false;
			if (msgLength > 0)
			{
				byte[] imgBuffer = new byte[msgLength];
				try
				{
					int totalBytesRead = 0;
					while (totalBytesRead < msgLength)
					{
						int bytesRead = _socket.Receive(imgBuffer);
						totalBytesRead += bytesRead;
					}
				}
				catch (Exception exception)
				{
					error = true;
					_logger.Log("HandleScreenMsg-" + exception);
				}
				if (!error)
				{
					ByteArrayToImage(imgBuffer);
					ResponseReceived?.Invoke(this, new ResponseEventArgs { FileName = BMP_FILENAME});
				}
			}
		}

		private void ByteArrayToImage(byte[] byteArrayIn)
		{
			//using (var ms = new MemoryStream(byteArrayIn))
			//{
			//	return Image.FromStream(ms);
			//}
			try
			{
				MemoryStream ms = new MemoryStream(byteArrayIn);

				Image img = Image.FromStream(ms);
				img.Save(BMP_FILENAME, ImageFormat.Jpeg);

				img.Dispose();
			}
			catch{}
		}

		private void CallbackReadHeader(IAsyncResult ar)
		{
			ReadMessageToken readMessageToken = ar.AsyncState as ReadMessageToken;
			try
			{
				int bytesRead = readMessageToken.Socket.EndReceive(ar);
				if (bytesRead < readMessageToken.Buffer.Length)
				{
					while (bytesRead < readMessageToken.Buffer.Length)
					{
						bytesRead += readMessageToken.Socket.Receive(readMessageToken.Buffer, bytesRead,
							readMessageToken.Buffer.Length - bytesRead, SocketFlags.None);
					}
				}
			}
			catch
			{
				readMessageToken.Buffer = null;
			}
			finally
			{
				readMessageToken?.Event.Set();
			}
		}
	}

	public class ResponseEventArgs : EventArgs
	{
		public string FileName { get; set; }
		public Image Image { get; set; }
	}
}
