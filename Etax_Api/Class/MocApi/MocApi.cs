using Microsoft.Extensions.Options;
using RestSharp;
using System.Net;

namespace Etax_Api.Class.MocApi
{
    public static class MocApi
    {
        public static WalkinCertData getDataMoc(string tax_id)
        {
            RestClientOptions options = new RestClientOptions("https://dataapi.moc.go.th")
            {
                MaxTimeout = -1,
                CookieContainer = new System.Net.CookieContainer(),
            };
            RestClient client = new RestClient(options);
            RestRequest request = new RestRequest("/juristic?juristic_id=" + tax_id, Method.Get);
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                WalkinCertData responseMoc = Newtonsoft.Json.JsonConvert.DeserializeObject<WalkinCertData>(response.Content);
                if (responseMoc != null)
                {
                    return responseMoc;
                }

                return null;
            }

            return null;
        }
    }
}
