using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;
using System.Text;

namespace WordSampleWebRole
{
    // see http://blogs.msdn.com/b/tsmatsuz/archive/2011/12/19/windows-live-skydrive-oauth-2-0-rest-api-web-api-development-sample.aspx

    public class SkyDriveUtil
    {
        public const string CLIENT_ID = "<your app client id>";
        public const string CLIENT_SECRET = "<your app client secret>";
        // http://<your published site>/Document/SaveToSkydrive
        public const string REDIRECT_URL = "<Please input your site url>/Document/SaveToSkydrive";

        public static void RequestAccessTokenByVerifier(string verifier, out OAuthToken token)
        {
            string content = String.Format("client_id={0}&redirect_uri={1}&client_secret={2}&code={3}&grant_type=authorization_code",
                HttpUtility.UrlEncode(CLIENT_ID),
                HttpUtility.UrlEncode(REDIRECT_URL),
                HttpUtility.UrlEncode(CLIENT_SECRET),
                HttpUtility.UrlEncode(verifier));
            RequestAccessToken(content, out token);
        }

        // This method is for refreshing access token. (see http://blogs.msdn.com/b/tsmatsuz/archive/2011/12/19/windows-live-skydrive-oauth-2-0-rest-api-web-api-development-sample.aspx)
        // Please catch WebException (StatusCode: 401, System.Net.HttpStatusCode.Unauthorized), and refresh access token !
        //public static void RequestAccessTokenByRefreshToken(string refreshToken, out OAuthToken token)
        //{
        //    string content = String.Format("client_id={0}&redirect_uri={1}&client_secret={2}&refresh_token={3}&grant_type=refresh_token",
        //        HttpUtility.UrlEncode(CLIENT_ID),
        //        HttpUtility.UrlEncode(REDIRECT_URL),
        //        HttpUtility.UrlEncode(CLIENT_SECRET),
        //        HttpUtility.UrlEncode(refreshToken));
        //    RequestAccessToken(content, out token);
        //}

        public static void RequestAccessToken(string postContent, out OAuthToken token)
        {
            token = null;

            HttpWebRequest webRequest = WebRequest.Create(@"https://login.live.com/oauth20_token.srf") as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            using (StreamWriter writer = new StreamWriter(webRequest.GetRequestStream()))
            {
                writer.Write(postContent);
            }
            using (HttpWebResponse response = webRequest.GetResponse() as HttpWebResponse)
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OAuthToken));
                token = serializer.ReadObject(response.GetResponseStream()) as OAuthToken;
            }
        }

        public static List<SelectListItem> GetFolders(OAuthToken token)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            FolderEntryCollection folders;

            HttpWebRequest webRequest = HttpWebRequest.Create(@"https://apis.live.net/v5.0/me/skydrive/files?access_token=" + HttpUtility.UrlEncode(token.AccessToken)) as HttpWebRequest;
            webRequest.Method = "GET";
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FolderEntryCollection));
                    folders = serializer.ReadObject(webResponse.GetResponseStream()) as FolderEntryCollection;
                }
                else
                    throw new Exception("Failed to get skydrive folders using REST API (Status : " + webResponse.StatusCode + ")");
            }

            foreach (var item in folders.Data)
            {
                result.Add(new SelectListItem()
                {
                    Value = item.UploadLocation,
                    Text = item.Name
                });
            }

            return result;
        }

        public static void UploadFile(string fileName, Stream readStream, string folderLocation, string accessToken)
        {
            HttpWebRequest webRequest = HttpWebRequest.Create(VirtualPathUtility.RemoveTrailingSlash(folderLocation) + "?access_token=" + HttpUtility.UrlEncode(accessToken)) as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "multipart/form-data; boundary=A300tsmatsuzdemox";
            using (Stream webStream = webRequest.GetRequestStream())
            {
                // 注 : 今回、エンコード (Base64 等) はしない !
                string startEnvelope = @"--A300tsmatsuzdemox
Content-Disposition: form-data; name=""file""; filename=""{0}""
Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document

";
                startEnvelope = string.Format(startEnvelope, fileName);
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] startData = encoding.GetBytes(startEnvelope);
                webStream.Write(startData, 0, startData.Length);

                int size = 4096, n;
                byte[] buf = new byte[size];
                while ((n = readStream.Read(buf, 0, size)) > 0)
                {
                    webStream.Write(buf, 0, n);
                }

                string endEnvelope = @"

--A300tsmatsuzdemox--";
                byte[] endData = encoding.GetBytes(endEnvelope);
                webStream.Write(endData, 0, endData.Length);

                webStream.Close();
            }
            using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
            {
                if (webResponse.StatusCode != HttpStatusCode.Created)
                    throw new Exception("Failed to save document (Status : " + webResponse.StatusCode + ")");
            }
        }
    }

    public static class OAuthConstants
    {
        #region OAuth 2.0 standard parameters
        public const string ClientID = "client_id";
        public const string ClientSecret = "client_secret";
        public const string Callback = "redirect_uri";
        public const string ClientState = "state";
        public const string Scope = "scope";
        public const string Code = "code";
        public const string AccessToken = "access_token";
        public const string ExpiresIn = "expires_in";
        public const string RefreshToken = "refresh_token";
        public const string ResponseType = "response_type";
        public const string GrantType = "grant_type";
        public const string Error = "error";
        public const string ErrorDescription = "error_description";
        public const string Display = "display";
        #endregion
    }

    [DataContract]
    public class OAuthToken
    {
        [DataMember(Name = OAuthConstants.AccessToken)]
        public string AccessToken { get; set; }
        [DataMember(Name = OAuthConstants.RefreshToken)]
        public string RefreshToken { get; set; }
        [DataMember(Name = OAuthConstants.ExpiresIn)]
        public string ExpiresIn { get; set; }
        [DataMember(Name = OAuthConstants.Scope)]
        public string Scope { get; set; }
    }

    /*** json format of folder
      {
         "id": "folder.9c0af81b735e29ea.9C0AF81B735E29EA!113", 
         "from": {
            "name": "剛 松崎", 
            "id": "9c0af81b735e29ea"
         }, 
         "name": "写真", 
         "description": null, 
         "parent_id": "folder.9c0af81b735e29ea", 
         "upload_location": "https://apis.live.net/v5.0/folder.9c0af81b735e29ea.9C0AF81B735E29EA!113/files/", 
         "count": 0, 
         "link": "https://skydrive.live.com/redir.aspx?cid\u003d9c0af81b735e29ea\u0026page\u003dview\u0026resid\u003d9C0AF81B735E29EA!113\u0026parid\u003d9C0AF81B735E29EA!112", 
         "type": "folder", 
         "shared_with": {
            "access": "Just me"
         }, 
         "created_time": "2008-11-04T05:39:24+0000", 
         "updated_time": "2008-11-04T05:39:24+0000"
      }
     */
    [DataContract]
    public class FolderEntry
    {
        [DataMember(Name = "id")]
        public string Id;
        [DataMember(Name = "name")]
        public string Name;
        [DataMember(Name = "upload_location")]
        public string UploadLocation;
    }

    [DataContract]
    public class FolderEntryCollection
    {
        [DataMember(Name = "data")]
        public FolderEntry[] Data;
    }
}
