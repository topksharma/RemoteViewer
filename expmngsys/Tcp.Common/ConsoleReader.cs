using System;
using System.Threading;

namespace Tcp.Common
{
	public static class ConsoleReader
	{
		private static string _text = "";
		static ConsoleReader()
		{
			var thread = new Thread(ReadText);
			thread.Start();
		}

		private static void ReadText()
		{
			while (true)
			{
				SignalReadLine.WaitOne();
				_text = Console.ReadLine();
				SignalLineRead.Set();
			}
		}

		private static readonly AutoResetEvent SignalReadLine = new AutoResetEvent(false);
		private static readonly AutoResetEvent SignalLineRead = new AutoResetEvent(false);
		public static string ReadLine(int timeoutMs)
		{
			SignalReadLine.Set();
			bool timedOut = SignalLineRead.WaitOne(timeoutMs);
			if (!timedOut)
			{
				throw new TimeoutException("Operation timed out.");
			}
			return _text;
		}
	}
}
