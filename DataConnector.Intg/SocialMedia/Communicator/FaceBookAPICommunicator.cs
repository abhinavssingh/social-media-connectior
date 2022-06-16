using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class FaceBookApiCommunicator: IFacebookApiCommunicator
    {
        private readonly ILog log;
        public FaceBookApiCommunicator(ILog log)
        {
            this.log = log;            
        }
        /// <summary>
        /// Use this method to get the Facebook Platform Ads data
        /// </summary>
        /// <param name="adsDataURI">Request URL</param>
        /// <param name="fullDataCheck">bool value of fullDataCheck </param>        
        /// <returns>Return the list of FB ads data</returns>
        public RootObjectAds GetAdsDataListbyBreakDown(string adsDataURI, bool fullDataCheck, int delayTime)
        {            
            RootObjectAds adEntityData = null;
            try
            {
                log.Info("FaceBookAPICommunicator GetAdsDataListbyBreakDown: Method start");
                //get the response data for the request url
                var res = GetdataByURI(adsDataURI, fullDataCheck, delayTime);
                log.Info("FaceBookAPICommunicator GetAdsDataListbyBreakDown: got the response for URI and now Deserializer");
                //Deserializer response to RootObjectAds model
                adEntityData = Deserializer<RootObjectAds>(res);
                log.Info("FaceBookAPICommunicator GetAdsDataListbyBreakDown: Deserializer RootObjectAds Model Done");
            }
            catch(Exception ex)
            {
                log.Error("FaceBookAPICommunicator GetAdsDataListbyBreakDown: Exception Found " + ex.Message);
                throw;
            }            
            return adEntityData;
        }

        /// <summary>
        /// Use this method for http call 
        /// </summary>
        /// <param name="URI">Request URL</param>
        /// <param name="flgDelay">bool value of flgDelay </param>        
        /// <returns>Return the response for the request url</returns>
        public string GetdataByURI(string URI, bool flgDelay, int delayTime = 60000)
        {                        
            try
            {
                log.Info("FaceBookAPICommunicator GetdataByURI: Method start");
                using (var httpClient = new HttpClient())
                {
                    log.Info("FaceBookAPICommunicator GetdataByURI: created httpClient object");
                    httpClient.MaxResponseContentBufferSize = 2147483647;
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    int remainingTries = 2;
                    // making do while for connection issue
                    // now it will try to make call for 1 more times if call failed
                    do
                    {
                        --remainingTries;
                        try
                        {                            
                            if (flgDelay)
                            {
                                log.Info("FaceBookAPICommunicator GetdataByURI: Delay start for " + delayTime +" miliseconds");
                                Thread.Sleep(delayTime);
                            }
                            log.Info("FaceBookAPICommunicator GetdataByURI: make httpClient call");
                            // make the http call
                            return httpClient.GetStringAsync(URI).Result;
                        }
                        catch (Exception ex)
                        {                            
                            log.Error("HttpService GetdataByURI: httpClient.GetStringAsync() Exception: " + ex.Message);
                        }
                    }
                    while (remainingTries >= 0);
                }
            }
            catch (Exception ex)
            {
                log.Error("FaceBookAPICommunicator GetdataByURI: Exception Found "+ ex.Message);
                throw;
            }            
            return null;
        }

        /// <summary>
        /// Use this method for Deserialize the obj to required model
        /// </summary>
        /// <param name="response">response object</param>           
        /// <returns>Return the model after deserializer response for specific type</returns>
        private T Deserializer<T>(string response)
        {            
            try
            {
                log.Info("FaceBookAPICommunicator Deserializer: Method start");
                var desObj = JsonConvert.DeserializeObject<T>(response);
                log.Info("FaceBookAPICommunicator Deserializer: Method end");
                return desObj;
            }
            catch(Exception ex)
            {
                log.Error("FaceBookAPICommunicator Deserializer: Exception Found" + ex.Message);
                throw;
            }
        }
    }
}
