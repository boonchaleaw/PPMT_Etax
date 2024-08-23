using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public static class ApiFileTransfer
    {
        public static string DownloadFile(string host, string path, string mode)
        {
            string key = "";
            using (System.Security.Cryptography.SHA512 shaM = new System.Security.Cryptography.SHA512Managed())
            {
                string keydata = "etax_transfer_file_" + DateTime.Now.ToString("MM-dd", new CultureInfo("en-US"));
                key = Convert.ToBase64String(shaM.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keydata)));
            }

            RestClient client = new RestClient(host + "/download_file");
            RestRequest request = new RestRequest();
            request.Method = Method.Post;

            var body = new
            {
                mode = mode,
                path = path,
                key = key,
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ResponseFileTransfer res = JsonConvert.DeserializeObject<ResponseFileTransfer>(response.Content);
                return res.data.filedata;
            }

            return "";
        }

        public static bool UploadFile(string host, string path, string file, string mode)
        {
            string key = "";
            using (System.Security.Cryptography.SHA512 shaM = new System.Security.Cryptography.SHA512Managed())
            {
                string keydata = "etax_transfer_file_" + DateTime.Now.ToString("MM-dd", new CultureInfo("en-US"));
                key = Convert.ToBase64String(shaM.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keydata)));
            }

            RestClient client = new RestClient(host + "/upload_file");
            RestRequest request = new RestRequest();
            request.Method = Method.Post;

            var body = new
            {
                mode = mode,
                path = path,
                file = file,
                key = key,
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ResponseFileTransfer res = JsonConvert.DeserializeObject<ResponseFileTransfer>(response.Content);
                return true;
            }

            return false;
        }
    }

    public class ResponseFileTransfer
    {
        public string message { get; set; }
        public ResponseFileTransferData data { get; set; }
    }

    public class ResponseFileTransferData
    {
        public string filedata { get; set; }
    }
}
