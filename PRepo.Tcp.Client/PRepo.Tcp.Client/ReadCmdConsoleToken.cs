using System.Threading;

namespace PRepo.Tcp.Client
{
	public class ReadCmdConsoleToken
	{
		public AutoResetEvent Event { get; set; }
		public string Command { get; set; }
	}

}