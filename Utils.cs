using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRepo.Tcp.Common
{
	public static class Utils
	{
		public static string RemoveInvalidChars(this string fileName)
		{
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}
		public static void GetDirectorySize(DirectoryInfo dInfo, ref int numOfFiles, ref long size)
		{
			try
			{
				if (dInfo.Exists)
				{
					//DirectoryInfo dInfo = new DirectoryInfo(dirName);
					FileInfo[] files = dInfo.GetFiles();
					numOfFiles += files.Length;
					for (int i = 0; i < files.Length; i++)
					{
						size += files[i].Length;
					}

					foreach (DirectoryInfo info in dInfo.GetDirectories())
					{
						GetDirectorySize(info, ref numOfFiles, ref size);
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
