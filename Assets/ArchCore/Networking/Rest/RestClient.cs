using System;
using System.Collections.Generic;
using ArchCore.Networking.Rest.Config;
using ArchCore.Networking.Rest.Converter;
using BestHTTP;

namespace ArchCore.Networking.Rest
{
	public class RestClient
	{

		private IRestConfig config;
		private IObjectConverter converter;

		private readonly Dictionary<string, string> globalHeaders = new Dictionary<string, string>();

		public RestClient(IRestConfig config, IObjectConverter converter)
		{
			this.config = config;
			this.converter = converter;
		}

		public void AddGlobalHeader(string key, string value)
		{
			globalHeaders[key] = value;
		}
		
		public RestRequest<T, RestError> Get<T>(string path)
		{
			return CreateRequest<T, RestError>(path, HTTPMethods.Get);
		}

		public RestRequest<T, RestError> Post<T>(string path, object body)
		{
			return CreateRequest<T, RestError>(path, HTTPMethods.Post).AddBody(body);
		}

		public RestRequest<T, RestError> Post<T>(string path)
		{
			return CreateRequest<T, RestError>(path, HTTPMethods.Post);
		}

		public RestRequest<T, RestError> Put<T>(string path, object body)
		{
			return CreateRequest<T, RestError>(path, HTTPMethods.Put).AddBody(body);
		}

		public RestRequest<T, RestError> Delete<T>(string path)
		{
			return CreateRequest<T, RestError>(path, HTTPMethods.Delete);
		}
		
		RestRequest<T, TError> CreateRequest<T, TError>(string path, HTTPMethods method) where TError : RestError, new()
		{
			return new RestRequest<T, TError>(converter, config.BaseUrl + path, method, config.GlobalQueryParams,
				globalHeaders);
		}
	}
}