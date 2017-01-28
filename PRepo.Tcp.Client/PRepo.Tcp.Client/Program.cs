using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRepo.Tcp.Client
{
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("starting client, press 'q' to quit.");
			ThreadPool.QueueUserWorkItem(WaitAndCatchBroadcast);
			ConsoleKeyInfo keyInfo = Console.ReadKey();
			while (keyInfo.KeyChar != 'q')
			{
				keyInfo = Console.ReadKey();
			}
			Console.WriteLine("quitting application");
			Thread.Sleep(5000);
		}

		private static EndPoint _remotEndPoint = new IPEndPoint(IPAddress.Any, 0);
		private static void WaitAndCatchBroadcast(object state)
		{
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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

			asyncResult.AsyncWaitHandle.WaitOne();
			// call finished
			broadcastToken.Event.WaitOne();

			if (broadcastToken.NumOfBytesRead > 0)
			{
				string broadcastMessage = Encoding.Default.GetString(broadcastToken.Buffer, 0, broadcastToken.NumOfBytesRead);
				Console.WriteLine($"msg from server {broadcastMessage}");
			}
		}

		private static void ReceiveBroadcastCallback(IAsyncResult ar)
		{
			BroadcastToken broadcastToken = ar.AsyncState as BroadcastToken;

			if (broadcastToken != null)
			{
				int numOfBytesRead = broadcastToken.Socket.EndReceiveFrom(ar, ref broadcastToken.RemoteEndPoint);
				broadcastToken.NumOfBytesRead = numOfBytesRead;
				broadcastToken.Event.Set();
			}
		}
	}
}
