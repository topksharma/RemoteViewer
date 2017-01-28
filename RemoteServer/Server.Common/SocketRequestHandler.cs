using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Server.Common;

namespace SrvCommon
{
	public class SocketRequestHandler
	{
		private readonly Socket _socket;
		private readonly ILogger _logger;
		private const int SCREEN_BUFFER_SIZE = 1024 * 64;
		private byte[] _screenBuffer;
		private const string BMP_FILENAME = @"internal/screen.jpg";
		private static readonly object ObjScreenLock = new object();

		public SocketRequestHandler(Socket socket, ILogger logger)
		{
			_socket = socket;
			_logger = logger;
		}

		public void HandleConnection(object obj)
		{
			if (_socket != null)
			{
				try
				{
					while (_socket.IsConnected())
					{
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

						result.AsyncWaitHandle.WaitOne();
						readMessageToken.Event.WaitOne();

						if (readMessageToken.Buffer != null)
						{
							Message.MESSAGE_ID msgType = Message.GetMessageType(readMessageToken.Buffer);
							int msgLength = Message.GetMessageLength(readMessageToken.Buffer);
							if (msgLength > 0)
							{
								byte[] msgData = new byte[msgLength];
								ReadCompleteMessage(msgData);
								HandleMessage(msgType, msgData);
							}
							else
							{
								HandleMessage(msgType, null);
							}
						}
					}
				}
				catch (Exception exception)
				{
					_logger.Log("HandleConnection- " + exception);
				}
				finally
				{
					_socket.DoClose();
				}
			}
		}

		private void ReadCompleteMessage(byte[] msgData)
		{
			int totalBytesToBeRead = msgData.Length;
			int bytesRead = 0;

			while (bytesRead < totalBytesToBeRead)
			{
				bytesRead += _socket.Receive(msgData, bytesRead, totalBytesToBeRead - bytesRead, SocketFlags.None);
			}
		}

		private void HandleMessage(Message.MESSAGE_ID msgType, byte[] msgData)
		{
			switch (msgType)
			{
				case Message.MESSAGE_ID.BUTTON:
					Debug.WriteLine(string.Format("BTN " + BitConverter.ToInt32(msgData, 0)));
					Win32Natives.keybd_event((byte)BitConverter.ToInt32(msgData, 0), 0, (int)Win32Natives.KeyActionState.Down, 0);
					Win32Natives.keybd_event((byte)BitConverter.ToInt32(msgData, 0), 0, (int)Win32Natives.KeyActionState.Up, 0);
					_socket.SendBytes(Message.Create(Message.MESSAGE_ID.OK, 0));
					break;
				case Message.MESSAGE_ID.MOUSE:
					int x = BitConverter.ToInt32(msgData, 0);
					int y = BitConverter.ToInt32(msgData, 4);
					break;
				case Message.MESSAGE_ID.NONE:
					break;
				case Message.MESSAGE_ID.GET_STATUS:
					break;
				case Message.MESSAGE_ID.STATUS:
					break;
				case Message.MESSAGE_ID.GET_SCREEN:
					lock (ObjScreenLock)
					{
						try
						{
							Bitmap bmpScreem = (Bitmap)ScreenCapture.CaptureDesktop();// NativeMethods.CaptureScreen(new Rectangle(0, 0, 272, 480));
							SendScreenBitmap(BitmapToByteArray(bmpScreem));

							//bmpScreem.Save(BMP_FILENAME, ImageFormat.Jpeg);
							//// send screen bitmap
							//SendScreenBitmap(BMP_FILENAME);
						}
						catch (Exception exception)
						{
							_socket.SendBytes(Message.Create(Message.MESSAGE_ID.NONE, 0));
							_logger.Log(string.Format("HandleMessage {0} {1}", msgType, exception));
						}
					}
					break;
				case Message.MESSAGE_ID.SCREEN:
					break;
			}
		}

		private byte[] BitmapToByteArray(Bitmap bmp)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				bmp.Save(ms, ImageFormat.Jpeg);
				return ms.ToArray();
			}
		}

		private void SendScreenBitmap(byte[] screenBytes)
		{
			try
			{
				long totalBytesToBeSent = screenBytes.Length;
				byte[] header = Message.Create(Message.MESSAGE_ID.SCREEN, (int)totalBytesToBeSent);
				_socket.SendBytes(header, header.Length);
				_socket.SendBytes(screenBytes);
			}
			catch (Exception exception)
			{
				_logger.Log("SendScreenBitmap - " + exception);
			}
		}

		private void SendScreenBitmap(string fileName)
		{
			FileStream fs = null;
			try
			{
				fs = new FileStream(fileName, FileMode.Open);
				long totalBytesToBeSent = fs.Length;
				int bytesSentSoFar = 0;

				byte[] header = Message.Create(Message.MESSAGE_ID.SCREEN, (int)totalBytesToBeSent);
				_socket.SendBytes(header, header.Length);
				if (totalBytesToBeSent < SCREEN_BUFFER_SIZE)
				{
					_screenBuffer = new byte[(int)totalBytesToBeSent];
				}
				else
				{
					_screenBuffer = new byte[SCREEN_BUFFER_SIZE];
				}

				int bytesReadInOneOperation = 0;
				while (bytesSentSoFar < totalBytesToBeSent)
				{
					bytesReadInOneOperation += fs.Read(_screenBuffer, bytesReadInOneOperation,
						_screenBuffer.Length - bytesReadInOneOperation);
					bytesSentSoFar += bytesReadInOneOperation;
					if (bytesReadInOneOperation >= _screenBuffer.Length)
					{
						_socket.SendBytes(_screenBuffer, bytesReadInOneOperation);
						// reset buffer
						bytesReadInOneOperation = 0;
						_screenBuffer = new byte[_screenBuffer.Length];
					}
				}
				if (bytesReadInOneOperation > 0)
				{
					_socket.SendBytes(_screenBuffer, bytesReadInOneOperation);
				}
			}
			catch (Exception exception)
			{
				_logger.Log("SendScreenBitmap - " + exception);
			}
			finally
			{
				if (fs != null)
				{
					fs.Close();
				}
			}
		}

		private void CallbackReadHeader(IAsyncResult ar)
		{
			ReadMessageToken readMessageToken = ar.AsyncState as ReadMessageToken;
			try
			{
				if (readMessageToken != null)
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
			}
			catch
			{
				if (readMessageToken != null)
				{
					readMessageToken.Buffer = null;
				}
			}
			finally
			{
				if (readMessageToken != null)
				{
					readMessageToken.Event.Set();
				}
			}
		}
	}
}
