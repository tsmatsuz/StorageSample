using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.ServiceModel;
using Microsoft.IdentityModel.Protocols.WSTrust;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Text;

/**************************
 * MSO の STS 接続の構成は、Web.config に記載しています
 **************************/

namespace WordSampleWebRole
{
    public class SPOUtil
    {
        public static List<string> GetSPListCollection(HttpSessionStateBase session, string siteUrl)
        {
            // Please see http://msdn.microsoft.com/en-us/library/lists.lists.getlistcollection(v=office.12).aspx

            HttpWebRequest webRequest = HttpWebRequest.Create(VirtualPathUtility.AppendTrailingSlash(siteUrl) + @"_vti_bin/Lists.asmx") as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.CookieContainer = GetSPOCookieContainer(session);
            webRequest.Headers["SOAPAction"] = "http://schemas.microsoft.com/sharepoint/soap/GetListCollection";
            webRequest.UserAgent = Helper.UserAgentForSPO;

            string envelope = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                        <GetListCollection xmlns=""http://schemas.microsoft.com/sharepoint/soap/"" />
                    </soap:Body>
                </soap:Envelope>";
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] data = encoding.GetBytes(envelope);
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            XDocument resDoc;
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.OK)
                    resDoc = XDocument.Load(webResponse.GetResponseStream());
                else
                    throw new Exception("Failed to get rootfolder using SOAP Web Services (Status : " + webResponse.StatusCode + ")");
            }

            // リスト名一覧の抽出
            List<string> result = new List<string>();
            string ns_soap = @"http://schemas.xmlsoap.org/soap/envelope/";
            string ns_spsoap = @"http://schemas.microsoft.com/sharepoint/soap/";
            var ent = from x in resDoc.Element(XName.Get("Envelope", ns_soap)).Element(XName.Get("Body", ns_soap)).Element(XName.Get("GetListCollectionResponse", ns_spsoap)).Element(XName.Get("GetListCollectionResult", ns_spsoap)).Elements(XName.Get("Lists", ns_spsoap))
                      select x;
            foreach (var l in ent.Elements(XName.Get("List", ns_spsoap)))
            {
                if (l.Attribute(XName.Get("BaseType")).Value == "1")
                    result.Add(l.Attribute(XName.Get("Title")).Value);
            }

            return result;
        }

        public static string GetSPListRootFolder(HttpSessionStateBase session, string siteUrl, string listName)
        {
            // Please see http://msdn.microsoft.com/en-us/library/lists.lists.getlist(v=office.12).aspx

            HttpWebRequest webRequest = HttpWebRequest.Create(VirtualPathUtility.AppendTrailingSlash(siteUrl) + @"_vti_bin/Lists.asmx") as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.CookieContainer = GetSPOCookieContainer(session);
            webRequest.Headers["SOAPAction"] = "http://schemas.microsoft.com/sharepoint/soap/GetList";
            webRequest.UserAgent = Helper.UserAgentForSPO;

            string envelope = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                        <GetList xmlns=""http://schemas.microsoft.com/sharepoint/soap/"">
                            <listName>{0}</listName>
                        </GetList>
                    </soap:Body>
                </soap:Envelope>";
            UTF8Encoding encoding = new UTF8Encoding();
            envelope = string.Format(envelope, listName);
            byte[] data = encoding.GetBytes(envelope);
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            XDocument resDoc;
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.OK)
                    resDoc = XDocument.Load(webResponse.GetResponseStream());
                else
                    throw new Exception("Failed to get rootfolder using SOAP Web Services (Status : " + webResponse.StatusCode + ")");
            }

            // RootFolder の抽出
            string ns_soap = @"http://schemas.xmlsoap.org/soap/envelope/";
            string ns_spsoap = @"http://schemas.microsoft.com/sharepoint/soap/";
            var ent = from x in resDoc.Element(XName.Get("Envelope", ns_soap)).Element(XName.Get("Body", ns_soap)).Element(XName.Get("GetListResponse", ns_spsoap)).Element(XName.Get("GetListResult", ns_spsoap)).Elements(XName.Get("List", ns_spsoap))
                      select x;

            return ent.Attributes(XName.Get("RootFolder")).First().Value;
        }

        public static void UploadSPFile(HttpSessionStateBase session, Stream readStream, string absoluteUrl)
        {
            HttpWebRequest webRequest = HttpWebRequest.Create(absoluteUrl) as HttpWebRequest;
            webRequest.Method = "PUT";
            webRequest.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            // "application/octet-stream" ??
            webRequest.CookieContainer = GetSPOCookieContainer(session);
            webRequest.UserAgent = Helper.UserAgentForSPO;
            using (Stream webStream = webRequest.GetRequestStream())
            {
                int size = 4096, n;
                byte[] buf = new byte[size];
                while ((n = readStream.Read(buf, 0, size)) > 0)
                {
                    webStream.Write(buf, 0, n);
                }
                webStream.Close();
            }
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode != HttpStatusCode.Created)
                    throw new Exception("Failed to save document (Status : " + webResponse.StatusCode + ")");
            }
        }

        //
        // Process login and create a cookie container
        //
        public static void ProcessSPOSecurity(HttpSessionStateBase session, string siteUrl, string userid, string password)
        {
            if (GetSPOCookieContainer(session) == null)
            {
                string wreplyUrl = (new Uri(siteUrl)).GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx?wa=wsignin1.0";
                XDocument resTokenDoc = RequestTokenToMSO(wreplyUrl, userid, password);
                SPOSecurityCookie secCookie = RequestLoginToSPO(wreplyUrl, resTokenDoc);
                CookieContainer spoCookieContainer = new CookieContainer();
                Cookie fedAuthCookie = new Cookie("FedAuth", secCookie.FedAuth)
                {
                    Expires = secCookie.Expires,
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    Domain = secCookie.Host.Host
                };
                spoCookieContainer.Add(fedAuthCookie);
                Cookie rtFaCookie = new Cookie("rtFA", secCookie.rtFa)
                {
                    Expires = secCookie.Expires,
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    Domain = secCookie.Host.Host
                };
                spoCookieContainer.Add(rtFaCookie);
                SetSPOCookieContainer(session, spoCookieContainer);
            }
        }

        //
        // request to MSO STS (extSTS.srf)
        //
        public static XDocument RequestTokenToMSO(string formUrl, string userid, string password)
        {
            XDocument result = null;

            // (エラー発生の解放時に例外が変更されるため、using は使用しない . . .)
            ChannelFactory<IWSTrustFeb2005Contract> factory =
                new ChannelFactory<IWSTrustFeb2005Contract>("O365AuthClient");

            factory.Credentials.UserName.UserName = userid;
            factory.Credentials.UserName.Password = password;
            factory.Open();
            IWSTrustFeb2005Contract proxy = factory.CreateChannel();

            // create security token object for request
            RequestSecurityToken secToken = new RequestSecurityToken(WSTrustFeb2005Constants.RequestTypes.Issue);
            secToken.AppliesTo = new EndpointAddress(formUrl);
            secToken.KeyType = WSTrustFeb2005Constants.KeyTypes.Bearer;
            secToken.TokenType = Microsoft.IdentityModel.Tokens.SecurityTokenTypes.Saml11TokenProfile11;

            // write security token into memory
            MemoryStream stream = new MemoryStream();
            using (XmlWriter xWriter = XmlWriter.Create(stream))
            {
                WSTrustFeb2005RequestSerializer serializer = new WSTrustFeb2005RequestSerializer();
                serializer.WriteXml(secToken, xWriter, new WSTrustSerializationContext());
            }

            // read memory and create message
            stream.Position = 0;
            Message reqMsg = Message.CreateMessage(MessageVersion.Default,
                WSTrustFeb2005Constants.Actions.Issue,
                XmlReader.Create(stream));

            // send and receive message (get resTokenDoc)
            IAsyncResult asyncRes = proxy.BeginIssue(reqMsg, null, null);
            Message resMsg = proxy.EndIssue(asyncRes);
            using (XmlDictionaryReader xdReader = resMsg.GetReaderAtBodyContents())
            {
                result = XDocument.Parse(xdReader.ReadOuterXml());
            }

            factory.Close();

            return result;
        }

        //
        // request to O365 sharepoint online
        //
        public static SPOSecurityCookie RequestLoginToSPO(string formUrl, XDocument msoToken)
        {
            SPOSecurityCookie res = null;

            var crypt = from result in msoToken.Descendants()
                        where result.Name == XName.Get("BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")
                        select result;
            HttpWebRequest webRequest = HttpWebRequest.Create(formUrl) as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.CookieContainer = new CookieContainer();
            webRequest.AllowAutoRedirect = false;
            webRequest.UserAgent = Helper.UserAgentForSPO;
            byte[] data = Encoding.UTF8.GetBytes(crypt.FirstOrDefault().Value);
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                    res = RequestLoginToSPO(webResponse.Headers["Location"], msoToken);
                else
                {
                    res = new SPOSecurityCookie();
                    res.FedAuth = webResponse.Cookies["FedAuth"].Value;
                    res.rtFa = webResponse.Cookies["rtFa"].Value;
                    res.Host = webRequest.RequestUri;
                    res.Expires = Convert.ToDateTime((from result in msoToken.Descendants()
                                                      where result.Name == XName.Get("Expires", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")
                                                      select result).First().Value);
                }
            }

            return res;
        }

        public static void SetSPOCookieContainer(HttpSessionStateBase session,
            System.Net.CookieContainer item)
        {
            session["SPOCookieContainer"] = item;
        }

        public static System.Net.CookieContainer GetSPOCookieContainer(HttpSessionStateBase session)
        {
            return (System.Net.CookieContainer)session["SPOCookieContainer"];
        }

        public static void ClearSPOCookieContainer(HttpSessionStateBase session)
        {
            session["SPOCookieContainer"] = null;
        }
    }

    public class SPOSecurityCookie
    {
        public string FedAuth;
        public string rtFa;
        public DateTime Expires;
        public Uri Host;
    }
}
