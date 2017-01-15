using System.Threading;

namespace PRepo.Tcp.Server
{
	using System.Net.Sockets;

	public class AcceptSocketToken
	{
		public Socket ServerSocket { get; set; }
		public Socket ClientSocket { get; set; }
		public AutoResetEvent Event { get; set; }
	}
}