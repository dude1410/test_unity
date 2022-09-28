using System;
using ArchCore.Networking.WebSocket.Message;

namespace ArchCore.Networking.WebSocket.Command {
	public interface ICommand {
		
		string Id { get; }
		WebSocketRequest Request { get; }
		Type ResponseType { get; }
		void SetResponse(object response);
		void Success();
		void Fail(Exception error);
	}
}