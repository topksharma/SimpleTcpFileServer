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

	public class BroadcastToken
	{
		public byte[] Buffer { get; set; }
		public int NumOfBytesRead { get; set; }
		public EndPoint RemoteEndPoint;
		public Socket Socket { get; set; }
		public AutoResetEvent Event { get; set; }
	}
}
