using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ArchCore.Networking.WebSocket.Command;
using ArchCore.Networking.WebSocket.Config;
using ArchCore.Networking.WebSocket.Converter;
using ArchCore.Networking.WebSocket.Message;
using ArchCore.Networking.WebSocket.Promise;
using Newtonsoft.Json;
using UnityEngine;

namespace ArchCore.Networking.WebSocket {
	public abstract class WebSocketClient {

		public delegate void ServerPushDelegate(object basePush);
		public delegate void ServerErrorDelegate(Exception exception);
		
		private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
		private readonly Dictionary<string, Type> serverPushTypes = new Dictionary<string, Type>();
		private readonly Dictionary<string, ServerPushDelegate> serverPushHandlers = new Dictionary<string, ServerPushDelegate>();
		private readonly Dictionary<string, ServerErrorDelegate> serverErrorHandlers = new Dictionary<string, ServerErrorDelegate>();
		private readonly ConnectionPromise promise = new ConnectionPromise();
		protected WebSocketConfig config;
		protected WebSocketObjectConverter converter;
		protected BestHTTP.WebSocket.WebSocket socket;
		
		public ConnectionStatus ConnectionStatus { get; private set; }
		
		public bool HasActiveConnection {
			get { return (socket != null && socket.IsOpen); }
		}

		public WebSocketClient(WebSocketConfig config, WebSocketObjectConverter converter) {
			this.config = config;
			this.converter = converter;
			
		}

		public ConnectionPromise Connect() {
			if (!HasActiveConnection) {
				OpenSocket(config.Url);
			}

			return promise;
		}
		
		public ConnectionPromise Disconnect() {
			ClearSocket();
			return promise;
		}

		public CommandPromise<T> SendRequest<T>(WebSocketRequest request) where T : WebSocketResponse {
			return InternalSendRequest<T>(request);
		}
		
		public void RegisterServerPushHandler<T>(string pushCode, ServerPushDelegate handler, ServerErrorDelegate errorHandler = null) {
			serverPushTypes[pushCode] = typeof(T);
			serverPushHandlers[pushCode] = handler;
			serverErrorHandlers[pushCode] = errorHandler;
		}

		public void UnRegisterServerPushHandler(string pushCode)
		{
			serverPushTypes.Remove(pushCode);
			serverPushHandlers.Remove(pushCode);
			serverErrorHandlers.Remove(pushCode);
		}

		protected CommandPromise<T> InternalSendRequest<T>(WebSocketRequest request) where T : WebSocketResponse {
			var command = new Command<T>(request);
			commands.Add(command.Id, command);
			Send(command);
			return command.Promise;
		}
		
		private void OpenSocket(string server) {
			Debug.LogFormat("<WS> Connecting to server {0}", server);
			ConnectionStatus = ConnectionStatus.Connecting;
			socket = new BestHTTP.WebSocket.WebSocket(new Uri(server));

			socket.OnOpen += HandleWebSocketOpen;
			socket.OnClosed += HandleWebSocketClosed;
			socket.OnMessage += HandleMessageReceived;
			socket.OnError += HandleError;

			ConfigureSocket(socket);
			socket.Open();
		}

		private void Send<T>(Command<T> command) where T : WebSocketResponse {
			if (config.SendFormat == WebSocketSendDataFormat.String) {
				var msg = EncodeToString(command);
				Debug.LogFormat("<WS> SND: {0}", msg);
				socket.Send(msg);
			}
			else if(config.SendFormat == WebSocketSendDataFormat.ByteArray) {
				var msg = EncodeToByteArray(command);
				Debug.LogFormat("<WS> SND: {0}", msg);
				socket.Send(msg);
			}
		}
		
		private void Decode(string message) {
			try {
				Debug.LogFormat("<WS> RCV {0}", message);
				var info = DecodeMessageInfo(message);
				switch (info.Type) {
					case WebSocketMessageType.Response:
						ProcessResponse(info);
						Debug.LogFormat("<WS> RSP {0}", message);
						break;
					/*case SocketMessageType.Request:
			            processRequest(info, length, message);
			            break;*/
					case WebSocketMessageType.Message:
						Debug.LogFormat("<WS> PUSH {0}", message);
						ProcessMessage(info);
						break;
					case WebSocketMessageType.Unsupported:
						Debug.Log("<WS> Unsupported context: " + message);
						break;
					default:
						Debug.LogErrorFormat("<WS> Unknown Message Type {0}", info.Type);
						break;
				}
			}
			catch (JsonSerializationException e) {
				Debug.LogWarning(e.Message);
				Debug.Log("<WS> Unsupported or System Message: " + message);
			}
		}

		protected virtual string EncodeToString(ICommand command) {
			return converter.ToString(command.Request);
		}

		protected virtual byte[] EncodeToByteArray(ICommand command) {
			var message = converter.ToString(command.Request);
			return Encoding.UTF8.GetBytes(message);
		}

		protected virtual void ConfigureSocket(BestHTTP.WebSocket.WebSocket socket) {
		}

		protected abstract WebSocketMessageInfo DecodeMessageInfo(string message);
		protected abstract object DecodeResponse(string data, Type type);
		protected abstract object DecodeServerPush(string data, Type type);

		private void ProcessResponse(WebSocketMessageInfo info) {
			var command = commands[info.Id];
			if (info.Exception == null) {
				command.SetResponse(DecodeResponse(info.Data, command.ResponseType));
				command.Success();
			}
			else {
				command.Fail(info.Exception);
			}
		}
		
		private void ProcessMessage(WebSocketMessageInfo info) {
			if (info.Exception == null) {
				if (!string.IsNullOrEmpty(info.ServerPushCode) && serverPushHandlers.TryGetValue(info.ServerPushCode, out var handler)) {
					object push = DecodeServerPush(info.Data, serverPushTypes[info.ServerPushCode]);
					
					handler(push);
				}
			}
			else {
				Debug.Log($"<WS> EXCEPTION=[{info.ServerPushCode}] {info.Exception.Message}");
				if (serverErrorHandlers.TryGetValue(info.ServerPushCode, out var handler))
				{
					handler?.Invoke(info.Exception);
				}
			}
		}
		
		private void ClearSocket() {
			if (socket != null && socket.IsOpen) {
				socket.OnOpen -= HandleWebSocketOpen;
				socket.OnClosed -= HandleWebSocketClosed;
				socket.OnMessage -= HandleMessageReceived;
				socket.OnError -= HandleError;
				socket.Close();
			}
			socket = null;
			ConnectionStatus = ConnectionStatus.Disconnected;
		}
		
		protected virtual void HandleWebSocketOpen(BestHTTP.WebSocket.WebSocket socket) {
			Debug.Log("<WS> Connection Established!");
			ConnectionStatus = ConnectionStatus.Connected;
			promise.ConnectComplete(null);
		}

		protected virtual void HandleWebSocketClosed(BestHTTP.WebSocket.WebSocket webSocket, ushort code, string msg) {
			Debug.LogFormat("<WS> Connection Closed (code:{0}, message:{1})", code, msg);
			ClearSocket();
			promise.DisconnectComplete(null);
		}

		protected virtual void HandleMessageReceived(BestHTTP.WebSocket.WebSocket socket, string msg) {
			Decode(msg);
		}
		
		protected virtual void HandleError(BestHTTP.WebSocket.WebSocket socket,  string reason) {
		    ClearSocket();
		    promise.DisconnectComplete(new Exception(reason));
		}
		
	}
}