using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace CloudFlareDDNS_CLI
{
    class Program
    {
        public static string GetIP(string server_url)
        {
            string ip = new WebClient().DownloadString(server_url);         //Default: "http://ip.42.pl/raw"
            //TODO: Console Output
            return ip;
        }


        static void Main(string[] args)
        {


            ConsoleLog.Log("Program started.");

            #region 打开配置文件
            XmlDocument config_xml = new XmlDocument();
            try
            {
                config_xml.Load("config.xml");
            }
            catch (FileNotFoundException e)
            {
                ConsoleLog.Error("Config file not found! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            catch (XmlException e)
            {
                ConsoleLog.Error("Config file data invalid.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            XmlElement config_root = config_xml.DocumentElement;
            ConsoleLog.Succeed("Configuration File opened.");
            #endregion

            #region 提取配置信息
            ConsoleLog.Log("Reading configurations.");
            DDNS.UserXAuth user;
            DDNS.DNSRecord record;
            DDNS.DDNSRequest request;
            List<String> api_list = new List<String>();
            int api_count = 0;
            try
            {
                user = new DDNS.UserXAuth
                {
                    X_Auth_Email = config_root.SelectSingleNode("/UserConfigurationData/xauth_email").InnerText,
                    X_Auth_Key = config_root.SelectSingleNode("/UserConfigurationData/xauth_key").InnerText,
                };
                ConsoleLog.Info("User | XAuthEmail | " + user.X_Auth_Email);
                ConsoleLog.Info("User | XAuthKey   | " + user.X_Auth_Key);
                record = new DDNS.DNSRecord
                {
                    id = "empty",
                    name = config_root.SelectSingleNode("/UserConfigurationData/record_name").InnerText,
                    content = "",
                    ttl = int.Parse(config_root.SelectSingleNode("/UserConfigurationData/record_ttl").InnerText),
                    proxied = bool.Parse(config_root.SelectSingleNode("/UserConfigurationData/record_proxied").InnerText)
                };
                ConsoleLog.Info("DNS  | Name    | " + record.name);
                ConsoleLog.Info("DNS  | TTL     | " + record.ttl);
                ConsoleLog.Info("DNS  | Proxied | " + record.proxied);
                request = new DDNS.DDNSRequest
                {
                    user = user,
                    record = record,
                    zone_id = config_root.SelectSingleNode("/UserConfigurationData/zone_id").InnerText
                };
                ConsoleLog.Info("Site | zone_id | " + request.zone_id);
            }
            catch (NullReferenceException e)
            {
                ConsoleLog.Error("Config file missing some information! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            catch (FormatException e)
            {
                ConsoleLog.Error("Config file's format is wrong! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            ConsoleLog.Succeed("Successfully read configuration.");
            #endregion

            #region 打开API配置文件
            XmlDocument api_xml = new XmlDocument();
            try
            {
                api_xml.Load("config_api.xml");
            }
            catch (FileNotFoundException e)
            {
                ConsoleLog.Error("Config file not found! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            catch (XmlException e)
            {
                ConsoleLog.Error("Config file data invalid.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            XmlElement api_root = api_xml.DocumentElement;
            ConsoleLog.Succeed("API Configuration File opened.");
            #region API列表读取
            try
            {
                XmlNodeList api_node_list = api_root.SelectNodes("/config/ipapis/api");
                foreach (XmlNode api in api_node_list)
                {
                    ConsoleLog.Info("Here");
                    api_count ++;
                    api_list.Add(api.InnerText);
                    ConsoleLog.Info("Public IP API (" + api_count.ToString().PadLeft(2, '0') + ") : " + api.InnerText);
                }
            }
            catch (NullReferenceException e)
            {
                ConsoleLog.Error("Config file missing some information! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            catch (FormatException e)
            {
                ConsoleLog.Error("Config file's format is wrong! Details below.");
                ConsoleLog.Info(e.Message);
                ConsoleLog.Log("Exit.");
                return;
            }
            #endregion

            #endregion

            #region 获取CloudFlare域名ID
            int trytimer = 10;
            while (request.record.id == "empty")
            {
                ConsoleLog.Log("Trying to get id for zone \"" + request.record.name + "\"...");
                try
                {
                    string id = DDNS.getID(ref request);
                    if (id == "empty" || id == "")
                    {
                        ConsoleLog.Error("Failed getting DNS record's ID. Try again after " + trytimer + " seconds.");
                        Thread.Sleep(trytimer * 1000);
                        trytimer += 10;
                        continue;
                    }
                    else
                    {
                        request.record.id = id;
                        ConsoleLog.Succeed("Successfully got DNS record's ID.");
                        break;
                    }
                }
                catch (WebException e)
                {
                    ConsoleLog.Error(e.Message + " Try again after " + trytimer + " seconds.");
                    Thread.Sleep(trytimer * 1000);
                    trytimer += 10;
                }
            }
            #endregion




            #region 获取公网IP
            string ip_prev = "";
            string ip_now = "";
            int temp_count = 0;
            trytimer = 10;
            while (ip_prev == "")
            {
                foreach (string api in api_list)
                {
                    temp_count++;
                    ConsoleLog.Log("Getting IP @ API." + temp_count.ToString().PadLeft(2, '0') + " : " + api);
                    try
                    {
                        ip_prev = new WebClient().DownloadString(api).Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
                    }
                    catch (WebException e)
                    {
                        ConsoleLog.Error(e.Message);
                        continue;
                    }
                    ConsoleLog.Info("Init IP: " + ip_prev);
                    request.record.content = ip_prev;
                    break;
                }
                if (ip_prev == "")
                {
                    ConsoleLog.Error("Failed getting public IP. Try again after " + trytimer + " seconds.");
                    Thread.Sleep(trytimer * 1000);
                    trytimer += 10;
                }
            }
            #endregion


            // 强制单次汇报
            ConsoleLog.Log("Report DDNS once for initialization.");
            trytimer = 10;
            while (true)
            {
                string retdata = "";
                try
                {
                    retdata = DDNS.Report(request);
                    JObject json = JObject.Parse(retdata);
                    if ((bool)json["success"])
                    {
                        ConsoleLog.Log(retdata);
                        break;
                    }
                    else
                    {
                        ConsoleLog.Error("Failed report DNS record. Details below. Try again after " + trytimer + " seconds.");
                        ConsoleLog.Info(retdata);
                        Thread.Sleep(trytimer * 1000);
                        trytimer += 10;
                        continue;
                    }
                }
                catch (WebException e)
                {
                    ConsoleLog.Error(e.Message+" Try again after " + trytimer + " seconds.");
                    Thread.Sleep(trytimer * 1000);
                    trytimer += 10;
                    continue;
                }
            }
            ConsoleLog.Succeed("Succeessfully reported.");

            ConsoleLog.Log("DDNS Service started. IP Monitor running.");
            while (true)
            {
                Thread.Sleep(60000);

                temp_count = 0;
                foreach (string api in api_list)
                {
                    temp_count++;
                    ConsoleLog.Log("Getting IP @ API." + temp_count.ToString().PadLeft(2, '0') + " : " + api);
                    try
                    {
                        ip_now = new WebClient().DownloadString(api).Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
                    }
                    catch (WebException e)
                    {
                        ConsoleLog.Error(e.Message);
                        continue;
                    }
                    ConsoleLog.Info("Now IP: " + ip_now);
                    break;
                }
                if (ip_now != ip_prev)
                {
                    ConsoleLog.Note("IP changed to " + ip_now + ". Will update DNS record.");
                    request.record.content = ip_now;

                    //Update Record
                    ConsoleLog.Log("Report DDNS once for initialization.");
                    trytimer = 10;
                    while (true)
                    {
                        string retdata = "";
                        try
                        {
                            retdata = DDNS.Report(request);
                            JObject json = JObject.Parse(retdata);
                            if ((bool)json["success"])
                            {
                                ConsoleLog.Log(retdata);
                                break;
                            }
                            else
                            {
                                ConsoleLog.Error("Failed report DNS record. Details below. Try again after " + trytimer + " seconds.");
                                ConsoleLog.Info(retdata);
                                Thread.Sleep(trytimer * 1000);
                                trytimer += 10;
                                continue;
                            }
                        }
                        catch (WebException e)
                        {
                            ConsoleLog.Error(e.Message + " Try again after " + trytimer + " seconds.");
                            Thread.Sleep(trytimer * 1000);
                            trytimer += 10;
                            continue;
                        }
                    }
                    ConsoleLog.Succeed("Succeessfully reported.");
                    ip_prev = ip_now;
                }
            }
        }
    }
}
