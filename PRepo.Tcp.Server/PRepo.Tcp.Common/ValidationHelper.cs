using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PRepo.Tcp.Common
{
	public class TcpCommands
	{
		public static readonly Dictionary<string, TcpMessage.MessageID> COMMANDS = new Dictionary<string, TcpMessage.MessageID>()
		{
			{ "+help",TcpMessage.MessageID.GET_HELP},
			{ "+explore",TcpMessage.MessageID.GET_ALL_FROM_REPO},
			{ "+dirclone",TcpMessage.MessageID.GET_DIR_CLONE},
			{ "+status",TcpMessage.MessageID.GET_STATUS},
			{ "+direxplore",TcpMessage.MessageID.DIR_EXPLORE},
			{ "+getfile",TcpMessage.MessageID.GET_FILE},
			{ "+reconnect",TcpMessage.MessageID.RECONNECT},
		};

		public static TcpMessage.MessageID GetMsgIDByCommand(string cmd)
		{
			TcpMessage.MessageID msgID;
			if (COMMANDS.TryGetValue(cmd, out msgID))
			{
				return msgID;
			}
			return TcpMessage.MessageID.NONE;
		}

		public static string GetHelpText()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("+help         gets all the available commands");
			sb.AppendLine("+explore      explores all catalog in repository");
			sb.AppendLine("+direxplore   explores given direcetory, usage +direxplore [dirname]");
			sb.AppendLine("+getfile      fetches given file, usage +getfile [filename]");
			sb.AppendLine("+dirclone     clones the requested directory from repository");
			sb.AppendLine("+status       gets server status");
			return sb.ToString();
		}

		public static string GetRepoListAll()
		{
			StringBuilder sb = new StringBuilder();
			foreach (string dir in Directory.GetDirectories(OptionConfiguration.GetRootFolder()))
			{
				DirectoryInfo dirInfo = new DirectoryInfo(dir);
				int numOfFiles = 0;
				long size = 0;
				Utils.GetDirectorySize(dirInfo, ref numOfFiles, ref size);
				sb.AppendLine($"{dirInfo.Name.AddChars(' ', 20)} {size.ToString().AddChars(' ', 15)} {numOfFiles}");
			}
			return sb.ToString();
		}

		public static string GetCommandText(TcpMessage.MessageID msgID)
		{
			if (msgID == TcpMessage.MessageID.GET_DIR_CLONE)
			{
				return "+dirclone [catalog/dir name]";
			}
			return $"no usage for {msgID}";
		}
	}
	public class ValidationHelper
	{
		public static bool IsCmdValid(string cmd)
		{
			try
			{
				if (TcpCommands.COMMANDS.ContainsKey(cmd.ToLower().Trim()))
				{
					return true;
				}
			}
			catch { }
			return false;
		}

		public static bool ValidateCommand(TcpMessage.MessageID requestMessageId, string[] commandStrings)
		{
			if (!commandStrings.IsNotNullOrEmpty())
			{
				return false;
			}
			int cmdLength = commandStrings.Length;
			switch (requestMessageId)
			{
				case TcpMessage.MessageID.GET_HELP:
					break;
				case TcpMessage.MessageID.GET_ALL_FROM_REPO:
					break;
				case TcpMessage.MessageID.GET_STATUS:
					break;
				case TcpMessage.MessageID.GET_DIR_CLONE:
					if (cmdLength < 2)
					{
						return false;
					}
					break;
			}
			return true;
		}
	}
}
