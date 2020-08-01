using System.Text;
using System.Net;
using System.IO;

// Source : https://www.cnblogs.com/lastcode/p/4878436.html

namespace CloudFlareDDNS_CLI
{
    class HttpClient
    {
        public string Delete(string url, WebHeaderCollection headers, string data) => CommonHttpRequest(url, headers, data, "DELETE");

        public string Delete(string url, WebHeaderCollection headers)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "DELETE";
            request.Headers = headers;

            HttpWebResponse myResponse = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);

            string xml_return = reader.ReadToEnd();

            reader.Close();
            myResponse.Close();

            return xml_return;
        }

        public string Put(string url, WebHeaderCollection headers, string data) => CommonHttpRequest(url, headers, data, "PUT");

        public string Post(string url, WebHeaderCollection headers, string data) => CommonHttpRequest(url, headers, data, "POST");

        public string Get(string url, WebHeaderCollection headers)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Headers = headers;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);

            string xml_return = reader.ReadToEnd();

            reader.Close();
            response.Close();

            return xml_return;
        }

        // HTTPRequest 基本请求函数
        private string CommonHttpRequest(string url, WebHeaderCollection headers, string data, string method)
        {

            byte[] data_buf = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method;
            request.Headers = headers;
            request.ContentLength = data_buf.Length;
            request.ContentType = "application/json";
            request.MaximumAutomaticRedirections = 1;
            request.AllowAutoRedirect = true;

            //网络流发送
            Stream stream = request.GetRequestStream();
            stream.Write(data_buf, 0, data_buf.Length);
            stream.Close();

            //获得回复数据
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string xml_return = reader.ReadToEnd();
            reader.Close();

            response.Close();

            return xml_return;
        }

    }
}
