using System;

namespace ArchCore.Networking.WebSocket.Converter {
	public interface WebSocketObjectConverter {
		T ToObject<T>(string data);
		object ToObject(string data, Type type);
		string ToString(object obj);
	}
}