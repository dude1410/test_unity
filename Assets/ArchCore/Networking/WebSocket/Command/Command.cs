using System;
using ArchCore.Networking.WebSocket.Promise;
using ArchCore.Networking.WebSocket.Message;

namespace ArchCore.Networking.WebSocket.Command {
	public class Command<T> : ICommand where T : WebSocketResponse {
		private readonly CommandPromise<T> promise;

		public CommandPromise<T> Promise { get { return promise; } }

		public Command(WebSocketRequest request) {
			Request = request;
			ResponseType = typeof(T);
			promise = new CommandPromise<T>(this);
		}

		public string Id {
			get { return Request.RequestId; }
		}

		public WebSocketRequest Request { get; private set; }
		public Type ResponseType { get; private set; }
		
		public T Response { get; private set; }

		public void SetResponse(object response) {
			Response = (T) response;
		}

		public void Success() {
			promise.Success();
		}

		public void Fail(Exception error) {
			promise.Fail(error);
		}
	}
}