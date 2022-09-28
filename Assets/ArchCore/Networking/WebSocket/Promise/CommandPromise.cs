using System;
using ArchCore.Networking.WebSocket.Command;

namespace ArchCore.Networking.WebSocket.Promise {
	public class CommandPromise<T> where T : WebSocketResponse {
		
		public delegate void SuccessDelegate(T response);

		public delegate void FailDelegate(Exception error);

		public delegate void CompleteDelegate(T response, Exception error, Command<T> command);

		private SuccessDelegate onSuccess;
		private FailDelegate onFail;
		private CompleteDelegate onComplete;

		public CommandPromise(Command<T> command)
		{
			Command = command;
		}

		public Command<T> Command { get; set; }

		public CommandPromise<T> OnSuccess(SuccessDelegate handler)
		{
			SuccessDelegate d = null;
			d = delegate(T data)
			{
				handler(data);
				onSuccess -= d;
			};
			onSuccess += d;
			return this;
		}

		public CommandPromise<T> OnFail(FailDelegate handler)
		{
			FailDelegate d = null;
			d = delegate(Exception error)
			{
				onFail -= d;
				handler(error);
			};
			onFail += d;
			return this;
		}

		public CommandPromise<T> OnComplete(CompleteDelegate handler)
		{
			CompleteDelegate d = null;
			d = delegate(T data, Exception error, Command<T> command)
			{
				onComplete -= d;
				handler(data, error, command);
			};
			onComplete += d;
			return this;
		}

		public CommandPromise<T> Success()
		{
			if (onSuccess != null)
			{
				onSuccess((T) Command.Response);
			}
			if (onComplete != null)
			{
				onComplete((T) Command.Response, null, Command);
			}
			return this;
		}

		public CommandPromise<T> Fail(Exception error)
		{
			if (onFail != null)
			{
				onFail(error);
			}
			if (onComplete != null)
			{
				onComplete(null, error, Command);
			}
			return this;
		}		

	}
}