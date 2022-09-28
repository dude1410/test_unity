namespace ArchCore.Networking.WebSocket.Config {
	public interface WebSocketConfig {
		WebSocketSendDataFormat SendFormat { get; }
		string Url { get; }
		void SetUrl(string url);
	}
}