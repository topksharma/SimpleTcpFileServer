using System;
using System.Collections;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PRepo.Tcp.Client
{
	using Common;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;

	partial class PTcpClient
	{
		private static readonly AutoResetEvent _signalAppExit = new AutoResetEvent(false);
		private static Timer _broadcastTimer;
		private static CommandProducer _commandProducer;
		static void Main(string[] args)
		{
			Console.Title = "Tcp file-client";
			//Console.BackgroundColor=ConsoleColor.Cyan;
			Console.ForegroundColor = ConsoleColor.Green;
			string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Logger.LogPath = basePath;
			Logger.LogFileName = "client.txt";
			Logger.Create();
			OptionConfiguration.ReadConfigurations(Path.Combine(basePath, OptionConfiguration.CONFIG_FILE_NAME));
			string rootFolder = OptionConfiguration.GetRootFolder();
			rootFolder.CreateDirectory();
			OptionConfiguration.GetDownloadFolder().CreateDirectory();
			Console.WriteLine("starting client, press 'q' to quit.");
			_broadcastTimer = new Timer(WaitAndCatchBroadcast, null, Timeout.Infinite, Timeout.Infinite);
			_broadcastTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
			_commandProducer = new CommandProducer(RestartBroadcast) {SignalAppExit = _signalAppExit};
			_commandProducer.Start();
			//ThreadPool.QueueUserWorkItem(WaitAndCatchBroadcast);
			//ConsoleKeyInfo keyInfo = Console.ReadKey();
			//while (keyInfo.KeyChar != 'q')
			//{
			//	keyInfo = Console.ReadKey();
			//}

			ExitApplication();
		}

		private static void ExitApplication()
		{
			_signalAppExit.WaitOne();
			Console.WriteLine();
			Console.WriteLine("quitting application");
			_broadcastTimer.Dispose();
			Thread.Sleep(3000);
		}
		private const int SERVER_BROADCAST_TIME_OUT = 10;
		private static void WaitAndCatchBroadcast(object state)
		{
			_broadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);
			_broadcastRestarted = true;
			Console.WriteLine("waiting for server broadcast...");
			Socket socket = null;
			try
			{
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				EndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 9090);
				BroadcastToken broadcastToken = new BroadcastToken()
				{
					Buffer = new byte[1024],
					RemoteEndPoint = serverEndPoint,
					Socket = socket,
					Event = new AutoResetEvent(false)
				};

				//IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
				//EndPoint tempRemoteEP = (EndPoint)serverEndPoint;
				socket.Bind(serverEndPoint);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

				//int bytesReceived = socket.ReceiveFrom(broadcastToken.Buffer, ref serverEndPoint);

				IAsyncResult asyncResult = broadcastToken.Socket.BeginReceiveFrom(broadcastToken.Buffer, 0, broadcastToken.Buffer.Length, SocketFlags.None,
					ref serverEndPoint, ReceiveBroadcastCallback, broadcastToken);

				bool gotSignalInTime = asyncResult.AsyncWaitHandle.WaitOne(SERVER_BROADCAST_TIME_OUT * 1000);
				if (!gotSignalInTime)
				{
					Console.WriteLine($"did not catch SERVER in {SERVER_BROADCAST_TIME_OUT}s, will try again, press 'q' to exit...");
					_broadcastTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.Zero);
				}
				else
				{
					// call finished
					broadcastToken.Event.WaitOne(SERVER_BROADCAST_TIME_OUT * 1000);

					if (broadcastToken.NumOfBytesRead > 0)
					{
						string broadcastMessage = Encoding.Default.GetString(broadcastToken.Buffer, 0, broadcastToken.NumOfBytesRead);
						Console.WriteLine($"msg from server {broadcastMessage}");
						if (broadcastMessage.StartsWith(TcpCommon.BROADCAST_PRETEXT))
						{
							// we got the right server, go ahead and establish connection
							ThreadPool.QueueUserWorkItem(StartCommunication, broadcastMessage);
						}
					}
				}
			}
			catch (SocketException socEx)
			{
				Logger.LogError($"WaitAndCatchBroadcast - {socEx.ErrorCode} {socEx}");
			}
			catch (Exception ex)
			{
				Logger.LogError($"WaitAndCatchBroadcast - {ex}");
			}
			finally
			{
				//socket?.Shutdown(SocketShutdown.Both);
				socket?.Close();
			}
		}
		public class ConnectToken
		{
			public Socket Socket { get; set; }
			public AutoResetEvent Event { get; set; }
		}
		private static CountdownEvent _cwEvent = new CountdownEvent(2);
		private static void StartCommunication(object state)
		{
			try
			{
				string msg = state as string;
				if (msg.IsNotNullOrEmpty())
				{
					string[] parts = msg.Split(TcpCommon.BROADCAST_SPLIT_CHAR);
					if (parts.Length >= 3)
					{
						string serverIP = parts[1];
						string serverPort = parts[2];

						IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), int.Parse(serverPort));
						Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

						ConnectToken connectToken = new ConnectToken()
						{
							Socket = socket,
							Event = new AutoResetEvent(false)
						};

						Console.WriteLine($"trying to connect to {serverIP}:{serverPort}");
						IAsyncResult result = socket.BeginConnect(serverEndPoint, BeginConnectCallback, connectToken);

						result.AsyncWaitHandle.WaitOne();
						connectToken.Event.WaitOne();
						// all went well
						_commandProducer.Start();
						Console.WriteLine($"connected to {serverIP}:{serverPort}, waiting for user commands...");
						// at this point we should start ReadMessage & WriteMessage threads
						ReadWriteToken readWriteToken = new ReadWriteToken()
						{
							Socket = socket,
							Event = new AutoResetEvent(false)
						};

						ThreadPool.QueueUserWorkItem(SendTcpRequest, readWriteToken);
						ThreadPool.QueueUserWorkItem(ReadTcpResponse, readWriteToken);

						_cwEvent.Wait();

						_cwEvent.Reset();
						//_cwEvent.AddCount(2);

						_broadcastRestarted = false;
						Console.WriteLine("client can try to re-connect");
					}
					else
					{
						// invalid message
						Logger.Log($"{nameof(StartCommunication)} invalid broadcast message {msg}");
					}
				}
			}
			catch (Exception exception)
			{
				Logger.Log($"{nameof(StartCommunication)}- {exception}");
			}
		}

		private static void ReadTcpResponse(object state)
		{
			ReadWriteToken readWriteToken = state as ReadWriteToken;
			ResponseHandler responseHandler = null;
			try
			{
				Socket socket = readWriteToken?.Socket;
				responseHandler = new ResponseHandler(socket);
				while (readWriteToken != null && socket.IsConnected())
				{
					// wait for Request Sender
					_signalResponseReader.WaitOne();
					// read response
					if (socket.IsConnected())
					{
						// try read Header first
						// create a token
						ResponseReaderToken responseReaderToken = new ResponseReaderToken()
						{
							Socket = socket,
							Event = new AutoResetEvent(false),
							Buffer = new byte[TcpMessage.HEADER_LENGTH]
						};

						IAsyncResult result = socket.BeginReceive(responseReaderToken.Buffer, 0, responseReaderToken.Buffer.Length, SocketFlags.None,
								ReadResponseHeaderCallback, responseReaderToken);

						result?.AsyncWaitHandle.WaitOne();
						responseReaderToken.Event.WaitOne();
						//
						if (responseReaderToken.HasError)
						{
							break;
						}
						else
						{
							// read rest of the message
							int msgLength = TcpMessage.GetMessageLength(responseReaderToken.Buffer);
							byte[] msgBytes = null;
							if (msgLength > 0)
							{
								msgBytes = new byte[msgLength];
								int numOfBytesRead = 0;
								while (numOfBytesRead < msgLength)
								{
									numOfBytesRead += responseReaderToken.Socket.Receive(msgBytes, numOfBytesRead,
							msgBytes.Length - numOfBytesRead, SocketFlags.None);
								}
								// done reading
								//responseHandler.HandleResponse(responseReaderToken.Buffer, msgBytes);
							}
							//else
							//{
							//	_signalCommandReader.Set();
							//}
							responseHandler.HandleResponse(responseReaderToken.Buffer, msgBytes);
							_signalCommandReader.Set();
						}
					}
				}
			}
			catch (SocketException socketException)
			{
				Logger.Log($"{nameof(ReadTcpResponse)}-{socketException.ErrorCode} {socketException}");
			}
			catch (Exception exception)
			{
				Logger.Log($"{nameof(ReadTcpResponse)}-{exception}");
			}
			finally
			{
				Console.WriteLine("SERVER DISCONNECTED");
				readWriteToken?.Socket.CloseSocket();
				responseHandler?.Close();
				_signalCommandReader.Set();
				_cwEvent.Signal();
			}
		}

		private static void ReadResponseHeaderCallback(IAsyncResult ar)
		{
			ResponseReaderToken responseReaderToken = ar.AsyncState as ResponseReaderToken;
			try
			{
				if (responseReaderToken != null)
				{
					int numOfBytesRead = responseReaderToken.Socket.EndReceive(ar);
					// do block reading
					while (numOfBytesRead < responseReaderToken.Buffer.Length)
					{
						numOfBytesRead += responseReaderToken.Socket.Receive(responseReaderToken.Buffer, numOfBytesRead,
							responseReaderToken.Buffer.Length - numOfBytesRead, SocketFlags.None);
					}
				}
			}
			catch (Exception)
			{
				if (responseReaderToken != null)
				{
					responseReaderToken.HasError = true;
				}
			}
			finally
			{
				responseReaderToken?.Event.Set();
			}
		}

		private static volatile bool _broadcastRestarted = false;
		private static bool RestartBroadcast()
		{
			if (!_broadcastRestarted)
			{
				_broadcastTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
				return true;
			}
			return false;
		}

		private static readonly AutoResetEvent _signalResponseReader = new AutoResetEvent(false);
		private static readonly AutoResetEvent _signalCommandReader = new AutoResetEvent(false);
		private static void SendTcpRequest(object state)
		{
			ReadWriteToken readWriteToken = state as ReadWriteToken;
			try
			{
				while (readWriteToken != null && readWriteToken.Socket.IsConnected())
				{
					string cmd = _commandProducer.GetCommand();
					//string cmd = Console.ReadLine();
					if (cmd.IsNotNullOrEmpty())
					//if (ReadCommandFromConsole(out cmd) )
					//if (ReadCommandFromConsole(out cmd))
					{
						cmd = cmd.ToLower();

						string[] commandStrings = cmd.Split(TcpCommon.REQUEST_SPLIT_CHAR);
						string strCommand = commandStrings[0];

						if (ValidationHelper.IsCmdValid(strCommand))
						{
							// send cmd to server
							TcpMessage.MessageID requestMessageId = TcpMessage.GetMessageID(strCommand);
							if (requestMessageId == TcpMessage.MessageID.RECONNECT)
							{
								//Console.Clear();
								RestartBroadcast();
								break;
							}
							if (!readWriteToken.Socket.IsConnected())
							{
								break;
							}
							if (!ValidationHelper.ValidateCommand(requestMessageId, commandStrings))
							{
								Console.WriteLine(TcpCommands.GetCommandText(requestMessageId));
								continue;
							}
							byte[] msgBytes = null;
							if (requestMessageId == TcpMessage.MessageID.GET_DIR_CLONE)
							{
								string strParameter = cmd.RemoveRootPath(strCommand);
								// then we must have second parameter
								msgBytes = TcpMessage.CreateMessage(requestMessageId, strParameter);
							}
							else if (requestMessageId == TcpMessage.MessageID.DIR_EXPLORE)
							{
								string strParameter = cmd.RemoveRootPath(strCommand);
								// then we must have second parameter
								msgBytes = TcpMessage.CreateMessage(requestMessageId, strParameter);
							}
							else if (requestMessageId == TcpMessage.MessageID.GET_FILE)
							{
								string strParameter = cmd.RemoveRootPath(strCommand);
								// then we must have second parameter
								msgBytes = TcpMessage.CreateMessage(requestMessageId, strParameter);
							}
							else
							{
								msgBytes = TcpMessage.CreateMessage(requestMessageId);
							}
							if (!readWriteToken.Socket.IsConnected())
							{
								break;
							}
							if (readWriteToken.Socket.SendBytes(msgBytes))
							{
								// set signal for Response-Reader
								_signalResponseReader.Set();
								// wait until response reader is finished
								_signalCommandReader.WaitOne();
							}
							else
							{
								// unable to send bytes
								break;
							}
						}
						else
						{
							Console.WriteLine("Unknown command, type +help to get list of all commands");
						}
					}
				}
			}
			catch (Exception exception)
			{
				Logger.Log($"{nameof(SendTcpRequest)} - {exception}");
			}
			finally
			{
				readWriteToken?.Socket?.Close();
				Console.WriteLine("CONNECTION CLOSED");
				_signalResponseReader.Set();
				_cwEvent.Signal();
			}
		}
		private static readonly AutoResetEvent _signalReadCommand = new AutoResetEvent(false);
		private static bool ReadCommandFromConsole(out string cmd)
		{
			cmd = Console.ReadLine();
			_signalReadCommand.Set();
			return true;
		}
		//private static bool ReadCommandFromConsole(out string cmd)
		//{
		//	cmd = String.Empty;
		//	AutoResetEvent signal = new AutoResetEvent(false);
		//	ReadCmdConsoleToken readCmdConsoleToken = new ReadCmdConsoleToken()
		//	{
		//		Event = signal
		//	};
		//	ThreadPool.QueueUserWorkItem(ReadCmdBlock, readCmdConsoleToken);
		//	bool gotSignalInTime = readCmdConsoleToken.Event.WaitOne(10000);
		//	if (gotSignalInTime)
		//	{
		//		cmd = readCmdConsoleToken.Command;
		//		return true;
		//	}
		//	return false;
		//}

		//private static void ReadCmdBlock(object state)
		//{
		//	ReadCmdConsoleToken readCmdConsoleToken = state as ReadCmdConsoleToken;
		//	if (readCmdConsoleToken != null)
		//	{
		//		readCmdConsoleToken.Command = Console.ReadLine();
		//		readCmdConsoleToken.Event.Set();
		//	}
		//}

		private static void BeginConnectCallback(IAsyncResult ar)
		{
			ConnectToken connectToken = ar.AsyncState as ConnectToken;
			if (connectToken != null)
			{
				connectToken.Socket.EndConnect(ar);
				connectToken.Event.Set();
			}
		}

		private static void ReceiveBroadcastCallback(IAsyncResult ar)
		{
			try
			{
				BroadcastToken broadcastToken = ar.AsyncState as BroadcastToken;

				if (broadcastToken != null)
				{
					int numOfBytesRead = broadcastToken.Socket.EndReceiveFrom(ar, ref broadcastToken.RemoteEndPoint);
					broadcastToken.NumOfBytesRead = numOfBytesRead;
					broadcastToken.Event.Set();
				}
			}
			catch (SocketException socEx)
			{
				Logger.LogError($"ReceiveBroadcastCallback - {socEx.ErrorCode} {socEx}");
			}
			catch (ObjectDisposedException objectDisposedException)
			{
				Logger.LogError($"ReceiveBroadcastCallback - {objectDisposedException}");
			}
			catch (Exception ex)
			{
				Logger.LogError($"ReceiveBroadcastCallback - {ex}");
			}
		}
	}
}
