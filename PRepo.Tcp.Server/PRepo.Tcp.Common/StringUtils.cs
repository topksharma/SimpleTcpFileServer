using System;
using System.IO;
using System.Text;

namespace PRepo.Tcp.Common
{
	public static class StringUtils
	{
		public static bool IsNotNullOrEmpty(this string str)
		{
			return !string.IsNullOrEmpty(str);
		}
		public static string AddChars(this string str, char repeatChar, int upto)
		{
			if (str.Length >= upto)
			{
				return str;
			}
			StringBuilder sb = new StringBuilder(str);
			sb.Append(repeatChar, upto - str.Length);
			return sb.ToString();
		}

		public static bool DirExists(this string strDirPath)
		{
			if (strDirPath.IsNotNullOrEmpty())
			{
				try
				{
					return Directory.Exists(strDirPath);
				}
				catch { }
			}
			return false;
		}

		public static bool CreateDirectory(this string strDirPath)
		{
			if (strDirPath.IsNotNullOrEmpty())
			{
				try
				{
					if (!Directory.Exists(strDirPath))
					{
						Directory.CreateDirectory(strDirPath);
					}
				}
				catch { }
			}
			return false;
		}

		public static string RemoveRootPath(this string path)
		{
			string tempPath = path.ToLower();
			if (tempPath.IsNotNullOrEmpty())
			{
				string root = OptionConfiguration.GetRootFolder().ToLower();
				if (tempPath.StartsWith(root))
				{
					path = path.Remove(0, root.Length);
					while (path.StartsWith(new string(Path.DirectorySeparatorChar, 1)))
					{
						path = path.Remove(0, 1);
					}
				}
			}
			return path;
		}

		public static string RemoveRootPath(this string path, string strToRemove)
		{
			string pathToRemove = strToRemove.ToLower();
			string tempPath = path.ToLower();
			if (tempPath.IsNotNullOrEmpty())
			{
				if (tempPath.StartsWith(pathToRemove))
				{
					path = path.Remove(0, strToRemove.Length);
					while (path.StartsWith(new string(Path.DirectorySeparatorChar, 1)))
					{
						path = path.Remove(0, 1);
					}
				}
			}
			return path.Trim();
		}
		public static string AddTabs(this string source, int count)
		{
			String tabString = new string('\t', count);
			//String tabString = new string(' ', count);
			return $"{tabString}{source}";
		}
	}
}
