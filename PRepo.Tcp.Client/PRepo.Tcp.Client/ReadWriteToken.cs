using System.Net.Sockets;
using System.Threading;

namespace PRepo.Tcp.Client
{
	public class ReadWriteToken
	{
		public Socket Socket { get; set; }
		public AutoResetEvent Event { get; set; }
	}
}