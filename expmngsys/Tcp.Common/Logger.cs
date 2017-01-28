using System;

namespace Tcp.Common
{
	using System.IO;

	public static class Logger
	{
		private static string _rootLogFolder = @"c:\praveendata";
		//static Logger()
		//{
		//	_logFileName = Path.Combine(_rootLogFolder, "log.txt");
		//}

		public static string LogFileName;
		private static string _logFileName;
		public static void Create()
		{
			if (!Directory.Exists(_rootLogFolder))
			{
				Directory.CreateDirectory(_rootLogFolder);
			}
			_logFileName = Path.Combine(_rootLogFolder, LogFileName);
			if (File.Exists(_logFileName))
			{
				File.Delete(_logFileName);
			}
		}
		private static object _objLocker = new object();
		public static void Log(string msg, string direction)
		{
			lock (_objLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{direction} {DateTime.Now.ToString("HH:mm:ss.fff")} {msg} ");
				}
			}
		}
		public static void Log(string msg)
		{
			lock (_objLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {msg} ");
				}
			}
		}

		public static void Exception(Exception exception, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
		{
			lock (_objLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{memberName} {DateTime.Now.ToString("HH:mm:ss.fff")} {exception.Message} ");
				}
			}
		}
	}
}
