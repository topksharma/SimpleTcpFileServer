using System.Net.Sockets;
using System.Threading;

namespace PRepo.Tcp.Client
{
	public class BaseToken
	{
		public Socket Socket { get; set; }
		public AutoResetEvent Event { get; set; }
	}
}