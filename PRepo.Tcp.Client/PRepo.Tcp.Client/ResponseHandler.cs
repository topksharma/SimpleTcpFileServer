using System;
using System.Text;
using PRepo.Tcp.Common;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace PRepo.Tcp.Client
{
	public class ResponseHandler
	{
		//private byte[] _headerBytes;
		//private byte[] _msgBytes;
		//public ResponseHandler(byte[] header, byte[] msgBytes)
		//{
		//	_headerBytes = header;
		//	_msgBytes = msgBytes;
		//}
		public ResponseHandler(Socket streamSocket)
		{
			_streamSocket = streamSocket;
		}

		public void HandleResponse(byte[] header, byte[] msgBytes)
		{
			TcpMessage.MessageID msgID = TcpMessage.GetMessageID(header);
			string strResponse = string.Empty;
			switch (msgID)
			{
				case TcpMessage.MessageID.NONE:
					break;
				case TcpMessage.MessageID.ERROR:
					Logger.Log($"< {msgID}");
					strResponse = Encoding.Default.GetString(msgBytes);
					Console.WriteLine(strResponse);
					break;
				case TcpMessage.MessageID.STATUS:
					Logger.Log($"< {msgID}");
					strResponse = Encoding.Default.GetString(msgBytes);
					Console.WriteLine(strResponse);
					break;
				case TcpMessage.MessageID.HELP:
					Logger.Log($"< {msgID}");
					strResponse = Encoding.Default.GetString(msgBytes);
					Console.WriteLine(strResponse);
					break;
				case TcpMessage.MessageID.DIR_EXPLORED:
					Logger.Log($"< {msgID}");
					strResponse = Encoding.Default.GetString(msgBytes);
					Console.WriteLine(strResponse);
					break;
				case TcpMessage.MessageID.ALL_FROM_REPO:
					Logger.Log($"< {msgID}");
					strResponse = Encoding.Default.GetString(msgBytes);
					Console.WriteLine(strResponse);
					break;
				case TcpMessage.MessageID.CLONE_BEGIN:
					StartReceivingCatalogClone(true);
					break;
				case TcpMessage.MessageID.GET_DIR_BEGIN:
					_getDir = true;
					StartReceivingCatalogClone(true);
					break;
				case TcpMessage.MessageID.FILE_TRANSFER_BEGIN:
					StartReceivingFile();
					break;
					//case TcpMessage.MessageID.DIR_INFO:
					//	string dirName = Encoding.Default.GetString(msgBytes);
					//	CreateDirectory(dirName);
					//	//Console.WriteLine(help);
					//	break;
					//case TcpMessage.MessageID.FILE_INFO:
					//	string fileName = Encoding.Default.GetString(msgBytes);
					//	CreateFile(fileName);
					//	//Console.WriteLine(help);
					//	break;
			}
		}

		private bool _getDir = false;

		private void StartReceivingFile()
		{
			StartReceivingCatalogClone(false);
		}

		public void Close()
		{
			try
			{
				_fsWrite?.Close();
				Console.WriteLine("File Stream CLOSEED");
			}
			catch
			{
			}
		}

		private readonly byte[] FILE_MSG_ACK_BYTES = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_DATA_OK);

		private void StartReceivingCatalogClone(bool isForCatalog)
		{
			//Console.WriteLine("cloning BEGIN.....");
			Tuple<byte[], byte[]> tcpMessage = ReadTcpMessage();
			TcpMessage.MessageID msgID = TcpMessage.GetMessageID(tcpMessage.Item1);
			//Logger.Log($"< {msgID}");
			bool endLoop = false;
			while (!endLoop)
			//while (msgID != TcpMessage.MessageID.CLONE_END || ended)
			{
				switch (msgID)
				{
					case TcpMessage.MessageID.FILE_INFO:
						string fileName = Encoding.Default.GetString(tcpMessage.Item2);
						Logger.Log($"< {msgID} {fileName}");
						try
						{
							//if (_getDir)
							//{

							//}
							if (isForCatalog)
							{
								CreateFile(CreateFilePath(fileName));
							}
							else
							{
								CreateFile(CreateDownloadFilePath(fileName));
							}
							//byte[] responseMsgBytes = TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_DATA_OK);
							_streamSocket.SendBytes(FILE_MSG_ACK_BYTES);
						}
						catch
						{
							Close();
							// if error, tell server
							_streamSocket.SendBytes(TcpMessage.CreateMessage(TcpMessage.MessageID.FILE_TRANSFER_CANCEL));
						}
						break;
					case TcpMessage.MessageID.DIR_INFO:
						string dirName = Encoding.Default.GetString(tcpMessage.Item2);
						Logger.Log($"< {msgID} {dirName}");
						CreateDirectory(dirName);
						break;
					case TcpMessage.MessageID.CLONE_END:
						endLoop = true;
						Logger.Log($"< {msgID}");
						break;
					case TcpMessage.MessageID.FILE_TRANSFER_END:
						endLoop = true;
						Logger.Log($"< {msgID}");
						break;
					case TcpMessage.MessageID.GET_DIR_END:
						endLoop = true;
						_getDir = false;
						Logger.Log($"< {msgID}");
						break;
					case TcpMessage.MessageID.FILE_DATA:
						if (tcpMessage.Item2.IsNotNullOrEmpty())
						{
							Logger.Log($"< {msgID} {tcpMessage.Item2.Length}");
							_fsWrite.Write(tcpMessage.Item2, 0, tcpMessage.Item2.Length);
						}
						else
						{
							Logger.Log($"< {msgID}");
						}
						_streamSocket.SendBytes(FILE_MSG_ACK_BYTES);
						break;
					case TcpMessage.MessageID.FILE_DATA_CLOSE:
						Logger.Log($"< {msgID}");
						_streamSocket.SendBytes(FILE_MSG_ACK_BYTES);
						Close();
						break;
					case TcpMessage.MessageID.ERROR:
						Logger.Log($"< {msgID}");
						Close();
						break;
					case TcpMessage.MessageID.CLONE_PROGRESS_PERCENTAGE:
						Logger.Log($"< [{tcpMessage.Item2[0]}%] cloned...");
						break;
				}
				if (!endLoop)
				{
					tcpMessage = ReadTcpMessage();
					msgID = TcpMessage.GetMessageID(tcpMessage.Item1);
				}

				//if (msgID != TcpMessage.MessageID.CLONE_END)
				//{
				//	tcpMessage = ReadTcpMessage();
				//	msgID = TcpMessage.GetMessageID(tcpMessage.Item1);
				//	if (msgID == TcpMessage.MessageID.CLONE_END)
				//	{
				//		Logger.Log($"< {msgID}");
				//	}
				//}
				//else
				//{
				//	Logger.Log($"< {msgID}");
				//	//Logger.Log("cloning END.....");
				//}
			}
		}

		private Tuple<byte[], byte[]> ReadTcpMessage()
		{
			//Tuple < byte[], byte[]> tcpMessage=new Tuple<byte[], byte[]>();
			byte[] headerBytes = new byte[TcpMessage.HEADER_LENGTH];
			ReadTcpMessage(headerBytes);
			int msgLength = TcpMessage.GetMessageLength(headerBytes);
			byte[] msgBytes = null;
			if (msgLength > 0)
			{
				msgBytes = new byte[msgLength];
				ReadTcpMessage(msgBytes);
			}

			return new Tuple<byte[], byte[]>(headerBytes, msgBytes);
		}

		private class ReadMsgToken
		{
			public byte[] Buffer { get; set; }
			public AutoResetEvent Event { get; set; }
			public int NumOfBytesRead { get; set; }
			public Socket Socket { get; set; }
		}

		private void ReadTcpMessage(byte[] headerBytes)
		{
			int numOfBytesRead = 0;
			ReadMsgToken readMsgToken = new ReadMsgToken()
			{
				Buffer = headerBytes,
				Event = new AutoResetEvent(false),
				NumOfBytesRead = 0,
				Socket = _streamSocket
			};

			//while (_streamSocket.IsConnected() && numOfBytesRead < headerBytes.Length)
			//{
			//	numOfBytesRead += _streamSocket.Receive(headerBytes, numOfBytesRead, headerBytes.Length - numOfBytesRead,
			//		SocketFlags.None);
			//}

			IAsyncResult result = _streamSocket.BeginReceive(headerBytes, numOfBytesRead, headerBytes.Length - numOfBytesRead,
				SocketFlags.None, ReadMsgCallback, readMsgToken);

			bool gotSignalInTime = result.AsyncWaitHandle.WaitOne(5000);
			if (!gotSignalInTime)
			{
				throw new TimeoutException("could not get message in 5 s");
			}
			readMsgToken.Event.WaitOne();
			//while (_streamSocket.IsConnected() && numOfBytesRead < headerBytes.Length)
			//{
			//	numOfBytesRead += _streamSocket.Receive(headerBytes, numOfBytesRead, headerBytes.Length - numOfBytesRead,
			//		SocketFlags.None);
			//}
		}

		private void ReadMsgCallback(IAsyncResult ar)
		{
			ReadMsgToken readMsgToken = ar.AsyncState as ReadMsgToken;
			try
			{
				if (readMsgToken != null)
				{
					readMsgToken.NumOfBytesRead += readMsgToken.Socket.EndReceive(ar);
					while (readMsgToken.NumOfBytesRead < readMsgToken.Buffer.Length)
					{
						readMsgToken.NumOfBytesRead += _streamSocket.Receive(readMsgToken.Buffer, readMsgToken.NumOfBytesRead,
							readMsgToken.Buffer.Length - readMsgToken.NumOfBytesRead, SocketFlags.None);
					}
					readMsgToken.Event.Set();
				}
			}
			catch (Exception exception)
			{
				Logger.LogError($"{nameof(ReadMsgCallback)} {exception}");
				Close();
				throw;
			}
			finally
			{
				readMsgToken?.Event.Set();
			}
		}

		private FileStream _fsWrite;
		private readonly Socket _streamSocket;

		private string CreateFilePath(string fileName)
		{
			string tempFileName = Path.GetFileName(fileName);
			tempFileName = tempFileName.RemoveInvalidChars();
			tempFileName = Path.Combine(Path.GetDirectoryName(fileName), tempFileName);
			string filePath = Path.Combine(OptionConfiguration.GetRootFolder(), tempFileName);
			return filePath;
		}

		private string CreateDownloadFilePath(string fileName)
		{
			string tempFileName = Path.GetFileName(fileName);
			tempFileName = tempFileName.RemoveInvalidChars();
			//tempFileName = Path.Combine(Path.GetDirectoryName(fileName), tempFileName);
			string filePath = Path.Combine(OptionConfiguration.GetDownloadFolder(), tempFileName);
			return filePath;
		}
		private void CreateFile(string filePath)
		{
			//string filePath = Path.Combine(OptionConfiguration.GetRootFolder(), fileName);

			try
			{
				//string tempFileName = Path.GetFileName(fileName);
				//tempFileName = tempFileName.RemoveInvalidChars();
				//tempFileName = Path.Combine(Path.GetDirectoryName(fileName), tempFileName);
				//string filePath = Path.Combine(OptionConfiguration.GetRootFolder(), tempFileName);

				//filePath = Path.Combine(OptionConfiguration.GetRootFolder(), fileName);
				_fsWrite = new FileStream(filePath, FileMode.Create);
			}
			catch (ArgumentException argumentException)
			{
				Console.WriteLine($"Error {argumentException.Message}");
				throw;
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Error creating file {filePath} {exception}");
				throw;
			}
			//finally
			//{
			//	if (_fsWrite != null)
			//	{
			//		_fsWrite.Close();
			//	}
			//}
		}

		private void CreateDirectory(string dirName)
		{
			string path = Path.Combine(OptionConfiguration.GetRootFolder(), dirName);
			Directory.CreateDirectory(path);
		}
	}
}