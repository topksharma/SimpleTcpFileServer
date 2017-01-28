using System;
using System.Collections.Generic;
using System.Threading;
using PRepo.Tcp.Common;

namespace PRepo.Tcp.Client
{
	public class CommandProducer
	{
		private readonly object _objLocker = new object();
		private readonly Thread _worker;
		private readonly Queue<string> _commandQueue = new Queue<string>();
		public CommandProducer(Func<bool> actionReconnect)
		{
			_actionReconnect = actionReconnect;
			_worker = new Thread(StartReadingCommand) { Name = "CommandReader" };
			//_worker.Start();
		}

		private bool _isStarted;
		public AutoResetEvent SignalAppExit { get; set; }

		public void Start()
		{
			if (!_isStarted)
			{
				_isStarted = true;
				_worker.Start();
			}
		}
		private void StartReadingCommand()
		{
			while (!_exit)
			{
				//Console.Write("-->");
				Console.Write("");
				string cmd = Console.ReadLine();
				lock (_objLocker)
				{
					if (!PeekCommand(cmd))
					{
						_commandQueue.Enqueue(cmd);
					}
				}
				_signalReader.Set();
			}
		}

		private bool _exit;
		private bool PeekCommand(string cmd)
		{
			cmd = cmd.ToLower();

			string[] commandStrings = cmd.Split(TcpCommon.REQUEST_SPLIT_CHAR);
			string strCommand = commandStrings[0];
			if (strCommand.ToLower() == "q")
			{
				_exit = true;
				SignalAppExit?.Set();
				return false;
			}
			if (ValidationHelper.IsCmdValid(strCommand))
			{
				TcpMessage.MessageID requestMessageId = TcpMessage.GetMessageID(strCommand);
				if (requestMessageId == TcpMessage.MessageID.RECONNECT)
				{
					if (_actionReconnect != null)
					{
						try
						{
							return _actionReconnect();
						}
						catch { }
					}
				}
			}
			return false;
		}

		private readonly AutoResetEvent _signalReader = new AutoResetEvent(false);
		private Func<bool> _actionReconnect;

		public string GetCommand()
		{
			int count = _commandQueue.Count;
			if (count == 0)
			{
				//Thread.Sleep(1000);
				_signalReader.WaitOne();
			}
			string cmd = string.Empty;
			lock (_objLocker)
			{
				if (_commandQueue.Count > 0)
				{
					cmd = _commandQueue.Dequeue();
				}
			}
			return cmd;
		}
	}
}