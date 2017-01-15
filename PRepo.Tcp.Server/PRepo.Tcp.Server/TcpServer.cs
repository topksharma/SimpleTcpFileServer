using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PRepo.Tcp.Common;

namespace PRepo.Tcp.Server
{
	using System.Threading;

	class TcpServer
	{
		private static Socket _sckServer;
		private static Timer _broadcastTimer;
		private const int BacklogConnections = 5;
		static void Main(string[] args)
		{
			try
			{
				Console.Title = "Tcp file server";
				Console.ForegroundColor = ConsoleColor.Yellow;
				string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				Logger.LogPath = basePath;
				Logger.LogFileName = "server.txt";
				Logger.Create();
				OptionConfiguration.ReadConfigurations(Path.Combine(basePath, OptionConfiguration.CONFIG_FILE_NAME));
				_sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
				EndPoint endPoint = new IPEndPoint(IPAddress.Any, TcpCommon.ServerPort);
				// bind the socket to the endpoint
				_sckServer.Bind(endPoint);
				// start listening
				_sckServer.Listen(BacklogConnections);
				Console.WriteLine($"server is listening on {TcpCommon.GetIpAddress()}:{TcpCommon.ServerPort}, press 'q' to quit.");
				Logger.Log("server is listening...");
				// start accepting client sockets
				ThreadPool.QueueUserWorkItem(StartAcceptingClientSockets);
				// start broadcast timer
				_broadcastTimer = new Timer(BroadcastTimerCallback, _sckServer.LocalEndPoint, Timeout.Infinite, Timeout.Infinite);
				_broadcastTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

				ConsoleKeyInfo keyInfo = Console.ReadKey();
				while (keyInfo.KeyChar != 'q')
				{
					keyInfo = Console.ReadKey();
				}
			}
			catch (SocketException socketException)
			{
				Logger.Log($"Error {socketException.ErrorCode} {socketException}");
				Console.WriteLine(socketException.ErrorCode);
			}
			catch (Exception exception)
			{
				Logger.Log($"Error {exception}");
				Console.WriteLine(exception.Message);
			}
		}

		private static void BroadcastTimerCallback(object state)
		{
			Socket socket = null;
			_broadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				// create socket
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				EndPoint broadcastPoint = new IPEndPoint(IPAddress.Broadcast, TcpCommon.BroadcastPort);
				//IPEndPoint localEndPoint = state as IPEndPoint;
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
				string ip = TcpCommon.GetIpAddress();
				string strBroadcast = $"{TcpCommon.BROADCAST_PRETEXT}{TcpCommon.BROADCAST_SPLIT_CHAR}{ip}{TcpCommon.BROADCAST_SPLIT_CHAR}{TcpCommon.ServerPort}";
				byte[] data = Encoding.Default.GetBytes(strBroadcast);
				socket.SendTo(data, data.Length, SocketFlags.None, broadcastPoint);
				//Console.WriteLine($"SENT {strBroadcast}");
				//Console.WriteLine($"SENT {strBroadcast} at {DateTime.Now:HH:mm:ss.fff}");
			}
			catch (Exception exception)
			{
				Console.WriteLine("broadcasting ex");
				Logger.Log(exception.ToString());
			}
			finally
			{
				socket?.Shutdown(SocketShutdown.Both);
				_broadcastTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.Zero);
			}
		}

		private static void StartAcceptingClientSockets(object state)
		{
			// run for ever
			while (true)
			{
				AcceptSocketToken acceptSocketToken = new AcceptSocketToken()
				{
					ServerSocket = _sckServer,
					Event = new AutoResetEvent(false)
				};
				IAsyncResult asyncResult = _sckServer.BeginAccept(AcceptCallback, acceptSocketToken);

				// wait until we have got a connection
				asyncResult.AsyncWaitHandle.WaitOne();
				// 
				acceptSocketToken.Event.WaitOne();
				// finished, hand-off client socket 
				ThreadPool.QueueUserWorkItem(HandleTcpClient, acceptSocketToken.ClientSocket);
			}
		}

		private static void HandleTcpClient(object state)
		{
			TcpClientHandler tcpClientHandler = new TcpClientHandler(state as Socket);
			tcpClientHandler.HandleClient();
		}

		private static void AcceptCallback(IAsyncResult ar)
		{
			AcceptSocketToken acceptSocketToken = ar.AsyncState as AcceptSocketToken;

			try
			{
				if (acceptSocketToken != null)
				{
					acceptSocketToken.ClientSocket = acceptSocketToken.ServerSocket.EndAccept(ar);
				}
			}
			catch { }
			finally
			{
				acceptSocketToken?.Event.Set();
			}
		}
	}
}
