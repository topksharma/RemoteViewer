using System.Net.Sockets;
using System.Threading;

namespace SrvCommon
{
	public class Token
	{
		public AutoResetEvent Event { get; set; }
	}

	public class AcceptSocketToken : Token
	{
		public Socket Socket { get; set; }
	}

	public class ReadMessageToken : Token
	{
		public byte[] Buffer { get; set; }
		public Socket Socket { get; set; }
	}

	public class ConnectToServerToken : Token
	{
		public Socket Socket { get; set; }

	}
}
