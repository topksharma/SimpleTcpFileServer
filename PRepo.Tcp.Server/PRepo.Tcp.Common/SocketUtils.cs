using System;
using System.Net.Sockets;

namespace PRepo.Tcp.Common
{
	public static class SocketUtils
	{
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
			catch { }
		}
		public static bool IsConnected(this Socket socket)
		{
			try
			{
				if (socket != null)
				{
					return !(socket.Poll(10, SelectMode.SelectRead) && socket.Available == 0);
				}
			}
			catch { }
			return false;
		}

		public static bool SendBytes(this Socket socket, byte[] dataBytes)
		{
			Logger.Log($"> {TcpMessage.GetMessageID(dataBytes)}");
			return SendBytes(socket, dataBytes, 0, dataBytes.Length);
			//try
			//{
			//	if (socket.IsConnected())
			//	{
			//		int totalBytesSend = 0;
			//		while (totalBytesSend < dataBytes.Length)
			//		{
			//			totalBytesSend += socket.Send(dataBytes, totalBytesSend, dataBytes.Length - totalBytesSend, SocketFlags.None);
			//		}
			//		return true;
			//	}
			//}
			//catch { }
			//return false;
		}

		public static bool SendBytes(this Socket socket, byte[] dataBytes, int offset, int size)
		{
			try
			{
				if (socket.IsConnected())
				{
					int totalBytesSend = 0;
					while (totalBytesSend < size)
					{
						totalBytesSend += socket.Send(dataBytes, totalBytesSend + offset, size - totalBytesSend, SocketFlags.None);
					}
					return true;
				}
			}
			catch { }
			return false;
		}
	}
}
