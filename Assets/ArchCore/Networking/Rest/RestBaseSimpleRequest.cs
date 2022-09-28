using System;
using System.Collections.Generic;
using BestHTTP;
using UnityEngine;

namespace ArchCore.Networking.Rest {
    public class RestBaseSimpleRequest : RestBaseRequest {
        
        public delegate void RequestSuccessDelegate(HTTPResponse data);
		public delegate void RequestErrorDelegate(Exception e);
	    public delegate void RequestCompleteDelegate(HTTPResponse data, Exception e);

		private readonly string path;
		private readonly HTTPMethods method;

		private Dictionary<string, string> headers = new Dictionary<string, string>();
		private Dictionary<string, object> queryParams = new Dictionary<string, object>();
		private RequestSuccessDelegate onSuccess;
		private RequestErrorDelegate onFail;
	    private RequestCompleteDelegate onComplete;
		private byte[] body;

		public RestBaseSimpleRequest(string path, HTTPMethods method) {
			this.path = path;
			this.method = method;
		}

		public RestBaseSimpleRequest AddQueryParam(string key, object value) {
			queryParams[key] = value;
			return this;
		}

		public RestBaseSimpleRequest AddHeader(string key, string value) {
			headers[key] = value;
			return this;
		}

		public RestBaseSimpleRequest AddBody(byte[] body) {
			this.body = body;
			return this;
		}

		public RestBaseSimpleRequest OnSuccess(RequestSuccessDelegate onSuccess) {
			this.onSuccess = onSuccess;
			return this;
		}
		
		public RestBaseSimpleRequest OnFail(RequestErrorDelegate onFail) {
			this.onFail = onFail;
			return this;
		}
	    
	    public RestBaseSimpleRequest OnComplete(RequestCompleteDelegate onComplete) {
		    this.onComplete = onComplete;
		    return this;
	    }

		public void Send() {
			HTTPRequest request = new HTTPRequest(CreateUri(path, queryParams), method,
				delegate(HTTPRequest originalRequest, HTTPResponse response) {
					try {
						HTTPResponse data = processResponse(originalRequest, response);
						Debug.Log("<REST> RESPONSE Uri: " + originalRequest.Uri + " Response: " + response.DataAsText);
						if (onComplete != null) {
							onComplete(data, null);
						}
						if (onSuccess != null) {
							onSuccess(data);
						}
					}
					catch (Exception e) {
						Debug.Log("<REST> ERROR: " + e.Message);
						if (onComplete != null) {
							onComplete(null, e);
						}
						if (onFail != null) {
							onFail(e);
						}
					}
				});

			foreach (var header in headers) {
				request.AddHeader(header.Key, header.Value);
			}

			if (body != null) {
				request.RawData = body;
				Debug.Log("<REST> CALLING SERVER Uri: " + request.Uri + "\nBody: " + body);
			}
			else {
				Debug.Log("<REST> CALLING SERVER Uri: " + request.Uri);				
			}
			
			request.Send();
		}
		
		private HTTPResponse processResponse(HTTPRequest request, HTTPResponse response) {
			if (response == null || !response.IsSuccess) {
				throw request.Exception ?? new Exception("Unknown Exception");
			}

			return response;
		}
    }
}