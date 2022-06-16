using Autofac;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging;
using DataConnector.Intg.SocialMedia.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class TwitterApiCommunicator : ITwitterApiCommunicator
    {
        private readonly ILog log;
        public const string OauthVersion = Constant.OauthAPIVersion;
        public const string OauthSignatureMethod = Constant.OauthSignatureMethodType;        
        readonly HMACSHA1 sigHasher;
        readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);        
        readonly string consumerKey;        
        readonly string accessToken;        
        public TwitterApiCommunicator(ILog log)
        {
            try
            {
                this.log = log;
                log.Info($"TwitterAPICommunicator Constructor");
                KeyVaultService _keyService = Dependency.Container.Resolve<KeyVaultService>();
                consumerKey = _keyService.GetSecretValue(Constant.TWConsumerKey).GetAwaiter().GetResult();
                string consumerKeySecret =  _keyService.GetSecretValue(Constant.TWConsumerKeySecret).GetAwaiter().GetResult();
                accessToken = _keyService.GetSecretValue(Constant.TWAccessToken).GetAwaiter().GetResult();
                string accessTokenSecret = _keyService.GetSecretValue(Constant.TWAccessTokenSecret).GetAwaiter().GetResult();
                sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", consumerKeySecret, accessTokenSecret)));
            }
            catch(Exception ex)
            {
                log.Error($"TwitterAPICommunicator Constructor: Exception " +ex);
                throw;
            }
        }
        /// <summary>
        /// Use this method to get the response data of twiiter
        /// </summary>
        /// <param name="resourceUrl">request url</param>
        /// <param name="method">method get/post</param>
        /// <param name="cursor">for pagination data</param>            
        /// <returns>Return the response of twitter ads data</returns>
        public WebResponse GetResponse(string resourceUrl, Method method, string cursor=null)
        {   
            try
            {
                log.Info($"TwitterAPICommunicator GetResponse: Method start");
                log.Info($"TwitterAPICommunicator GetResponse: seprating main url and query paramters from uri");
                Dictionary<string, string> dicQueryString = new Dictionary<string, string>();
                string path = resourceUrl.IndexOf("?") > -1 ? resourceUrl.Substring(0, resourceUrl.IndexOf("?")): resourceUrl;
                string query = resourceUrl.IndexOf("?") > -1 ? resourceUrl.Split(new[] { '?' })[1] : "";
                if (!string.IsNullOrEmpty(query))
                {
                    log.Info($"TwitterAPICommunicator GetResponse: adding query parameters to Dictionary");
                    dicQueryString =
                            query.Split('&')
                                 .ToDictionary(c => c.Split('=')[0],
                                               c => Uri.UnescapeDataString(c.Split('=')[1]));
                }
                ServicePointManager.Expect100Continue = false;
                WebRequest request = null;

                if (method == Method.GET)
                {
                    log.Info($"TwitterAPICommunicator GetResponse: WebRequest client is initialized");
                    request = (HttpWebRequest)WebRequest.Create(resourceUrl);
                    request.Method = method.ToString();
                }

                if (request != null)
                {
                    log.Info($"TwitterAPICommunicator GetResponse: adding OAuth and query parameters to SortedDictionary");
                    var requestParameters = new SortedDictionary<string, string>();
                    // Timestamps are in seconds since 1/1/1970.
                    var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

                    // Add all the OAuth headers we'll need to use when constructing the hash.                    
                    requestParameters.Add(Constant.oauthConsumerKey, consumerKey);
                    // Generate the OAuth Nonce and add it to our payload.                    
                    requestParameters.Add(Constant.oauthNonce, CreateOauthNonce());
                    requestParameters.Add(Constant.oauthSignatureMethod, OauthSignatureMethod);
                    requestParameters.Add(Constant.oauthTimestamp, timestamp.ToString());
                    requestParameters.Add(Constant.oauthToken, accessToken);
                    requestParameters.Add(Constant.oauthVersion, OauthVersion);
                    foreach (var item in dicQueryString)
                    {                        
                        requestParameters.Add(item.Key, item.Value);
                    }                    
                    
                    // Generate the OAuth signature and add it to our payload.
                    requestParameters.Add(Constant.oauthSignature, CreateOauthSignature(path, method, requestParameters));

                    log.Info($"TwitterAPICommunicator GetResponse: create OAuth Header");
                    // Build the OAuth HTTP Header from the data.
                    string oAuthHeader = CreateHeader(requestParameters);
                    
                    request.Headers.Add("Authorization", oAuthHeader);
                    request.Timeout = 300000;
                    Thread.Sleep(8000);
                    log.Info($"TwitterAPICommunicator GetResponse: Get Response for the request");
                    return request.GetResponse();                    
                }
            }
            catch(Exception ex)
            {
                log.Error($"TwitterAPICommunicator GetResponse: Exception found "+ ex);
                throw;
            }
            return null;
        }
        /// <summary>
        /// Use this method to create OauthNonce
        /// </summary>
        /// <returns>Return the OauthNonce value</returns>
        private string CreateOauthNonce()
        {   
            string randomvalue;
            try
            {
                log.Info($"TwitterAPICommunicator CreateOauthNonce: Method start");

                using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
                {
                    byte[] val = new byte[8];
                    crypto.GetBytes(val);
                    randomvalue = Convert.ToString(BitConverter.ToInt32(val, 1));
                }
            }
            catch(Exception ex)
            {
                log.Error($"TwitterAPICommunicator CreateOauthNonce: Exception Found "+ ex);
                throw;
            }
            return randomvalue;
        }

        /// <summary>
        /// Use this method to create Request Header
        /// </summary>
        /// <param name="requestParameters">OAuth Parameters</param>
        /// <returns>Return the Request Header value</returns>
        private string CreateHeader(SortedDictionary<string, string> requestParameters)
        {
            try
            {
                log.Info($"TwitterAPICommunicator CreateHeader");
                return "OAuth " + string.Join(
                ", ",
                requestParameters
                    .Where(kvp => kvp.Key.StartsWith("oauth_"))
                    .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
                );
            }
            catch(Exception ex)
            {
                log.Error($"TwitterAPICommunicator CreateHeader: Exception "+ ex);
                throw;
            }
        }
        /// <summary>
        /// Use this method to create Oauth Signature
        /// </summary>
        /// <param name="resourceUrl">Request URL</param>
        /// <param name="method">method get/post</param>
        /// <param name="requestParameters">all the parameters of request(query parameters with OAuth Parameters)</param>
        /// <returns>Return the Oauth Signature value</returns>
        private string CreateOauthSignature
        (string resourceUrl, Method method, SortedDictionary<string, string> requestParameters)
        {
            try
            {
                log.Info($"TwitterAPICommunicator CreateOauthSignature : Method start");
                SortedDictionary<string, string> requestParametersCopy = requestParameters;
                // joining all the parameters with &
                var sigString = string.Join("&",
                requestParameters
                    .Union(requestParametersCopy)
                    .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                );

                // joining method , request url, and sign parameters string 
                var fullSigData = string.Format("{0}&{1}&{2}", method.ToString(), Uri.EscapeDataString(resourceUrl), Uri.EscapeDataString(sigString.ToString()));

                log.Info($"TwitterAPICommunicator CreateOauthSignature : Method end");
                // Convert the fullSigData into Base64 string 
                return Convert.ToBase64String(sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData.ToString())));
            }
            catch(Exception ex)
            {
                log.Error($"TwitterAPICommunicator CreateOauthSignature : Exception "+ ex);
                throw;
            }
        }
    }

    public enum Method
    {
        POST,
        GET
    }
}
