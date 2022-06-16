using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class SimilarWebApiCommunicator : ISimilarWebApiCommunicator
    {
        private readonly ILog log;
        public SimilarWebApiCommunicator(ILog log)
        {
            try
            {
                this.log = log;                
                log.Info("SimilarWebApiCommunicator Constructor");
            }
            catch(Exception ex)
            {
                log.Error("SimilarWebApiCommunicator Constructor :  Exception " + ex);
                throw;
            }
        }       

        /// <summary>
        /// Use this method to get the Similar Web desktop data
        /// </summary>
        /// <param name="adsDataURI">Request URL</param>
        /// <param name="delayTime">API call delay time</param>        
        /// <returns>Return the list of SW desktop data</returns>s
        public SWDesktopTrafficSourceEntity GetSWDesktopTrafficeData(string adsDataURI, int delayTime)
        {
            SWDesktopTrafficSourceEntity desktopEntityData = null;
            try
            {
                log.Info("SimilarWebApiCommunicator GetSWDesktopTrafficeData: Method start");
                //get the response data for the request url
                var res = GetdataByURI(adsDataURI, delayTime);
                if (res != null)
                {
                    log.Info("SimilarWebApiCommunicator GetSWDesktopTrafficeData: got the response for URI and now Deserializer");
                    //Deserializer response to SWDesktopTrafficSourceEntity model
                    var jarrayObj = JObject.Parse(res).SelectToken("visits.*");
                    JObject jObjOverview = new JObject(new JProperty("DesktopOverview", jarrayObj));
                    desktopEntityData = Deserializer<SWDesktopTrafficSourceEntity>(jObjOverview.ToString());
                    log.Info("SimilarWebApiCommunicator GetSWDesktopTrafficeData: Deserializer SWDesktopTrafficSourceEntity Model Done");
                }
                else
                {
                    log.Info("SimilarWebApiCommunicator GetSWDesktopTrafficeData: desktop data not found for the URI");
                }
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebApiCommunicator GetSWDesktopTrafficeData: Exception Found " + ex.Message);
                throw;
            }
            return desktopEntityData;
        }

        /// <summary>
        /// Use this method to get the Similar Web mobile data
        /// </summary>
        /// <param name="adsDataURI">Request URL</param>
        /// <param name="delayTime">API call delay time</param>        
        /// <returns>Return the list of SW mobile data</returns>s
        public SWMobileTrafficSourceEntity GetSWMobileTrafficeData(string adsDataURI, int delayTime)
        {
            SWMobileTrafficSourceEntity mobileEntityData = null;
            try
            {
                log.Info("SimilarWebApiCommunicator GetSWTrafficeDataList: Method start");
                //get the response data for the request url
                var res = GetdataByURI(adsDataURI, delayTime);
                if (res != null)
                {
                    log.Info("SimilarWebApiCommunicator GetSWTrafficeDataList: got the response for URI and now Deserializer");
                    //Deserializer response to SWMobileTrafficSourceEntity model
                    var jarrayObj = JObject.Parse(res).SelectToken("visits.*");
                    JObject jObjOverview = new JObject(new JProperty("MobileOverview", jarrayObj));
                    mobileEntityData = Deserializer<SWMobileTrafficSourceEntity>(jObjOverview.ToString());
                    log.Info("SimilarWebApiCommunicator GetSWTrafficeDataList: Deserializer SWMobileTrafficSourceEntity Model Done");
                }
                else
                {
                    log.Info("SimilarWebApiCommunicator GetSWTrafficeDataList: mobile data not found for this URI");
                }
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebApiCommunicator GetSWTrafficeDataList: Exception Found " + ex.Message);
                throw;
            }
            return mobileEntityData;
        }

        /// <summary>
        /// Use this method for http call 
        /// </summary>
        /// <param name="URI">Request URL</param>
        /// <param name="flgDelay">bool value of flgDelay </param>        
        /// <returns>Return the response for the request url</returns>
        public string GetdataByURI(string URI, int delayTime)
        {
            try
            {
                log.Info("SimilarWebApiCommunicator GetdataByURI: Method start");
                using (var httpClient = new HttpClient())
                {
                    log.Info("SimilarWebApiCommunicator GetdataByURI: created httpClient object");
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
                            if (delayTime > 0)
                            {
                                log.Info("SimilarWebApiCommunicator GetdataByURI: Delay start for " + delayTime + " miliseconds");
                                Thread.Sleep(delayTime);
                            }
                            log.Info("SimilarWebApiCommunicator GetdataByURI: make httpClient call");
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
                log.Error("SimilarWebApiCommunicator GetdataByURI: Exception Found " + ex.Message);
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
                log.Info("SimilarWebApiCommunicator Deserializer: Method start");
                var desObj = JsonConvert.DeserializeObject<T>(response);
                log.Info("SimilarWebApiCommunicator Deserializer: Method end");
                return desObj;
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebApiCommunicator Deserializer: Exception Found" + ex.Message);
                throw;
            }
        }
    }
}
