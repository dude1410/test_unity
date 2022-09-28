using System;

namespace ArchCore.Networking.WebSocket.Promise
{
	public class ConnectionPromise
	{

		public delegate void ConnectSuccessDelegate();

		public delegate void DisconnectSuccessDelegate();

		public delegate void ConnectFailDelegate(Exception e);

		public delegate void DisconnectFailDelegate(Exception e);

		private ConnectSuccessDelegate onConnectSuccess;
		private DisconnectSuccessDelegate onDisconnectSuccess;
		private ConnectFailDelegate onConnectFail;
		private DisconnectFailDelegate onDisconnectFail;

		public ConnectionPromise OnConnectSuccess(ConnectSuccessDelegate handler)
		{
			ConnectSuccessDelegate d = null;
			d = delegate
			{
				onConnectSuccess -= d;
				handler();
			};
			onConnectSuccess += d;
			return this;
		}

		public ConnectionPromise OnConnectFail(ConnectFailDelegate handler)
		{
			ConnectFailDelegate d = null;
			d = delegate(Exception e)
			{
				onConnectFail -= d;
				handler(e);
			};
			onConnectFail += d;
			return this;
		}

		public ConnectionPromise OnDisconnectSuccess(DisconnectSuccessDelegate handler)
		{
			DisconnectSuccessDelegate d = null;
			d = delegate
			{
				onDisconnectSuccess -= d;
				handler();
			};
			onDisconnectSuccess += d;
			return this;
		}

		public ConnectionPromise OnDisconnectFail(DisconnectFailDelegate handler)
		{
			DisconnectFailDelegate d = null;
			d = delegate(Exception e)
			{
				onDisconnectFail -= d;
				handler(e);
			};
			onDisconnectFail += d;
			return this;
		}

		public ConnectionPromise ConnectComplete(Exception e)
		{
			if (e == null)
			{
				if (onConnectSuccess != null)
				{
					onConnectSuccess();
				}
			}
			else
			{
				if (onConnectFail != null)
				{
					onConnectFail(e);
				}
			}

			return this;
		}

		public ConnectionPromise DisconnectComplete(Exception e)
		{
			if (e == null)
			{
				if (onDisconnectSuccess != null)
				{
					onDisconnectSuccess();
				}
			}
			else
			{
				if (onDisconnectFail != null)
				{
					onDisconnectFail(e);
				}
			}

			return this;
		}

		public void ClearAllHandlers()
		{
			onConnectSuccess = null;
			onDisconnectSuccess = null;
			onConnectFail = null;
			onDisconnectFail = null;
		}
	}
}