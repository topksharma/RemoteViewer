using System;

namespace SrvCommon
{
	public class Message
	{
		public enum MESSAGE_ID : ushort
		{
			NONE,
			OK,
			GET_STATUS,
			STATUS,
			GET_SCREEN,
			SCREEN,
			BUTTON,
			MOUSE
		}

		public enum KEY_ID : ushort
		{
			VIEW,
			TBL,
			FUNC,
			INFO,
			CAL,
			MEM,
			START_STOP,
			PAUSE_CONTINUE,
			CANCEL,
			ENTER,
			SETUP,
			POWER,
			UP,
			DOWN,
			LEFT,
			RIGHT
		}

		public const int MSG_HEADER_LENGTH = 6;
		public const int MSG_TYPE_LENGTH = 2;

		public static MESSAGE_ID GetMessageType(byte[] buffer)
		{
			if (buffer != null && buffer.Length >= MSG_HEADER_LENGTH)
			{
				return (MESSAGE_ID)BitConverter.ToUInt16(buffer, 0);
			}
			return MESSAGE_ID.NONE;
		}

		public static int GetMessageLength(byte[] buffer)
		{
			if (buffer != null && buffer.Length >= MSG_HEADER_LENGTH)
			{
				return BitConverter.ToInt32(buffer, MSG_TYPE_LENGTH);
			}
			return 0;
		}

		public static byte[] Create(MESSAGE_ID messageId)
		{
			return Create(messageId, 0);
		}

		public static byte[] Create(MESSAGE_ID messageId, int msgLength)
		{
			byte[] msgBytes = new byte[MSG_HEADER_LENGTH];

			byte[] msgIdBytes = BitConverter.GetBytes((ushort)messageId);
			msgIdBytes.CopyTo(msgBytes, 0);

			byte[] msgLengthBytes = BitConverter.GetBytes(msgLength);
			msgLengthBytes.CopyTo(msgBytes, MSG_TYPE_LENGTH);

			return msgBytes;
		}

		//public static byte[] CreateMouseMessage(int msgDataX, int msgDataY)
		//{
		//	byte[] header = Create(MESSAGE_ID.MOUSE, 8);

		//	byte[] completeMsg = new byte[MSG_HEADER_LENGTH + 8];
		//	header.CopyTo(completeMsg, 0);

		//	BitConverter.GetBytes(msgDataX).CopyTo(completeMsg, MSG_HEADER_LENGTH);
		//	BitConverter.GetBytes(msgDataY).CopyTo(completeMsg, MSG_HEADER_LENGTH + 4);

		//	return completeMsg;

		//}

		public static byte[] CreateMessageWithInts(MESSAGE_ID msgId, int[] args)
		{
			int argsSize = 0;
			if (args == null)
			{
				return Create(msgId, 0);
			}

			argsSize = args.Length * 4;

			byte[] header = Create(msgId, argsSize);

			byte[] completeMsg = new byte[MSG_HEADER_LENGTH + argsSize];
			header.CopyTo(completeMsg, 0);

			int index = MSG_HEADER_LENGTH;
			for (int i = 0; i < args.Length; i++)
			{
				BitConverter.GetBytes(args[i]).CopyTo(completeMsg, index);
				index += 4;
			}
			return completeMsg;
		}
	}
}
