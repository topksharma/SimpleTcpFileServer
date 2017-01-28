namespace PRepo.Tcp.Client
{
	public class ResponseReaderToken : BaseToken
	{
		public byte[] Buffer { get; set; }
		public bool HasError { get; set; }
	}
}