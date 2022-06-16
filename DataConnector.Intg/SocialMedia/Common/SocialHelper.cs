using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Common
{
    public class SocialHelper : ISocialHelper
    {   
        private readonly ApplicationSettings _applicationSettings;
        private readonly ILog log;
        public SocialHelper(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                _applicationSettings = applicationSettings;
                this.log = log;
                log.Info("SocialHelper Constructor");
            }
            catch(Exception ex)
            {
                log.Error("SocialHelper Constructor: Exception Found: " + ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to get the date range list for full/delta run 
        /// </summary>
        /// <param name="lastRunDate">date of the last run</param>
        /// <param name="requestModel">requestModel</param>
        /// <param name="dayChunkSize">break the time period to a specific date range based on daychunk value</param>
        /// <param name="socialMediaType">like facebook, google, twitter</param>
        /// <returns>Return the date range list of a time period</returns>
        public List<YearsMonths> GetBusinessDatesList(string lastRunDate = null, RequestModel requestModel = null, int dayChunkSize = 7, string socialMediaType = null)
        {            
            try
            {
                log.Info("SocialHelper GetBusinessDatesList: Method started");
                string defaultDate = string.Empty;                
                log.Info("SocialHelper GetBusinessDatesList: DeltaDataCheck: " + requestModel.DeltaDataCheck + "FullDataCheck: " + requestModel.FullDataCheck);

                lastRunDate = string.IsNullOrEmpty(lastRunDate) ? (DateTime.UtcNow.Year - 1).ToString() + "-01-01" : lastRunDate;               
                log.Info("SocialHelper GetBusinessDatesList: lastRunDate: " + lastRunDate);

                bool.TryParse(requestModel.DeltaDataCheck, out bool deltaDataLoad);
                bool.TryParse(requestModel.FullDataCheck, out bool fullDataLoad);                               

                //Case 1 FullData Check true
                if (fullDataLoad)
                {
                    log.Info("SocialHelper GetBusinessDatesList: DeltaDataCheck is false so entering in FullDataCheck dateRange");
                    // Get the default date for fullload based on social media type
                    defaultDate = GetDafaultDateForSocialMediaType(socialMediaType, requestModel);
                    log.Info("SocialHelper GetBusinessDatesList: getting FullData as per DefaultDate which is: " + defaultDate);
                    var dateRange = SplitDateRange(Convert.ToDateTime(defaultDate), DateTime.Today, dayChunkSize, socialMediaType).ToList();
                    return ConvertDatetimeListToDateFormat(dateRange);
                }
                else if (deltaDataLoad)                  
                {
                    log.Info("SocialHelper GetBusinessDatesList: getting DeltaData for dateRange where lastRunDate: " + lastRunDate + "to current date and dayChunkSize: " + dayChunkSize.ToString());                    
                    var dateRange = SplitDateRange(Convert.ToDateTime(lastRunDate), DateTime.Today, dayChunkSize, socialMediaType).ToList();
                    log.Info("SocialHelper GetBusinessDatesList: Method End");
                    return ConvertDatetimeListToDateFormat(dateRange);
                }
            }
            catch (Exception ex)
            {
                log.Error("SocialHelper GetBusinessDatesList: Exception Found: " + ex.Message);
                throw;
            }            
            return new List<YearsMonths>();
        }

        /// <summary>
        /// Use this method to get  defaultDate
        /// </summary>
        /// <param name="socialMediaType">socialMediaType</param> 
        /// <param name="requestModel">requestModel</param> 
        /// <returns>defaultDate</returns>
        private string GetDafaultDateForSocialMediaType(string socialMediaType, RequestModel requestModel)
        {
            try
            {
                log.Info("SocialHelper GetDafaultDateForSocialMediaType: Method Start");
                string defaultDate = string.Empty;
                if (!string.IsNullOrEmpty(socialMediaType))
                {
                    if (socialMediaType == Constant.facebook)
                    {
                        defaultDate = requestModel.FullLoadForMonth ? DateTime.Today.AddMonths(-1).ToString() : _applicationSettings.FBDefaultDate;
                    }
                    else if (socialMediaType == Constant.google)
                    {
                        defaultDate = _applicationSettings.GADefaultDate;
                    }
                    else if (socialMediaType == Constant.twitter)
                    {
                        defaultDate = requestModel.FullLoadForMonth ? DateTime.Today.AddMonths(-1).ToString() : _applicationSettings.TWDefaultDate;
                    }
                    else if (socialMediaType == Constant.dv360)
                    {
                        defaultDate = _applicationSettings.DVDefaultDate;
                    }
                }
                else
                {
                    defaultDate = (DateTime.UtcNow.Year - 1).ToString() + "-01-01";
                }
                log.Info("SocialHelper GetDafaultDateForSocialMediaType: Method End");
                return defaultDate;
            }
            catch(Exception ex)
            {
                log.Error("SocialHelper GetDafaultDateForSocialMediaType: Exception Found: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use this method to convert start and end datetime to yyyy-MM-dd format without time 
        /// </summary>
        /// <param name="singleRange">date range list</param>        
        /// <returns>Return the date range list with date format in yyyy-MM-dd format</returns>
        private List<YearsMonths> ConvertDatetimeListToDateFormat(List<Tuple<DateTime, DateTime>> singleRange)
        {
            try
            {
                log.Info("SocialHelper ConvertDatetimeListToDateFormat: Method Start");
                List<YearsMonths> list = new List<YearsMonths>();
                foreach (var range in singleRange)
                {
                    YearsMonths yearMonth = new YearsMonths();
                    yearMonth.StartDate = range.Item1.ToString(("yyyy-MM-dd"));
                    yearMonth.EndDate = range.Item2.ToString(("yyyy-MM-dd"));
                    list.Add(yearMonth);
                }
                log.Info("SocialHelper ConvertDatetimeListToDateFormat: Method End");
                return list;
            }
            catch (Exception ex)
            {
                log.Error("SocialHelper ConvertDatetimeListToDateFormat: Exception:" + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use this method to split the time period as per dayChunkSize
        /// </summary>
        /// <param name="start">start date</param>
        /// <param name="end">end date</param>
        /// <param name="dayChunkSize">split with dayChunkSize</param>
        /// <param name="socialMediaType">like facebook, google, twitter</param>
        /// <returns>Return the date range list based on dayChunkSize</returns>
        private IEnumerable<Tuple<DateTime, DateTime>> SplitDateRange(DateTime start, DateTime end, int dayChunkSize, string socialMediaType = null)
        {            
            DateTime startOfThisPeriod = start;
            if (!string.IsNullOrEmpty(socialMediaType) && (socialMediaType == Constant.google || socialMediaType == Constant.dv360))
            {

                yield return Tuple.Create(startOfThisPeriod, end);
            }
            else
            {
                while (startOfThisPeriod < end)
                {
                    DateTime endOfThisPeriod = startOfThisPeriod.AddDays(dayChunkSize);
                    endOfThisPeriod = endOfThisPeriod < end ? endOfThisPeriod : end;
                    yield return Tuple.Create(startOfThisPeriod, endOfThisPeriod);
                    startOfThisPeriod = (socialMediaType == Constant.twitter) ? endOfThisPeriod : endOfThisPeriod.AddDays(1);
                }
            }                      
        }
    }
}
