using System.IO;

namespace PRepo.Tcp.Common
{
	using System;

	public class Logger
	{
		//private static string _rootLogFolder = @"c:\praveendata";
		//static Logger()
		//{
		//	_logFileName = Path.Combine(_rootLogFolder, "log.txt");
		//}

		public static string LogFileName = "log.txt";
		private static string _logFileName;
		public static string LogPath { get; set; }

		public static void Create()
		{
			if (!Directory.Exists(LogPath))
			{
				Directory.CreateDirectory(LogPath);
			}
			_logFileName = Path.Combine(LogPath, LogFileName);
			if (File.Exists(_logFileName))
			{
				File.Delete(_logFileName);
			}
		}
		private static readonly object ObjLocker = new object();
		public static void Log(string msg, string direction)
		{
			lock (ObjLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{direction} {DateTime.Now:HH:mm:ss.fff} {msg} ");
				}
			}
		}
		public static void Log(string msg)
		{
			lock (ObjLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {msg} ");
				}
				Console.WriteLine(msg);
			}
		}
		public static void LogError(string msg)
		{
			lock (ObjLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {msg} ");
				}
				//Console.WriteLine(msg);
			}
		}

		public static void Exception(Exception exception, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
		{
			lock (ObjLocker)
			{
				using (StreamWriter sw = new StreamWriter(_logFileName, true))
				{
					sw.WriteLine($"{memberName} {DateTime.Now:HH:mm:ss.fff} {exception.Message} ");
				}
			}
		}
	}
}