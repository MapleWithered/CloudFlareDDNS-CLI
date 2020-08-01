using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CloudFlareDDNS_CLI
{
    class DDNS
    {



        public class UserXAuth
        {
            public string X_Auth_Email { get; set; }
            public string X_Auth_Key { get; set; }
        }

        public class DNSRecord
        {
            public string id { get; set; }
            public string name { get; set; }
            public string content { get; set; }
            public int ttl { get; set; }
            public bool proxied { get; set; }
        }

        public class DDNSRequest
        {
            public UserXAuth user { get; set; }
            public string zone_id { get; set; }
            public DNSRecord record { get; set; }
        }







        #region 获取 公网IP
        public static string GetIP(string server_url)
        {
            string ip = new WebClient().DownloadString(server_url);         //Default: "http://ip.42.pl/raw"
            //TODO: Console Output
            return ip;
        }
        #endregion





        #region 获取site_id
        public static string getID(ref DDNSRequest request)
        {
            HttpClient client = new HttpClient();
            WebHeaderCollection headers = new WebHeaderCollection();

            string url = "https://api.cloudflare.com/client/v4/zones/" + request.zone_id + "/dns_records";

            headers.Add("X-Auth-Email: " + request.user.X_Auth_Email);
            headers.Add("X-Auth-Key: " + request.user.X_Auth_Key);
            headers.Add("Content-Type: application/json");

            string retdata = client.Get(url, headers);

            JObject json = JObject.Parse(retdata);
            JArray sites = (JArray)json["result"];

            string id = "empty";

            ConsoleLog.Log(sites.Count + " records found.");
            int temp_count = 0;
            foreach (JObject record in sites)
            {
                temp_count++;
                ConsoleLog.Log(temp_count.ToString().PadLeft(2, '0') + " : " + (string)record["name"]);
            }
            foreach (JObject record in sites)
            {
                if ((string)record["name"] == request.record.name)
                {
                    ConsoleLog.Log("Found DDNS record \"" + (string)record["name"] + "\"");
                    ConsoleLog.Log("DNS name in configuration is full name.");
                    id = (string)record["id"];
                }
                else if ((string)record["name"] == request.record.name + "." + (string)record["zone_name"])
                {
                    ConsoleLog.Log("Found DDNS record \"" + (string)record["name"] + "\"");
                    ConsoleLog.Note("DNS name in configuration is short. Recommend using \"" + (string)record["name"] + "\" instead.");
                    request.record.name = (string)record["name"];
                    id = (string)record["id"];
                }
            }
            if(id == "empty")
            {
                ConsoleLog.Error("Cannot find the specific DNS record.");
            }
            else if(id == "")
            {
                id = "empty";
            }
            else
            {
                ConsoleLog.Info("ID for " + request.record.name + " is : " + id);
            }

            return id;
        }
        #endregion










        #region DDNS汇报函数
        public static string Report(DDNSRequest request)
        {
            HttpClient client = new HttpClient();

            string url = "https://api.cloudflare.com/client/v4/zones/" + request.zone_id + "/dns_records/" + request.record.id;

            WebHeaderCollection headers = new WebHeaderCollection();

            headers.Add("X-Auth-Email: " + request.user.X_Auth_Email);
            headers.Add("X-Auth-Key: " + request.user.X_Auth_Key);
            headers.Add("Content-Type: application/json");

            string data =
                @"{""type"":""A""," +
                @"""name"":""" + request.record.name +
                @""",""content"":""" + request.record.content +
                @""",""ttl"":" + request.record.ttl +
                @",""proxied"":" + (request.record.proxied ? "true" : "false") +
                "}";

            string retdata = client.Put(url, headers, data);

            return retdata;
        }
        #endregion
    }
}
