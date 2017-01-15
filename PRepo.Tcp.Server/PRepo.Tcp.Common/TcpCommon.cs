namespace PRepo.Tcp.Common
{
	using System.Net;
	using System.Net.Sockets;

	/// <summary>
	/// The tcp common.
	/// </summary>
	public class TcpCommon
	{
		/// <summary>
		/// The port for server.
		/// </summary>
		public const int ServerPort = 11888;
		public const int BroadcastPort = 9090;
		public const char BROADCAST_SPLIT_CHAR = ':';
		public const char REQUEST_SPLIT_CHAR = ' ';
		public const string BROADCAST_PRETEXT = "CATCHME";
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
	}
}
