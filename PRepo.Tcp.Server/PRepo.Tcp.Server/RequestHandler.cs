using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using PRepo.Tcp.Common;

namespace PRepo.Tcp.Server
{
	public class RequestHandler
	{
		//private byte[] _headerBytes;
		//private byte[] _msgBytes;
		private readonly Socket _socket;

		public RequestHandler(Socket socket)
		{
			this._socket = socket;
		}
		public void HandleRequest(byte[] requestHeaderBytes, byte[] requestMsgBytes)
		{
			TcpMessage.MessageID msgID = TcpMessage.GetMessageID(requestHeaderBytes);
			Logger.Log($"< [{_socket.RemoteEndPoint} {msgID}]");
			string responseText = "UNKNOWN_COMMAND";
			byte[] responseBytes = null;
			byte[] msg = null;
			switch (msgID)
			{
				case TcpMessage.MessageID.NONE:
					break;
				case TcpMessage.MessageID.GET_HELP:
					responseText = TcpCommands.GetHelpText();
					responseBytes = Encoding.Default.GetBytes(responseText);
					msg = TcpMessage.CreateMessage(TcpMessage.MessageID.HELP, responseBytes);
					// send response
					_socket?.SendBytes(msg);
					break;
				case TcpMessage.MessageID.GET_ALL_FROM_REPO:
					responseText = TcpCommands.GetRepoListAll();
					responseBytes = Encoding.Default.GetBytes(responseText);
					msg = TcpMessage.CreateMessage(TcpMessage.MessageID.ALL_FROM_REPO, responseBytes);
					// send response
					_socket?.SendBytes(msg);
					break;
				case TcpMessage.MessageID.GET_DIR_CLONE:
					HandleDirectoryCloneRequest(requestHeaderBytes, requestMsgBytes);
					break;
				case TcpMessage.MessageID.GET_FILE:
					HandleGetFileRequest(requestMsgBytes);
					break;
				case TcpMessage.MessageID.DIR_EXPLORE:
					ExploreDirectory(requestMsgBytes);
					break;
				case TcpMessage.MessageID.GET_STATUS:
					_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.STATUS, Encoding.Default.GetBytes("ALIVE")));
					break;
			}
		}

		private void HandleGetFileRequest(byte[] requestMsgBytes)
		{
			_totalBytesTransferred = 0;
			_totalSize = 0;
			string fileToClone = Encoding.Default.GetString(requestMsgBytes);
			string filePath = Path.Combine(OptionConfiguration.GetRootFolder(), fileToClone);
			byte[] responseMsgBytes = null;
			if (File.Exists(filePath))
			{
				FileInfo fInfo = new FileInfo(filePath);
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_TRANSFER_BEGIN);
				_socket.SendBytes(responseMsgBytes);
				_totalSize = fInfo.Length;
				SendFileToClient(fInfo);
				//CloneDirectory(Path.Combine(OptionConfiguration.GetRootFolder(), dirToClone));
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_TRANSFER_END);
				_socket.SendBytes(responseMsgBytes);
			}
			else
			{
				// produce error message
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.ERROR, $"{Path.GetFileName(filePath)} does not exist.");
				_socket.SendBytes(responseMsgBytes);
			}
		}

		private void ExploreDirectory(byte[] requestMsgBytes)
		{
			try
			{
				string dirToClone = Encoding.Default.GetString(requestMsgBytes);
				dirToClone = dirToClone.Trim();
				string dirPath = Path.Combine(OptionConfiguration.GetRootFolder(), dirToClone);
				if (Directory.Exists(dirPath))
				{
					StringBuilder sb = new StringBuilder();
					ExploreDir(new DirectoryInfo(dirPath), sb, 0);
					_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.DIR_EXPLORED, Encoding.Default.GetBytes(sb.ToString())));
				}
				else
				{
					_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.DIR_EXPLORED, Encoding.Default.GetBytes("DIR_NOT_FOUND")));
				}
			}
			catch
			{
				_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.DIR_EXPLORED, Encoding.Default.GetBytes("ERROR")));
			}
		}

		private void ExploreDir(DirectoryInfo src, StringBuilder sb, int tabCounts)
		{
			sb.AppendLine($"{src.Name.AddTabs(tabCounts)}");
			tabCounts++;
			foreach (FileInfo file in src.GetFiles())
			{
				sb.AppendLine($"{file.Name.AddTabs(tabCounts)}\t{file.Length / 1024}KB");
			}
			tabCounts++;

			foreach (DirectoryInfo dirInfo in src.GetDirectories())
			{
				sb.AppendLine($"{dirInfo.Name.AddTabs(tabCounts)}");
				//ExploreDir(dirInfo, sb, tabCounts++);
			}
		}

		private long _totalBytesTransferred = 0;
		private long _totalSize = 0;
		private string _strNewRoot;

		private void HandleDirectoryCloneRequest(byte[] header, byte[] requestMsgBytes)
		{
			_totalSize = 0;
			string dirToClone = Encoding.Default.GetString(requestMsgBytes);
			string dirPath = Path.Combine(OptionConfiguration.GetRootFolder(), dirToClone);
			_strNewRoot = Path.GetDirectoryName(dirPath);
			byte[] responseMsgBytes = null;
			int numOfFiles = 0;
			if (dirPath.DirExists())
			{
				Utils.GetDirectorySize(new DirectoryInfo(dirPath), ref numOfFiles, ref _totalSize);
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.CLONE_BEGIN);
				_socket.SendBytes(responseMsgBytes);
				CloneDirectory(Path.Combine(OptionConfiguration.GetRootFolder(), dirToClone));
				_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.CLONE_PROGRESS_PERCENTAGE, new byte[] { 100 }));
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.CLONE_END);
				_socket.SendBytes(responseMsgBytes);
				_strNewRoot = "";
			}
			else
			{
				// produce error message
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.ERROR, $"{dirToClone} does not exist.");
				_socket.SendBytes(responseMsgBytes);
			}
		}

		private void CloneDirectory(string dirPath)
		{
			byte[] responseMsgBytes = null;
			if (dirPath.DirExists())
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
				// start cloning, send DIR_INFO first
				responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.DIR_INFO, $"{dirPath.RemoveRootPath(_strNewRoot)}");
				_socket.SendBytes(responseMsgBytes);
				// get all files & start sending file by file
				FileInfo[] files = directoryInfo.GetFiles();
				for (int i = 0; i < files.Length; i++)
				{
					FileInfo fileInfo = files[i];
					SendFileToClient(fileInfo);
				}
				foreach (DirectoryInfo info in directoryInfo.GetDirectories())
				{
					CloneDirectory(info.FullName);
				}
			}
		}

		private const int READ_BUFFER = 1024 * 1024 * 64;
		private void SendFileToClient(FileInfo fileInfo)
		{
			if (fileInfo.Exists)
			{
				FileStream fs = null;
				try
				{
					// start with sending FILE_INFO first
					byte[] responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_INFO, $"{fileInfo.FullName.RemoveRootPath(_strNewRoot)}");
					_socket.SendBytes(responseMsgBytes);
					WaitForAckFromClient();

					long fileLength = fileInfo.Length;
					long totalBytesRead = 0;
					byte[] readBuffer = null;

					if (fileLength < READ_BUFFER)
					{
						readBuffer = new byte[(int)fileLength];
					}
					else
					{
						readBuffer = new byte[READ_BUFFER];
					}

					fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
					while (totalBytesRead < fileLength)
					{
						int numOfBytesRead = 0;
						int bytesReadSoFar = 0;

						while (bytesReadSoFar < readBuffer.Length)
						{
							numOfBytesRead = fs.Read(readBuffer, numOfBytesRead, readBuffer.Length - numOfBytesRead);

							totalBytesRead += numOfBytesRead;
							bytesReadSoFar += numOfBytesRead;
							_totalBytesTransferred += numOfBytesRead;

							if (totalBytesRead == fileLength)
							{
								break;
							}
						}
						// send bytes
						if (bytesReadSoFar > 0)
						{
							byte[] fileDataBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_DATA, readBuffer, bytesReadSoFar);
							if (_socket.SendBytes(fileDataBytes))
							{
								WaitForAckFromClient();
								byte percentage = (byte)((_totalBytesTransferred * 100) / _totalSize);
								_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.CLONE_PROGRESS_PERCENTAGE,
									new byte[] { percentage }));
							}
						}
					}
					// send close Msg
					if (_socket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_DATA_CLOSE)))
					{
						WaitForAckFromClient();
					}
				}
				catch (UserCanceldFileTransferException userCanceldFileTransferException)
				{
					Logger.Log($"{nameof(SendFileToClient)} - {userCanceldFileTransferException.Message}");
				}
				finally
				{
					fs?.Close();
				}
			}
		}

		private void WaitForAckFromClient()
		{
			byte[] ackBytes = new byte[TcpMessage.HEADER_LENGTH];
			int numOfBytesRead = _socket.Receive(ackBytes, 0, ackBytes.Length, SocketFlags.None);
			while (numOfBytesRead < ackBytes.Length)
			{
				numOfBytesRead += _socket.Receive(ackBytes, numOfBytesRead, ackBytes.Length - numOfBytesRead, SocketFlags.None);
			}
			Logger.Log($"< {TcpMessage.GetMessageID(ackBytes)}");

			if (TcpMessage.GetMessageID(ackBytes) == TcpMessage.MessageID.FILE_TRANSFER_CANCEL)
			{
				throw new UserCanceldFileTransferException("User canceled file transfer.");
			}
			else if (TcpMessage.GetMessageID(ackBytes) != TcpMessage.MessageID.FILE_DATA_OK)
			{
				throw new InvalidOperationException("Did not reeive Ack.");
			}
		}
	}

	public class UserCanceldFileTransferException : Exception
	{
		public UserCanceldFileTransferException(string msg) : base(msg)
		{

		}
	}
}