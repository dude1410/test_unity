using System;
using System.Collections.Generic;
using System.Text;
using ArchCore.Networking.Rest.Converter;
using BestHTTP;
using BestHTTP.Forms;
using UnityEngine;

namespace ArchCore.Networking.Rest
{
    [Flags]
    public enum RestRequestConfig
    {
        Default = 0,
        Silent = 1,
        AllowEmptyResponse = 2,
        AllowInvalidResponse = 4,
    }

    public sealed class RestRequest<T, TError> : RestBaseRequest where TError : RestError, new()
    {
        public delegate void RequestSuccessDelegate(RestResponse<T> response);

        public delegate void RequestErrorDelegate(TError error);

        public delegate void RequestCompleteDelegate(RestResponse<T> response, TError error);

        public delegate void RequestUploadProgressDelegate(float progress);

        private readonly IObjectConverter converter;
        private readonly string path;
        private readonly HTTPMethods method;
        private bool Verbose => (config & RestRequestConfig.Silent) == 0;
        private bool AllowEmptyResponse => (config & RestRequestConfig.AllowEmptyResponse) != 0;
        private bool AllowInvalidResponse => (config & RestRequestConfig.AllowInvalidResponse) != 0;

        private RestRequestConfig config;

        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private Dictionary<string, object> queryParams = new Dictionary<string, object>();
        private RequestSuccessDelegate onSuccess;
        private RequestErrorDelegate onFail;
        private RequestCompleteDelegate onComplete;
        private RequestUploadProgressDelegate onUploadProgress;
        private object body;
        private string queryBody;
        private byte[] bytes;
        private HTTPMultiPartForm form;
        private bool showLog = true;

        public RestRequest(IObjectConverter converter, string path, HTTPMethods method,
            Dictionary<string, object> globalQueryParams, Dictionary<string, string> globalHeaders)
        {
            this.converter = converter;
            this.path = path;
            this.method = method;

            if (globalQueryParams != null)
            {
                foreach (var globalQueryParam in globalQueryParams)
                {
                    queryParams.Add(globalQueryParam.Key, globalQueryParam.Value);
                }
            }

            if (globalHeaders != null)
            {
                foreach (var header in globalHeaders)
                {
                    headers.Add(header.Key, header.Value);
                }
            }
        }

        public RestRequest<T, TError> Config(RestRequestConfig config)
        {
            this.config = config;
            return this;
        }

        public RestRequest<T, TError> AddQueryParam(string key, object value)
        {
            queryParams[key] = value;
            return this;
        }

        public RestRequest<T, TError> AddHeader(string key, string value)
        {
            headers[key] = value;
            return this;
        }

        private static readonly string AuthorizationHeader = "Authorization";
        
        public RestRequest<T, TError> AddBasicAuth(string username, string password)
        {
            var data = string.Concat("Basic ", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{username}:{password}")));
            Debug.Log($"{AuthorizationHeader}, {data}");
            headers[AuthorizationHeader] = data;
            return this;
        }

        public RestRequest<T, TError> AddBody(object body)
        {
            this.body = body;
            return this;
        }

        public RestRequest<T, TError> AddBodyQuery(string body)
        {
            queryBody = body;
            return this;
        }

        public RestRequest<T, TError> AddBinaryData(byte[] bytes)
        {
            this.bytes = bytes;
            return this;
        }

        public RestRequest<T, TError> AddFormData(string fieldName, byte[] content)
        {
            if (form == null)
            {
                form = new HTTPMultiPartForm();
            }

            form.AddBinaryData(fieldName, content);
            return this;
        }

        public RestRequest<T, TError> AddFormObjectData(string fieldName, object content)
        {
            if (form == null)
            {
                form = new HTTPMultiPartForm();
            }

            form.AddBinaryData(fieldName, converter.ToRawData(content), null, "application/json");
            return this;
        }

        public RestRequest<T, TError> OnSuccess(RequestSuccessDelegate onSuccess)
        {
            this.onSuccess = onSuccess;
            return this;
        }

        public RestRequest<T, TError> OnFail(RequestErrorDelegate onFail)
        {
            this.onFail = onFail;
            return this;
        }

        public RestRequest<T, TError> OnComplete(RequestCompleteDelegate onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }

        public RestRequest<T, TError> OnUploadProgress(RequestUploadProgressDelegate onUploadProgress)
        {
            this.onUploadProgress = onUploadProgress;
            return this;
        }

        public void Send()
        {
            HTTPRequest request = new HTTPRequest(CreateUri(path, queryParams), method,
                delegate(HTTPRequest originalRequest, HTTPResponse response)
                {
                    RestResponse<T> resp = null;
                    TError error = null;

                    if (originalRequest.Exception != null)
                    {
                        Log("<REST> ERROR Uri: " + originalRequest.Uri + " Exception: " +
                            originalRequest.Exception.Message);
                        error = new TError { exceptionMessage = originalRequest.Exception.Message };
                        InvokeComplete(resp ?? new RestResponse<T>(default, default), error);
                        return;
                    }

                    Log("<REST> RESPONSE Uri: " + originalRequest.Uri + " Response: " + response?.DataAsText);

                    try
                    {
                        resp = ProcessResponse(originalRequest, response);
                    }
                    catch (RestException restException)
                    {
                        Log("<REST> SERVER ERROR: " + restException.Message);
                        error = (TError)restException.error;
                    }
                    catch (Exception e)
                    {
                        Log("<REST> ERROR: " + e.Message);
                        error = new TError { exceptionMessage = e.Message };
                    }

                    InvokeComplete(resp ?? new RestResponse<T>(default, default), error);
                });
            //new HTTPRequest[]

            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }

            if (onUploadProgress != null)
            {
                request.OnUploadProgress += delegate(HTTPRequest originalRequest, long uploaded, long length)
                {
                    onUploadProgress(uploaded / (float)length);
                };
            }

            if (body != null)
            {
                request.RawData = converter.ToRawData(body);
                Log("<REST> CALLING SERVER Uri: " + request.Uri + "\nBody: " + converter.ToString(body));
            }
            else if (queryBody != null)
            {
                request.RawData = Encoding.UTF8.GetBytes(queryBody);
                Log("<REST> CALLING SERVER Uri: " + request.Uri + "\nBody: " + converter.ToString(queryBody));
            }
            else if (bytes != null)
            {
                request.AddBinaryData("reactionVideoInBytes", bytes);
                Log("<REST> CALLING SERVER Uri: " + request.Uri + "\nBinary Data length: " + bytes.Length);
            }
            else if (form != null)
            {
                request.SetForm(form);
                Log("<REST> CALLING SERVER Uri: " + request.Uri + "\nForm Data: " + form.ToString());
            }
            else
            {
                Log("<REST> CALLING SERVER Uri: " + request.Uri);
            }

            request.Send();
        }

        private RestResponse<T> ProcessResponse(HTTPRequest request, HTTPResponse response)
        {
            if (response == null)
            {
                throw request.Exception ?? new Exception("Unknown Exception");
            }

            if (!response.IsSuccess)
            {
                TError error = converter.ToObject<TError>(response.DataAsText);
                throw new RestException(error, response.Message);
            }

            string dataText = response.DataAsText;
            RestResponse<T> result;

            if (AllowEmptyResponse && IsEmptyResponse(dataText))
            {
                result = new RestResponse<T>(default, dataText, response.Headers);
            }
            else if (Convert(dataText, out var wrapper))
            {
                if (!wrapper.success)
                {
                    Debug.Log($"ProcessResponse fail:{response.StatusCode}, {response.DataAsText}, {request.Uri}");
                    if (wrapper.errorData != null)
                        throw new RestException(
                            new RestError((RestErrorType)wrapper.errorData.code, wrapper.errorData.message));
                    else
                        throw new RestException(
                            new RestError(RestErrorType.Unsupported, "No Error Data"));
                }

                result = new RestResponse<T>(wrapper.data, dataText, response.Headers);
            }
            else if (AllowInvalidResponse)
            {
                result = new RestResponse<T>(default, dataText, response.Headers);
            }
            else
            {
                throw new Exception("Unexpected");
            }

            return result;
        }

        private bool IsEmptyResponse(string dataText)
        {
            int s = 0;
            for (int index = 0; index < dataText.Length; ++index)
            {
                if (!char.IsWhiteSpace(dataText[index]))
                {
                    if ((s == 0 && dataText[index] == '{') || (s == 1 && dataText[index] == '}'))
                    {
                        s++;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return s != 1;
        }

        bool Convert(string dataText, out RestResponseWrapper<T> converted)
        {
            if (AllowInvalidResponse)
            {
                try
                {
                    converted = converter.ToObject<RestResponseWrapper<T>>(dataText);
                }
                catch
                {
                    converted = null;
                    return false;
                }
            }

            converted = converter.ToObject<RestResponseWrapper<T>>(dataText);

            return true;
        }

        void Log(string msg)
        {
            if (!Verbose)
                return;
            if(!showLog)
                return; 
            Debug.Log(msg);
        }

        public RestRequest<T, TError> ShowLog(bool value)
        {
            showLog = value;
            return this;
        }

        private void InvokeComplete(RestResponse<T> resp, TError error)
        {
            onComplete?.Invoke(resp, error);

            if (error == null)
            {
                onSuccess?.Invoke(resp);
            }

            if (error != null)
            {
                onFail?.Invoke(error);
            }
        }
    }
}