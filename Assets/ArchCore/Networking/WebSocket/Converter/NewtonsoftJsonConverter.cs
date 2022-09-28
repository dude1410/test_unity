using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ArchCore.Networking.WebSocket.Converter {
	public class NewtonsoftJsonConverter : WebSocketObjectConverter {
		
		private readonly JsonSerializerSettings settings;

		public NewtonsoftJsonConverter() {
			settings = new JsonSerializerSettings{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
			settings.Converters.Add(new StringEnumConverter());
			settings.Converters.Add(new IsoDateTimeConverter());
		}
		
		public T ToObject<T>(string data) {
			return JsonConvert.DeserializeObject<T>(data, settings);
		}

		public object ToObject(string data, Type type) {
			return JsonConvert.DeserializeObject(data, type);
		}

		public string ToString(object obj) {
			return JsonConvert.SerializeObject(obj, Formatting.None, settings);
		}
	}
}