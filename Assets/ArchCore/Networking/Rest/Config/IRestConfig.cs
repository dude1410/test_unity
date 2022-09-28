using System.Collections.Generic;

namespace ArchCore.Networking.Rest.Config {
	public interface IRestConfig {
		string BaseUrl { get; }
		Dictionary<string, object> GlobalQueryParams { get; }
		
		void SetUrl(string url);
	}
}