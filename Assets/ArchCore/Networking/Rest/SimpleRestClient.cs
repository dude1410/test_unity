using BestHTTP;

namespace ArchCore.Networking.Rest
{
    public static class SimpleRestClient
    {
        public static RestBaseSimpleRequest Get(string url)
        {
            return new RestBaseSimpleRequest(url, HTTPMethods.Get);
        }

        public static RestBaseSimpleRequest Post(string url, byte[] body)
        {
            return new RestBaseSimpleRequest(url, HTTPMethods.Post).AddBody(body);
        }

        public static RestBaseSimpleRequest Post(string url)
        {
            return new RestBaseSimpleRequest(url, HTTPMethods.Post);
        }

        public static RestBaseSimpleRequest Put(string url, byte[] body)
        {
            return new RestBaseSimpleRequest(url, HTTPMethods.Put).AddBody(body);
        }

        public static RestBaseSimpleRequest Delete(string url)
        {
            return new RestBaseSimpleRequest(url, HTTPMethods.Delete);
        }
    }
}