using System;
using System.Runtime.Serialization;

namespace ArchCore.Networking.WebSocket.Message {
	public sealed class WebSocketMessageInfo {
		public WebSocketMessageInfo(WebSocketMessageType type, string id, Exception exception, string data, string serverPushCode) {
			Type = type;
			Id = id;
			Exception = exception;
			Data = data;
			ServerPushCode = serverPushCode;
		}

		public WebSocketMessageType Type { get; private set; }

		public string Id { get; private set; }

		public Exception Exception { get; private set; }
		
		public string Data { get; private set; }
		
		public string ServerPushCode { get; private set; }

		public bool IsRequest {
			get { return Type == WebSocketMessageType.Request; }
		}

		public bool IsMessage {
			get { return Type == WebSocketMessageType.Message; }
		}

		public bool IsResponse {
			get { return Type == WebSocketMessageType.Response; }
		}
	}
}