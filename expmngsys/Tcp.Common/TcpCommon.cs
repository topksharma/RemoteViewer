using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace Tcp.Common
{
	public enum ResponseType : ushort
	{
		OK,
		NOK,
		Timeout,
		Error
	}
	public class UserData
	{
		public string RemoteAddress { get; set; }
		public Socket RefSocket { get; set; }
		public string UserName { get; set; }
	}
	public class TcpFileMessage
	{
		public enum MsgTypeId : byte
		{
			Text,
			File,
			None,
			Login,
			Login_Ack
		}
		public enum FileMsgTypeId : byte
		{
			Header,
			Block,
			Close,
			None
		}

		public const int MESSAGE_HEADER_LENGTH = 8;
		public const int MESSAGE_LENGTH = 4;
		public const int MESSAGE_TYPE_LENGTH = 2;
		// Indexes
		public const int MSG_LENGTH_INDEX = 4;
		public const int FILE_ID_INDEX = 2;

		public static MsgTypeId GetMsgType(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return (MsgTypeId)data[0];
			}
			return MsgTypeId.None;
		}

		public static FileMsgTypeId GetFileMsgType(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return (FileMsgTypeId)data[1];
			}
			return FileMsgTypeId.None;
		}
		public static UInt32 GetMsgLength(byte[] data)
		{
			if (data != null && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return BitConverter.ToUInt32(data, MSG_LENGTH_INDEX);
			}
			return 0;
		}

		public static UInt16 GetFileId(byte[] data)
		{
			if (data.IsNotNullOrEmpty() && data.Length >= MESSAGE_HEADER_LENGTH)
			{
				return BitConverter.ToUInt16(data, FILE_ID_INDEX);
			}
			return 0;
		}

		public static UInt16 GetFileNameLength(byte[] data)
		{
			if (data.IsNotNullOrEmpty() && data.Length >= FILE_NAME_LENGTH)
			{
				return BitConverter.ToUInt16(data, 0);
			}
			return 0;
		}

		public const int FILE_NAME_LENGTH = 2;

		public static UInt64 GetFileLength(ushort fileNameLength, byte[] data)
		{
			if (data.IsNotNullOrEmpty() && data.Length >= (FILE_NAME_LENGTH + fileNameLength))
			{
				return BitConverter.ToUInt64(data, FILE_NAME_LENGTH + fileNameLength);
			}
			return 0;
		}

		public static byte[] CreateFileHeader(string getFileName, ushort fileId, ulong fileSize)
		{
			byte[] headerBytes = new byte[MESSAGE_HEADER_LENGTH];
			headerBytes[0] = (byte)MsgTypeId.File;
			headerBytes[1] = (byte)FileMsgTypeId.Header;

			byte[] fileIdBytes = BitConverter.GetBytes(fileId);
			fileIdBytes.CopyTo(headerBytes, FILE_ID_INDEX);
			// Create File Header

			// 2 + FileName + 8 (FileSize)
			byte[] fileNameBytes = Encoding.ASCII.GetBytes(getFileName);

			byte[] fileNameLengthBytes = BitConverter.GetBytes((UInt16)fileNameBytes.Length);

			byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

			UInt32 msgLength = (UInt32)(fileNameBytes.Length + fileNameLengthBytes.Length + fileSizeBytes.Length);
			byte[] msgLengthBytes = BitConverter.GetBytes(msgLength);
			//
			msgLengthBytes.CopyTo(headerBytes, MSG_LENGTH_INDEX);
			// create final array
			byte[] finalBytes = new byte[headerBytes.Length + msgLength];
			// copy header
			int index = 0;
			headerBytes.CopyTo(finalBytes, index);
			index += headerBytes.Length;
			// right after header are 2 bytes for file name length
			fileNameLengthBytes.CopyTo(finalBytes, index);
			index += fileNameLengthBytes.Length;
			// filename itself
			fileNameBytes.CopyTo(finalBytes, index);
			index += fileNameBytes.Length;
			// file length size
			fileSizeBytes.CopyTo(finalBytes, index);

			return finalBytes;
		}

		public static byte[] CreateFileMsgData(byte[] dataBytes, UInt32 numOfBytes, FileMsgTypeId fileMsg, UInt16 fileId)
		{
			byte[] headerBytes = new byte[MESSAGE_HEADER_LENGTH];
			headerBytes[0] = (byte)MsgTypeId.File;
			headerBytes[1] = (byte)fileMsg;

			byte[] fileIdBytes = BitConverter.GetBytes(fileId);
			fileIdBytes.CopyTo(headerBytes, FILE_ID_INDEX);

			byte[] msgLengthBytes = BitConverter.GetBytes(numOfBytes);
			msgLengthBytes.CopyTo(headerBytes, MSG_LENGTH_INDEX);

			byte[] finalDataBytes = new byte[MESSAGE_HEADER_LENGTH + numOfBytes];
			headerBytes.CopyTo(finalDataBytes, 0);
			Array.Copy(dataBytes, 0, finalDataBytes, MESSAGE_HEADER_LENGTH, numOfBytes);

			return finalDataBytes;
		}
	}

	public class TcpFileData
	{
		public ulong FileLength { get; set; }
		public ulong FileBytesRead { get; set; }
		public Socket RefSocket { get; set; }
		public byte[] Data { get; set; }
		public bool IsHeaderRead { get; set; }
		public ulong NumOfBytesRead { get; set; }
		public TcpFileMessage.MsgTypeId MsgType { get; set; }
		public TcpFileMessage.FileMsgTypeId FileMsgType { get; set; }
		public uint MsgLength { get; set; }
		public UInt32 NumOfBytesToRead { get; set; }
		public string FileName { get; set; }
		public ushort FileId { get; set; }

		public TcpFileData()
		{
			Data = new byte[TcpFileMessage.MESSAGE_HEADER_LENGTH];
		}

		public void Reset()
		{
			NumOfBytesRead = 0;
			NumOfBytesToRead = 0;
		}

		public string DirToStoreData { get; set; }

		public TextWriter LogOut { get; set; }

		private string _rootFolder = @"c:\PraveenData\userdata";
		private FileStream _fsWriter;
		public void CreateFileStream()
		{
			if (!string.IsNullOrEmpty(FileName))
			{
				string filePath = Path.Combine(this._rootFolder, FileName);
				string dirPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(dirPath))
				{
					Directory.CreateDirectory(dirPath);
				}
				this._fsWriter = File.Create(filePath);
			}
		}
		public void WriteBytes()
		{
			if (this._fsWriter != null)
			{
				this._fsWriter.Write(Data, 0, Data.Length);
			}
		}

		public void ResetFileData()
		{
			this.Reset();
			IsHeaderRead = false;
			FileMsgType = TcpFileMessage.FileMsgTypeId.None;
			MsgType = TcpFileMessage.MsgTypeId.None;
			Data = new byte[TcpFileMessage.MESSAGE_HEADER_LENGTH];
			NumOfBytesToRead = (uint)Data.Length;
		}

		public void CloseFile()
		{
			if (this._fsWriter != null)
			{
				this._fsWriter.Close();
				this._fsWriter = null;
				FileName = string.Empty;
				FileLength = 0;
				FileBytesRead = 0;
			}
		}

		public TcpFileData CreateNew()
		{
			TcpFileData tcpFileData = new TcpFileData
			{
				FileName = this.FileName,
				FileLength = this.FileLength,
				FileBytesRead = this.FileBytesRead,
				MsgType = this.MsgType,
				FileMsgType = this.FileMsgType,
				IsHeaderRead = this.IsHeaderRead,
				Data = new byte[this.Data.Length],
				NumOfBytesRead = this.NumOfBytesRead,
				NumOfBytesToRead = this.NumOfBytesToRead,
				RefSocket = this.RefSocket,
				_fsWriter = this._fsWriter,
				_rootFolder = this._rootFolder,
				MsgLength = this.MsgLength,
				FileId = FileId
			};

			return tcpFileData;
		}
	}

	public class TcpFileSender
	{
		private class FileSendData
		{
			public string FileName { get; set; }
			public Socket RefSocket { get; set; }
			public string UserDataFolder { get; set; }
			private FileStream _fsRead;
			private long _fileLength;
			private long _numOfBytesSend;
			private long _numOfBytesRead;
			private byte[] _data;
			public const int MAX_READ_LENGTH = 1024 * 4;
			private UInt16 _thisFileId;
			private TcpFileSender _tcpFileSender;

			public FileSendData(TcpFileSender tcpFileSender)
			{
				this._tcpFileSender = tcpFileSender;
			}
			public void Start()
			{
				this._fsRead = new FileStream(FileName, FileMode.Open, FileAccess.Read);
				this._fileLength = this._fsRead.Length;

				//if (this._fileLength <= MAX_READ_LENGTH)
				//{
				//	this._data = new byte[this._fileLength];
				//}
				//else
				//{
				//	this._data = new byte[MAX_READ_LENGTH];
				//}
				int dataLength = this.GetNextDataLength();
				this._data = new byte[dataLength];
				// send header with blocking mode
				this._thisFileId = CreateFileId();
				byte[] headerBytes = TcpFileMessage.CreateFileHeader(Path.GetFileName(FileName).Replace(UserDataFolder, ""), this._thisFileId, (ulong)this._fileLength);
				RefSocket.SendBytes(headerBytes);
				// go for file read
				//this._fsRead.BeginRead(this._data, 0, this._data.Length, ReadCallback, this);

				long numOfBytesToRead = this._fileLength;
				long numOfBytesRead = 0;

				//numOfBytesRead += (ulong)this._fsRead.Read(this._data, (int)numOfBytesRead, _data.Length - (int)numOfBytesRead);

				//RefSocket.SendBytes(headerBytes);
				byte[] msgBlock;
				while (numOfBytesRead < numOfBytesToRead)
				{
					int nRead = this._fsRead.Read(this._data, 0, this._data.Length);
					numOfBytesRead += nRead;
					if (numOfBytesRead < numOfBytesToRead)
					{
						// block
						msgBlock = TcpFileMessage.CreateFileMsgData(this._data, (UInt32)nRead, TcpFileMessage.FileMsgTypeId.Block, this._thisFileId);
						Interlocked.Add(ref this._tcpFileSender._totalBytesTransferedSoFar, nRead);
					}
					else
					{
						// close
						msgBlock = TcpFileMessage.CreateFileMsgData(this._data, (UInt32)nRead, TcpFileMessage.FileMsgTypeId.Close, this._thisFileId);
						Interlocked.Add(ref this._tcpFileSender._totalBytesTransferedSoFar, nRead);
					}
					if (msgBlock != null)
					{
						RefSocket.SendBytes(msgBlock);
						this._tcpFileSender.UpdateProgress(nRead);
					}
					msgBlock = null;
				}
			}

			private static UInt16 _fileId;
			public static UInt16 CreateFileId()
			{
				if (_fileId == UInt16.MaxValue)
				{
					_fileId = 0;
				}
				return ++_fileId;
			}
			//private void ReadCallback(IAsyncResult ar)
			//{
			//	try
			//	{
			//		int numOfBytesRead = this._fsRead.EndRead(ar);
			//		this._numOfBytesRead += numOfBytesRead;
			//		if (numOfBytesRead > 0)
			//		{
			//			byte[] msgBlock;
			//			// create Msg & send it
			//			if (this._numOfBytesRead < this._fileLength)
			//			{
			//				// Block
			//				msgBlock = TcpFileMessage.CreateFileMsgData(this._data, (UInt32)numOfBytesRead, TcpFileMessage.FileMsgTypeId.Block, this._thisFileId);
			//				// send data to client
			//				RefSocket.SendBytes(msgBlock);
			//				// go for next file read
			//				this._fsRead.BeginRead(this._data, 0, this.GetNextDataLength(), ReadCallback, this);
			//			}
			//			else if (this._numOfBytesRead == this._fileLength)
			//			{
			//				// Close
			//				msgBlock = TcpFileMessage.CreateFileMsgData(this._data, (UInt32)numOfBytesRead, TcpFileMessage.FileMsgTypeId.Close, this._thisFileId);
			//				RefSocket.SendBytes(msgBlock);
			//				CloseFile();
			//			}
			//		}
			//	}
			//	catch (Exception exception)
			//	{
			//		Logger.Exception(exception);
			//		this.CloseFile();
			//	}
			//}

			private int GetNextDataLength()
			{
				if ((this._numOfBytesRead + MAX_READ_LENGTH) > this._fileLength)
				{
					return (int)(this._fileLength - this._numOfBytesRead);
				}
				return MAX_READ_LENGTH;
			}

			private void CloseFile()
			{
				if (this._fsRead != null)
				{
					this._fsRead.Close();
				}
			}
		}
		public const int PROGRESS_REPORT_THRESHOLD = 2 * 1024;
		private int _fileProgressUpdater;
		private void UpdateProgress(int currentBytesTransferred)
		{
			_fileProgressUpdater += currentBytesTransferred;
			if (this._fileProgressUpdater >= PROGRESS_REPORT_THRESHOLD)
			{
				this._fileProgressUpdater -= PROGRESS_REPORT_THRESHOLD;
				//
				double percentage = ((double)_totalBytesTransferedSoFar / (double)this._totalSize) * 100.0;
				WebSocket?.SendWebSocketData(FileUpdateToJSON((int)percentage));
			}
		}

		private string FileUpdateToJSON(int percentage)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"MsgId\": \"FilePercentage\"");
			sb.Append($",\"Value\": \"{percentage}%\"");
			sb.Append("}");
			return sb.ToString();
		}
		public Socket GetSocket()
		{
			return this._socket;
		}
		private string[] _fileNames;
		private Socket _socket;

		private List<FileSendData> _lstFileSendData;
		public TcpFileSender(Socket socket, string[] fileNames)
		{
			if (socket == null || !fileNames.IsNotNullOrEmpty())
			{
				throw new ArgumentNullException($"{nameof(socket)} or {nameof(fileNames)} is null.");
			}
			this._fileNames = fileNames;
			this._socket = socket;
		}

		public string UserData { get; set; }
		public Socket WebSocket { get; set; }

		~TcpFileSender()
		{
			this._socket.CloseSocket();
		}

		private long _totalSize;
		private long _totalBytesTransferedSoFar;
		public void StartSending()
		{
			this._totalBytesTransferedSoFar = 0;
			this._totalSize = 0;
			this._lstFileSendData = new List<FileSendData>(this._fileNames.Length);
			for (int i = 0; i < this._fileNames.Length; i++)
			{
				this._lstFileSendData.Add(new FileSendData(this)
				{
					FileName = this._fileNames[i],
					RefSocket = this._socket,
					UserDataFolder = UserData
				});
				this._totalSize += new FileInfo(this._fileNames[i]).Length;
			}

			this._lstFileSendData.ForEach(fData => { fData.Start(); });
			WebSocket?.SendWebSocketData(FileUpdateToJSON(100));
			Thread.Sleep(5000);
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"MsgId\": \"FilePercentage\"");
			sb.Append($",\"Value\": \"DONE\"");
			sb.Append("}");
			WebSocket?.SendWebSocketData(sb.ToString());
		}

		public void AddFiles(string[] fileNames)
		{
			this._fileNames = fileNames;
		}
	}
	public class TcpFileReceiver
	{

		private Socket _commSocket;
		public TcpFileReceiver(Socket socket)
		{
			if (socket == null)
			{
				throw new ArgumentNullException("socket can't be null");
			}
			this._commSocket = socket;
		}

		public TextWriter LogOut { get; set; }
		private byte[] _data;

		~TcpFileReceiver()
		{
			this._commSocket.CloseSocket();
		}
		public void StartReceiving()
		{
			LogOut?.WriteLine($" file receiving started...");
			this._data = new byte[1024 * 8];
			try
			{
				while (this._commSocket.IsSocketConnected())
				{
					this.ReadHeader();
					TcpFileMessage.MsgTypeId msgType = TcpFileMessage.GetMsgType(this._data);
					if (msgType != TcpFileMessage.MsgTypeId.File)
					{
						throw new InvalidOperationException($"invalid message type {msgType}");
					}
					TcpFileMessage.FileMsgTypeId fileMsgType = TcpFileMessage.GetFileMsgType(this._data);
					uint msgLength = TcpFileMessage.GetMsgLength(this._data);
					ushort fileId = TcpFileMessage.GetFileId(this._data);

					ReadTcpMessage(msgLength);
					FileStream fs = null;
					switch (fileMsgType)
					{
						case TcpFileMessage.FileMsgTypeId.Header:
							UInt16 fileNameLength = TcpFileMessage.GetFileNameLength(_data);
							string fileName = Encoding.ASCII.GetString(_data, TcpFileMessage.FILE_NAME_LENGTH, fileNameLength);
							AddFileData(fileName, fileId);
							LogOut?.WriteLine($"receiving file {fileName}");
							Logger.Log($"{fileId}, {msgLength} {fileMsgType}", "<");
							break;
						case TcpFileMessage.FileMsgTypeId.Block:
							//LogOut?.WriteLine($"{fileId}, {msgLength} {fileMsgType}");
							Logger.Log($"{fileId}, {msgLength} {fileMsgType}", "<");
							fs = GetTargetStream(fileId);
							if (fs != null)
							{
								fs.Write(this._data, 0, (int)msgLength);
							}
							break;
						case TcpFileMessage.FileMsgTypeId.Close:
							LogOut?.WriteLine($"{fileId}, {msgLength} {fileMsgType}");
							Logger.Log($"{fileId}, {msgLength} {fileMsgType}", "<");
							fs = GetTargetStream(fileId);
							if (fs != null)
							{
								fs.Write(this._data, 0, (int)msgLength);
							}
							LogOut?.WriteLine($"finished file {fileId}");
							RemoveFileData(fileId);
							break;
						case TcpFileMessage.FileMsgTypeId.None:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
			catch (Exception exception)
			{
				LogOut.WriteLine(exception.Message);
				this._commSocket.CloseSocket();
				foreach (var fileId in this._dictOfFileStreams.Keys)
				{
					FileStream fs = this._dictOfFileStreams[fileId];
					if (fs != null)
					{
						fs.Close();
					}
				}
			}
		}

		private void RemoveFileData(ushort fileId)
		{
			FileStream fs = GetTargetStream(fileId);
			if (fs != null)
			{
				fs.Close();
			}
			this._dictOfFileStreams.Remove(fileId);
		}

		private FileStream GetTargetStream(ushort fileId)
		{
			FileStream fs;
			if (this._dictOfFileStreams.TryGetValue(fileId, out fs))
			{
				return fs;
			}
			return null;
		}

		private void AddFileData(string fileName, ushort fileId)
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				string filePath = Path.Combine(this._rootFolder, fileName);
				string dirPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(dirPath))
				{
					Directory.CreateDirectory(dirPath);
				}
				FileStream fs = File.Create(filePath);
				this._dictOfFileStreams[fileId] = fs;
			}
		}
		private Dictionary<ushort, FileStream> _dictOfFileStreams = new Dictionary<ushort, FileStream>();
		private void ReadTcpMessage(uint numOfBytesToRead)
		{
			int numOfBytesRead = this._commSocket.Receive(this._data, 0, (int)numOfBytesToRead, SocketFlags.None);
			while (numOfBytesRead < numOfBytesToRead && this._commSocket.IsSocketConnected())
			{
				numOfBytesRead += this._commSocket.Receive(this._data, numOfBytesRead, (int)(numOfBytesToRead - numOfBytesRead), SocketFlags.None);
			}
		}
		private void ReadHeader()
		{
			int numOfBytesRead = this._commSocket.Receive(this._data, 0, TcpFileMessage.MESSAGE_HEADER_LENGTH, SocketFlags.None);
			uint numOfBytesToRead = TcpFileMessage.MESSAGE_HEADER_LENGTH;
			while (numOfBytesRead < numOfBytesToRead && this._commSocket.IsSocketConnected())
			{
				numOfBytesRead += this._commSocket.Receive(this._data, numOfBytesRead, TcpFileMessage.MESSAGE_HEADER_LENGTH - numOfBytesRead, SocketFlags.None);
			}
		}
		//private class FileTracker
		//{
		//	public UInt32 NumOfBytesToRead { get; set; }
		//}

		//private void TcpFileReceiveCallback(IAsyncResult ar)
		//{
		//	try
		//	{
		//		FileTracker fileTracker = ar.AsyncState as FileTracker;
		//		int numOfBytesRead = this._commSocket.EndReceive(ar);
		//		//tcpFileData.NumOfBytesRead += (UInt32)numOfBytesRead;
		//		if (numOfBytesRead > 0)
		//		{
		//			if (tcpFileData.NumOfBytesRead != tcpFileData.NumOfBytesToRead)
		//			{
		//				// do block read
		//				tcpFileData.RefSocket.Receive(tcpFileData.Data, (int)tcpFileData.NumOfBytesRead, tcpFileData.Data.Length - (int)tcpFileData.NumOfBytesRead, SocketFlags.None);
		//			}

		//			if (tcpFileData.NumOfBytesRead != tcpFileData.NumOfBytesToRead)
		//			{
		//				throw new InvalidOperationException("wrong size of header.");
		//			}
		//			tcpFileData.IsHeaderRead = true;
		//			tcpFileData.MsgType = TcpFileMessage.GetMsgType(tcpFileData.Data);
		//			if (tcpFileData.MsgType == TcpFileMessage.MsgTypeId.File)
		//			{
		//				tcpFileData.FileMsgType = TcpFileMessage.GetFileMsgType(tcpFileData.Data);
		//				tcpFileData.MsgLength = TcpFileMessage.GetMsgLength(tcpFileData.Data);
		//				tcpFileData.FileId = TcpFileMessage.GetFileId(tcpFileData.Data);
		//				tcpFileData.Data = new byte[tcpFileData.MsgLength];
		//				tcpFileData.Reset();
		//				tcpFileData.NumOfBytesToRead = (uint)tcpFileData.Data.Length;
		//			}
		//			// since msg was not read completely we need to go and read it until it's finished
		//			tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, 0, tcpFileData.Data.Length, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//		}
		//		else if (numOfBytesRead > 0 && tcpFileData.IsHeaderRead)
		//		{
		//			if (tcpFileData.NumOfBytesRead < tcpFileData.NumOfBytesToRead)
		//			{
		//				tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, (int)tcpFileData.NumOfBytesRead, tcpFileData.Data.Length - (int)tcpFileData.NumOfBytesRead, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//			}
		//			else if (tcpFileData.NumOfBytesRead == tcpFileData.NumOfBytesToRead)
		//			{
		//				switch (tcpFileData.FileMsgType)
		//				{
		//					case TcpFileMessage.FileMsgTypeId.Header:
		//						UInt16 fileNameLength = TcpFileMessage.GetFileNameLength(tcpFileData.Data);
		//						tcpFileData.FileName = Encoding.ASCII.GetString(tcpFileData.Data, TcpFileMessage.FILE_NAME_LENGTH, fileNameLength);
		//						tcpFileData.FileLength = TcpFileMessage.GetFileLength(fileNameLength, tcpFileData.Data);
		//						// create new tcpFileData
		//						tcpFileData = tcpFileData.CreateNew();
		//						// add to global dictionary
		//						AddFileData(tcpFileData);
		//						// create file stream
		//						tcpFileData.CreateFileStream();
		//						LogOut?.WriteLine($"receiving file {tcpFileData.FileName}");
		//						Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//						break;
		//					case TcpFileMessage.FileMsgTypeId.Block:
		//						Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//						tcpFileData.FileBytesRead += tcpFileData.MsgLength;
		//						// first write the bytes to file
		//						tcpFileData.WriteBytes();
		//						break;
		//					case TcpFileMessage.FileMsgTypeId.Close:
		//						Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//						tcpFileData.FileBytesRead += tcpFileData.MsgLength;
		//						// first write the bytes to file
		//						tcpFileData.WriteBytes();
		//						tcpFileData.CloseFile();
		//						LogOut?.WriteLine($"finished file {tcpFileData.FileName}");
		//						RemoveFileData(tcpFileData);
		//						break;
		//					case TcpFileMessage.FileMsgTypeId.None:
		//						break;
		//					default:
		//						throw new ArgumentOutOfRangeException();
		//				}
		//				tcpFileData.ResetFileData();
		//				tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, 0, tcpFileData.Data.Length, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//			}
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		Logger.Exception(exception);
		//		tcpFileData.RefSocket.CloseSocket();
		//	}
		//}


		//private void XXXTcpFileReceiveCallback(IAsyncResult ar)
		//{
		//	TcpFileData tcpFileData = ar.AsyncState as TcpFileData;
		//	if (tcpFileData != null)
		//	{
		//		try
		//		{
		//			int numOfBytesRead = tcpFileData.RefSocket.EndReceive(ar);
		//			tcpFileData.NumOfBytesRead += (UInt32)numOfBytesRead;

		//			if (numOfBytesRead > 0 && !tcpFileData.IsHeaderRead)
		//			{
		//				if (tcpFileData.NumOfBytesRead != tcpFileData.NumOfBytesToRead)
		//				{
		//					// do block read
		//					tcpFileData.RefSocket.Receive(tcpFileData.Data, (int)tcpFileData.NumOfBytesRead, tcpFileData.Data.Length - (int)tcpFileData.NumOfBytesRead, SocketFlags.None);
		//				}

		//				if (tcpFileData.NumOfBytesRead != tcpFileData.NumOfBytesToRead)
		//				{
		//					throw new InvalidOperationException("wrong size of header.");
		//				}
		//				tcpFileData.IsHeaderRead = true;
		//				tcpFileData.MsgType = TcpFileMessage.GetMsgType(tcpFileData.Data);
		//				if (tcpFileData.MsgType == TcpFileMessage.MsgTypeId.File)
		//				{
		//					tcpFileData.FileMsgType = TcpFileMessage.GetFileMsgType(tcpFileData.Data);
		//					tcpFileData.MsgLength = TcpFileMessage.GetMsgLength(tcpFileData.Data);
		//					tcpFileData.FileId = TcpFileMessage.GetFileId(tcpFileData.Data);
		//					tcpFileData.Data = new byte[tcpFileData.MsgLength];
		//					tcpFileData.Reset();
		//					tcpFileData.NumOfBytesToRead = (uint)tcpFileData.Data.Length;
		//				}
		//				// since msg was not read completely we need to go and read it until it's finished
		//				tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, 0, tcpFileData.Data.Length, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//			}
		//			else if (numOfBytesRead > 0 && tcpFileData.IsHeaderRead)
		//			{
		//				if (tcpFileData.NumOfBytesRead < tcpFileData.NumOfBytesToRead)
		//				{
		//					tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, (int)tcpFileData.NumOfBytesRead, tcpFileData.Data.Length - (int)tcpFileData.NumOfBytesRead, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//				}
		//				else if (tcpFileData.NumOfBytesRead == tcpFileData.NumOfBytesToRead)
		//				{
		//					switch (tcpFileData.FileMsgType)
		//					{
		//						case TcpFileMessage.FileMsgTypeId.Header:
		//							UInt16 fileNameLength = TcpFileMessage.GetFileNameLength(tcpFileData.Data);
		//							tcpFileData.FileName = Encoding.ASCII.GetString(tcpFileData.Data, TcpFileMessage.FILE_NAME_LENGTH, fileNameLength);
		//							tcpFileData.FileLength = TcpFileMessage.GetFileLength(fileNameLength, tcpFileData.Data);
		//							// create new tcpFileData
		//							tcpFileData = tcpFileData.CreateNew();
		//							// add to global dictionary
		//							AddFileData(tcpFileData);
		//							// create file stream
		//							tcpFileData.CreateFileStream();
		//							LogOut?.WriteLine($"receiving file {tcpFileData.FileName}");
		//							Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//							break;
		//						case TcpFileMessage.FileMsgTypeId.Block:
		//							Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//							tcpFileData.FileBytesRead += tcpFileData.MsgLength;
		//							// first write the bytes to file
		//							tcpFileData.WriteBytes();
		//							break;
		//						case TcpFileMessage.FileMsgTypeId.Close:
		//							Logger.Log($"{tcpFileData.FileId}, {tcpFileData.MsgLength} {tcpFileData.FileMsgType}", "<");
		//							tcpFileData.FileBytesRead += tcpFileData.MsgLength;
		//							// first write the bytes to file
		//							tcpFileData.WriteBytes();
		//							tcpFileData.CloseFile();
		//							LogOut?.WriteLine($"finished file {tcpFileData.FileName}");
		//							RemoveFileData(tcpFileData);
		//							break;
		//						case TcpFileMessage.FileMsgTypeId.None:
		//							break;
		//						default:
		//							throw new ArgumentOutOfRangeException();
		//					}
		//					tcpFileData.ResetFileData();
		//					tcpFileData.RefSocket.BeginReceive(tcpFileData.Data, 0, tcpFileData.Data.Length, SocketFlags.None, TcpFileReceiveCallback, tcpFileData);
		//				}
		//			}
		//		}
		//		catch (Exception exception)
		//		{
		//			Logger.Exception(exception);
		//			tcpFileData.RefSocket.CloseSocket();
		//		}
		//	}
		//}
		private string _rootFolder = @"c:\PraveenData\userdata";

		public void CreateFileStream(string FileName)
		{
			//if (!string.IsNullOrEmpty(FileName))
			//{
			//	string filePath = Path.Combine(this._rootFolder, FileName);
			//	string dirPath = Path.GetDirectoryName(filePath);
			//	if (!Directory.Exists(dirPath))
			//	{
			//		Directory.CreateDirectory(dirPath);
			//	}
			//	this._fsWriter = File.Create(filePath);
			//}
		}
		private readonly object _objLocker = new object();
		private readonly Dictionary<UInt16, TcpFileData> _tcpFileDataDict = new Dictionary<ushort, TcpFileData>();
		private void AddFileData(TcpFileData tcpFileData)
		{
			lock (_objLocker)
			{
				this._tcpFileDataDict[tcpFileData.FileId] = tcpFileData;
			}
		}
		private void RemoveFileData(TcpFileData tcpFileData)
		{
			lock (_objLocker)
			{
				this._tcpFileDataDict.Remove(tcpFileData.FileId);
			}
		}
	}



	public static class TcpCommon
	{
		public const int BROADCAST_PORT = 9090;
		public const int SERVER_PORT = 8050;
		public const int HTTP_SERVER_PORT = 8888;
		public const int TCP_FILE_RECEIVER_PORT = 8080;
		public const int LOGIN_SERVER_PORT = 9999;
		public const int CLIENT_PING_PORT = 10000;

		public static byte[] TextToBytes(this string str)
		{
			if (!string.IsNullOrEmpty(str))
			{
				return Encoding.UTF8.GetBytes(str);
			}
			return null;
		}
		public static void SaveToFile(this byte[] data, string fileName)
		{
			if (data.IsNotNullOrEmpty())
			{
				using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
				{
					fs.Write(data, 0, data.Length);
				}
			}
		}
		public static void SendToSocket(this string text, Socket refSocket)
		{
			if (!string.IsNullOrEmpty(text))
			{
				byte[] data = Encoding.UTF8.GetBytes(text);
				if (refSocket.IsSocketConnected())
				{
					refSocket.Send(data);
				}
			}
		}
		public static void CloseSocket(this Socket socket)
		{
			try
			{
				if (socket != null)
				{
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
			}
			catch (Exception)
			{
			}
		}

		public static bool IsSocketConnected(this Socket socket)
		{
			try
			{
				if (socket != null)
				{
					return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
				}
			}
			catch
			{
			}
			return false;
		}

		public static void SendBytes(this Socket refSocket, byte[] dataBytes)
		{
			try
			{
				if (refSocket.IsSocketConnected())
				{
					refSocket.Send(dataBytes);
				}
			}
			catch (Exception e)
			{
				refSocket.CloseSocket();
				Logger.Exception(e);
			}
		}

		public static int ReadSocketBytes(this Socket clientSocket, byte[] data)
		{
			int bytesToRead = data.Length;
			int bytesRead = 0;
			while (clientSocket.Connected && bytesRead < bytesToRead)
			{
				bytesRead += clientSocket.Receive(data, bytesRead, bytesToRead - bytesRead, SocketFlags.None);
			}
			return bytesRead;
		}

		public static
			string ReadLine(this Socket clientSocket)
		{
			StringBuilder sb = new StringBuilder();
			bool gotCR = false;
			while (clientSocket.Connected)
			{
				if (clientSocket.IsSocketConnected())
				{
					int bytesAvailable = clientSocket.Available;
					if (bytesAvailable > 0)
					{
						// read byte by byte
						byte[] bData = new byte[1];
						//int recLength = clientSocket.Receive(bData);
						for (int i = 0; i < bytesAvailable; i++)
						{
							int recLength = clientSocket.Receive(bData);
							//Console.Write($"{(char)bData[0]}: {bData[0]}");
							// all sensible characters starts after ASCII code 32
							if (recLength > 0)
							{
								if (bData[0] >= 32)
								{
									sb.Append((char)bData[0]);
								}
								else if (bData[0] == 13) // CR
								{
									gotCR = true;
								}
								else if (bData[0] == 10 && gotCR)
								{
									return sb.ToString();
								}
							}
							else
							{
								if (gotCR)
								{
									return sb.ToString();
								}
							}
						}
					}
				}
				else
				{
					break;
				}
			}
			return "";
		}
	}

	public class Utils
	{
		public static string CreateJSON(string msgId, string value)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append($"\"MsgId\": \"{msgId}\"");
			sb.Append($",\"Value\": \"{value}\"");
			sb.Append("}");
			return sb.ToString();
		}

		public static string GetIpAddress()
		{
			IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
			for (int i = 0; i < ipAddresses.Length; i++)
			{
				if (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork)
				{
					return ipAddresses[i].ToString();
				}
			}
			return "";
		}

		public static string GetIpAddresses()
		{
			StringBuilder sb = new StringBuilder();
			IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
			bool add = false;
			for (int i = 0; i < ipAddresses.Length; i++)
			{
				if (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork)
				{
					if (!add)
					{
						sb.Append(ipAddresses[i]);
						add = true;
					}
					else
					{
						sb.Append($";{ipAddresses[i]}");
					}
				}
			}
			return sb.ToString();
		}
	}
}
