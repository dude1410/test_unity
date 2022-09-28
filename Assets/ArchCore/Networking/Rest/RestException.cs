using System;

namespace ArchCore.Networking.Rest
{
	public class RestException : Exception
	{
		public readonly RestError error;

		public RestException(RestError error, string message) : base(message)
		{
			this.error = error;
		}

		public RestException(RestError error) : base(error.exceptionMessage)
		{
			this.error = error;
		}
	}
}