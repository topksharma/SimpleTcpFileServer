using System;
using System.Text;

namespace PRepo.Tcp.Common
{
	public class TcpMessage
	{
		public enum MessageID : ushort
		{
			NONE,
			GET_HELP,
			HELP,
			GET_ALL_FROM_REPO,
			ALL_FROM_REPO,
			DIR_INFO,
			CLONE_BEGIN,
			CLONE_PROGRESS_PERCENTAGE,
			CLONE_END,
			FILE_TRANSFER_BEGIN,
			FILE_TRANSFER_END,
			FILE_DATA,
			FILE_INFO,
			FILE_DATA_CLOSE,
			FILE_DATA_OK,
			FILE_TRANSFER_CANCEL,
			GET_STATUS,
			GET_FILE,
			GET_DIR,
			GET_DIR_BEGIN,
			GET_DIR_END,
			STATUS,
			GET_DIR_CLONE,
			ERROR,
			DIR_EXPLORE,
			DIR_EXPLORED,
			RECONNECT
		}
		// everything is in bytes
		public const int HEADER_LENGTH = 6;
		public const int MSG_TYPE_LENGTH = 2;
		public const int MSG_LENGTH = 4;

		public static byte[] CreateMessage(MessageID messageId)
		{
			byte[] headerBytes = new byte[HEADER_LENGTH];
			byte[] msgIDBytes = BitConverter.GetBytes((UInt16)messageId);
			msgIDBytes.CopyTo(headerBytes, 0);
			return headerBytes;
		}

		public static MessageID GetMessageID(string cmd)
		{
			if (ValidationHelper.IsCmdValid(cmd))
			{
				return TcpCommands.GetMsgIDByCommand(cmd);
			}
			return MessageID.NONE;
		}

		public static byte[] CreateMessage(MessageID msgID, byte[] responseBytes)
		{
			byte[] completeMsgBytes = new byte[HEADER_LENGTH + responseBytes.Length];
			// fill MsgID
			byte[] msgIDBytes = BitConverter.GetBytes((UInt16)msgID);
			msgIDBytes.CopyTo(completeMsgBytes, 0);
			// fill length
			byte[] msgLengthBytes = BitConverter.GetBytes(responseBytes.Length);
			msgLengthBytes.CopyTo(completeMsgBytes, MSG_TYPE_LENGTH);
			//
			responseBytes.CopyTo(completeMsgBytes, HEADER_LENGTH);
			return completeMsgBytes;
		}

		public static MessageID GetMessageID(byte[] buffer)
		{
			if (buffer == null || buffer.Length < HEADER_LENGTH)
			{
				return MessageID.NONE;
			}
			return (MessageID)BitConverter.ToUInt16(buffer, 0);
		}

		public static int GetMessageLength(byte[] buffer)
		{
			if (buffer == null || buffer.Length < HEADER_LENGTH)
			{
				return 0;
			}
			return BitConverter.ToInt32(buffer, MSG_TYPE_LENGTH);
		}

		public static byte[] CreateMessage(MessageID requestMessageId, string message)
		{
			byte[] msgBytes = Encoding.Default.GetBytes(message);
			return CreateMessage(requestMessageId, msgBytes);
		}

		public static byte[] CreateMessage(MessageID msgId, byte[] buffer, int size)
		{
			byte[] msgBytes = new byte[size];
			Array.Copy(buffer, 0, msgBytes, 0, size);
			return CreateMessage(msgId, msgBytes);
		}
	}
}
