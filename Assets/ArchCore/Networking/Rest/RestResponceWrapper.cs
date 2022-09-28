using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace ArchCore.Networking.Rest
{
	public class RestResponseWrapper<TData>
	{
		public bool success;
		public TData data;
		
		public RestWrapperError errorData;
	}

	public class RestWrapperError
	{
		public int code;
		public string message;
	}
}