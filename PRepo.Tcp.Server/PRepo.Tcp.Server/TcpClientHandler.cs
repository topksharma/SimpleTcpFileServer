using System;
using PRepo.Tcp.Common;

namespace PRepo.Tcp.Server
{
	using System.Net.Sockets;

	public class RequestReaderToken
	{
		public byte[] Buffer { get; set; }
	}
	public class TcpClientHandler
	{
		private readonly Socket _socket;

		public TcpClientHandler(Socket socket)
		{
			_socket = socket;
		}

		public void HandleClient()
		{
			RequestHandler requestHandler = new RequestHandler(_socket);
			string remoteClientAddress = _socket.RemoteEndPoint.ToString();
			try
			{
				while (_socket.IsConnected())
				{
					// wait & read request header
					byte[] buffer = new byte[TcpMessage.HEADER_LENGTH];

					int numOfBytesRead = 0;
					while (numOfBytesRead < buffer.Length)
					{
						numOfBytesRead += _socket.Receive(buffer, numOfBytesRead,
							buffer.Length - numOfBytesRead, SocketFlags.None);
					}
					// got header, read rest of the message
					numOfBytesRead = 0;
					int msgLength = TcpMessage.GetMessageLength(buffer);
					byte[] msgBytes = new byte[msgLength];
					if (msgLength > 0)
					{
						while (numOfBytesRead < msgLength)
						{
							numOfBytesRead += _socket.Receive(msgBytes, numOfBytesRead,
								msgBytes.Length - numOfBytesRead, SocketFlags.None);
						}
					}
					// done reading, prepare response
					requestHandler.HandleRequest(buffer, msgBytes);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"Error - {e}");
			}
			finally
			{
				_socket?.CloseSocket();
				Console.WriteLine($"CONNECTION CLOSED TO [{remoteClientAddress}]");
			}
		}
	}
}
