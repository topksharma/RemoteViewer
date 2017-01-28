using System;
using System.IO;

namespace SrvCommon
{
	public interface ILogger
	{
		void Log(string msg);
	}

	public class FileLogger : ILogger
	{
		private readonly string _fileName;
		private readonly object ObjLocker = new object();
		public FileLogger(string fileName)
		{
			_fileName = fileName;
		}

		public void Log(string msg)
		{
			lock (ObjLocker)
			{
				using (StreamWriter sw = new StreamWriter(_fileName))
				{
					sw.WriteLine(string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), msg));
				}
			}
		}
	}

	public class ConsoleLogger : ILogger
	{
		public void Log(string msg)
		{
			Console.WriteLine(string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), msg));
		}
	}
}
