using DataConnector.Intg.Interfaces.ICommon;
using ExcelDataReader;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace DataConnector.Intg.SocialMedia.Common
{
    public class FileConvertor : IFileConvertor
    {
        private readonly ILog log;
        public FileConvertor(ILog log)
        {
            this.log = log;            
        }

        /// <summary>
        ///  Method is used to write the datatable data in existing/new excel or csv files
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="fileExtension"></param>
        /// <param name="existingFilePath"></param> 
        /// <param name="flgUseExistingData"></param> 
        /// <returns>retun the excel/csv file</returns>
        public string WriteDataTableToFile(DataTable dataTable, string fileExtension = ".CSV", string existingFilePath = null, bool flgUseExistingData = true)
        {            
            bool fileExists = false;
            StreamWriter streamWriter = null;
            StreamReader streamReader = null;
            string tempFilePath = string.Empty;

            try
            {
                log.Info("FileConvertor WriteDataTableToFile: Entered in method");
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    log.Info("FileConvertor WriteDataTableToFile: Datatable has Rows, processing..");
                    
                    if (existingFilePath!= null && File.Exists(existingFilePath))
                    {
                        streamReader = new StreamReader(existingFilePath);
                        if (streamReader != null && streamReader.ReadToEnd().Length > 0)
                        {
                            fileExists = true;
                            tempFilePath = existingFilePath;
                        }
                        if (streamReader != null)
                        {
                            streamReader.Dispose();
                        }
                    }
                    else
                    {
                        var tempPath = Path.GetTempPath(); // Get %TEMP% path
                        var tempFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()); // Get random file name without extension
                        tempFilePath = Path.Combine(tempPath, tempFileName + fileExtension); // Get random file path
                        log.Info("FileConvertor WriteDataTableToFile: tempFilePath will create at path: " + tempFilePath);
                    }

                    StringBuilder stringBuilder = new StringBuilder();
                    streamWriter = new StreamWriter(tempFilePath, flgUseExistingData);
                    if (!fileExists)
                    {
                        foreach (DataColumn dataColumn in dataTable.Columns)
                        {
                            stringBuilder.Append(string.Concat(dataColumn.ColumnName, ","));
                        }
                        streamWriter.Write(stringBuilder.ToString().TrimEnd(",".ToCharArray()));
                        log.Info("FileConvertor WriteDataTableToFile: Datatable Columns written on File from Path " + tempFilePath);
                    }
                    else
                    {
                        log.Info("FileConvertor WriteDataTableToFile: File already exists, so no need to create columns");
                    }

                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        streamWriter.WriteLine();
                        for (int startIndex = 0; startIndex < dataTable.Columns.Count; startIndex++)
                        {
                            string strValue = ((dataRow[startIndex].ToString().Contains(",") || dataRow[startIndex].ToString().Contains(Environment.NewLine)
                                                || dataRow[startIndex].ToString().Contains("\n")) ? string.Format("\"{0}\"", dataRow[startIndex].ToString())
                                                : dataRow[startIndex].ToString());
                            if (startIndex == 0)
                            {
                                streamWriter.Write(ReplaceContent(strValue));
                            }
                            else
                            {
                                streamWriter.Write(string.Concat(",", ReplaceContent(strValue)));
                            }
                        }
                    }
                    log.Info("FileConvertor WriteDataTableToFile: Datatable Rows written on File from Path " + tempFilePath);
                }
                log.Info("FileConvertor WriteDataTableToFile: Method end");
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor WriteDataTableToFile: Exception occured " + ex.Message + ex.StackTrace);
                throw;
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Dispose();
                }
                if (streamWriter != null)
                {
                    streamWriter.Dispose();
                }
            }            
            return tempFilePath;
        }

        /// <summary>
        ///  Method is used to convert Model to DataTable
        /// </summary>
        /// <param name="List<T> items"></param>        
        /// <returns>retun DataTable</returns>
        public DataTable ToDataTable<T>(List<T> items)
        {            
            DataTable dataTable = new DataTable(typeof(T).Name);
            try
            {
                log.Info("FileConvertor ToDataTable: Method Start");
                log.Info("FileConvertor ToDataTable: Get all the properties");
                //Get all the properties
                PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in Props)
                {
                    //Defining type of data column gives proper data table 
                    var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                    //Setting column names as Property names
                    dataTable.Columns.Add(prop.Name, type);
                }
                log.Info("FileConvertor ToDataTable: getting items value and adding to dataTable");
                foreach (T item in items)
                {
                    var values = new object[Props.Length];
                    for (int i = 0; i < Props.Length; i++)
                    {
                        //inserting property values to datatable rows
                        values[i] = Props[i].GetValue(item, null);
                    }
                    dataTable.Rows.Add(values);
                }
                log.Info("FileConvertor ToDataTable: dataTable rows count: "+ dataTable.Rows.Count);
                log.Info("FileConvertor ToDataTable: Method End");                
            }
            catch(Exception ex)
            {
                log.Error("FileConvertor ToDataTable: Exception: " + ex.Message);
                throw;
            }            
            return dataTable;
        }

        /// <summary>
        ///  Method is used to download file and convertinto excel or csv files
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileExtension"></param>        
        /// <returns>retun the excel/csv file</returns>
        public string ExportCSVExcelToFile(string filePath, string fileExtension)
        {
            var client = new WebClient();
            var tempFilePath = string.Empty;
            try
            {
                log.Info("FileConvertor ExportCSVExcelToFile: Entered ExportCSVToFile");
                var tempPath = Path.GetTempPath(); // Get %TEMP% path
                var tempFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()); // Get random file name without extension
                tempFilePath = Path.Combine(tempPath, tempFileName + fileExtension); // Get random file path
                log.Info("FileConvertor ExportCSVExcelToFile:Temp File Path Created for Downloading Input File at path : " + tempFilePath);

                log.Info("FileConvertor ExportCSVExcelToFile: CSV File Downloading Started at Temp Location : " + tempFilePath);
                client.DownloadFile(filePath, tempFilePath);
                log.Info("FileConvertor ExportCSVExcelToFile:CSV File Downloading Completed at Temp Location : " + tempFilePath);
                log.Info("FileConvertor ExportCSVExcelToFile: Exited");
            }
            catch (Exception ex)
            {                
                log.Error(DateTime.Now + " Exception :" + ex.InnerException + " " + ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                client.Dispose();                
            }            
            return tempFilePath;
        }

        /// <summary>
        ///  Method is used to provide excel or csv files
        /// </summary>
        /// <param name="csvFilePathUri"></param>
        /// <param name="fileExtension"></param>
        /// <param name="status"></param>         
        /// <returns>retun the excel/csv file and status</returns>
        public string ReadCSVFile(string csvFilePathUri, string fileExtension, out string status)
        {   
            string filepath = string.Empty;
            status = "failed";
            try
            {
                log.Info("FileConvertor ReadCSVFile: Method start");
                filepath = ExportCSVExcelToFile(csvFilePathUri, fileExtension);
                if ((!string.IsNullOrEmpty(filepath)) && File.Exists(filepath))
                {
                    status = "success";
                }
                log.Info("FileConvertor ReadCSVFile: Method end");
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor ReadCSVFile: Exception: " + ex.Message);                
            }            
            return filepath;
        }

        /// <summary>
        ///  Method is used to econvert excel file to csv file
        /// </summary>
        /// <param name="excelFilePathUri"></param>
        /// <param name="fileExtension"></param>
        /// <param name="status"></param>         
        /// <returns>retun the csv file</returns>
        public string ExcelToCSV(string excelFilePathUri, string fileExtension, out string status)
        {            
            string filepath = string.Empty;
            string tempFilePath = string.Empty;
            status = "failed";
            try
            {
                log.Info("FileConvertor ExcelToCSV: Method start");
                log.Info("FileConvertor ExcelToCSV: Download file from path and save in temp file");
                tempFilePath = ExportCSVExcelToFile(excelFilePathUri, ".xlsx");
                log.Info("FileConvertor ExcelToCSV: convert temp file to datatable");
                DataTable dataTable = ConvertExcelToDataTable(tempFilePath);
                log.Info("FileConvertor ExcelToCSV: convert datatable to csv file");
                filepath = WriteDataTableToFile(dataTable, fileExtension);

                if ((!string.IsNullOrEmpty(filepath)) && File.Exists(filepath))
                {
                    status = "success";
                }
                log.Info("FileConvertor ExcelToCSV: Method end");
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor ExcelToCSV: Exception: " + ex.Message);
                filepath = string.Empty;                
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }            
            return filepath;
        }

        /// <summary>
        /// Convert DataTable to JSON using Nuget
        /// </summary>
        /// <param name="table"></param>
        /// <returns>string</returns>
        public string ConvertDataTableToJSONWithJSONNet(DataTable table)
        {
            try
            {
                log.Info("Entering DataTableToJSONWithJSONNet: Serialize data to json");
                string JSONString = string.Empty;
                JSONString = JsonConvert.SerializeObject(table);
                log.Info("Exiting DataTableToJSONWithJSONNet: Serialize data to json");
                return JSONString;
            }
            catch (Exception ex)
            {
                log.Error("DataTableToJSONWithJSONNet: Exception " + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        ///  Method is used to convert excel file to datatable 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>retun the excel/csv file</returns>
        public DataTable ConvertExcelToDataTable(string filePath)
        {   
            FileStream stream = null;
            IExcelDataReader excelReader = null;
            DataTable dataTable = new DataTable();

            try
            {
                log.Info("FileConvertor ConvertExcelToDataTable: Method start");
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                excelReader = ExcelReaderFactory.CreateReader(stream);

                DataSet result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true,
                        ReadHeaderRow = rowReader =>
                        {
                            while (rowReader.GetValue(0) == null || (string.IsNullOrEmpty(rowReader.GetValue(0).ToString())))
                            {
                                rowReader.Read();
                            }
                        }
                    }
                });


                if (result != null && result.Tables.Count > 0)
                {
                    dataTable = result.Tables[0];
                }
                log.Info("FileConvertor ConvertExcelToDataTable: Method end");
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor ConvertExcelToDataTable: Exception Found "+ ex);
                throw;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
                if (excelReader != null)
                {
                    excelReader.Dispose();
                }
            }            
            return dataTable;
        }



        /// <summary>
        ///  Method is used to ReplaceContentV2 - start and end with " & replace " by ""
        /// </summary>
        /// <param name="inputText"></param>        
        /// <returns>retun inputText</returns>        
        private string ReplaceContent(string inputText)
        {            
            inputText = inputText.Replace("\"", "\"\""); // replace " by "".
            inputText = string.Format("\"{0}\"", inputText); //start and end with " 
            return inputText;
        }

        /// <summary>
        /// Convert CSV to DataTable
        /// </summary>
        /// <param name="strFilePath"></param>
        /// <returns>DataTable</returns>
        public DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dataTable = new DataTable();
            try
            {
                log.Info("FileConvertor ConvertCSVtoDataTable : Method start");                
                using (StreamReader streamReader = new StreamReader(strFilePath))
                {
                    string[] headers = streamReader.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }
                    while (!streamReader.EndOfStream)
                    {
                        string values = RemoveColumnDelimitersInsideValues(streamReader.ReadLine());
                        string[] rows = values.Split(',');
                        DataRow dr = dataTable.NewRow();
                        for (int index = 0; index < headers.Length; index++)
                        {
                            dr[index] = rows[index];
                        }
                        dataTable.Rows.Add(dr);
                    }
                }
                log.Info("FileConvertor ConvertCSVtoDataTable : Method end");
            }
            catch(Exception ex)
            {
                log.Error("FileConvertor ConvertCSVtoDataTable : Exception Found" + ex);
                throw;
            }
            return dataTable;
        }

        /// <summary>
        /// Convert CSV to DataTable
        /// </summary>
        /// <param name="strFilePath"></param>
        /// <returns>DataTable</returns>
        public DataTable ConvertSpecialCSVtoDataTable(string strFilePath)
        {
            DataTable dataTable = new DataTable();
            try
            {
                int row = 1;
                log.Info("FileConvertor ConvertSpecialCSVtoDataTable : Method start");
                using (StreamReader streamReader = new StreamReader(strFilePath))
                {
                    string[] allLines = File.ReadAllLines(strFilePath);
                    string[] headers = allLines[2].Split(new char[] { '\t' });
                    
                    foreach (string header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string rowString = streamReader.ReadLine().Replace("\t", ",");
                        if (row > 3)
                        {                            
                            string values = RemoveColumnDelimitersInsideValues(rowString);
                            string[] rows = values.Split(',');
                            DataRow dr = dataTable.NewRow();
                            for (int index = 0; index < headers.Length; index++)
                            {
                                dr[index] = rows[index];
                            }
                            dataTable.Rows.Add(dr);
                        }
                        row++;
                    }
                }                
                log.Info("FileConvertor ConvertSpecialCSVtoDataTable : Method end");
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor ConvertSpecialCSVtoDataTable : Exception Found" + ex);
                throw;
            }
            return dataTable;
        }

        /// <summary>
        /// Remove comma(,) from csv column value
        /// </summary>
        /// <param name="input"></param>
        /// <returns>string</returns>
        public string RemoveColumnDelimitersInsideValues(string input)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                log.Info("FileConvertor RemoveColumnDelimitersInsideValues : Method start");
                const char valueDelimiter = '"';
                const char columnDelimiter = ',';
                const char columnDelimiterCurrency = '$';
                bool isInsideValue = false;
                for (var index = 0; index < input.Length; index++)
                {
                    var currentChar = input[index];

                    if (currentChar == valueDelimiter)
                    {
                        isInsideValue = !isInsideValue;                        
                        continue;
                    }

                    if(currentChar.Equals(columnDelimiterCurrency))
                    {
                        continue;
                    }
                    else if (currentChar != columnDelimiter || !isInsideValue)
                    {
                        output.Append(currentChar);
                    }                    
                    // else ignore columnDelimiter inside value
                }
                log.Info("FileConvertor RemoveColumnDelimitersInsideValues : Method end");
            }
            catch(Exception ex)
            {
                log.Info("FileConvertor RemoveColumnDelimitersInsideValues : Exception "+ ex);
            }
            return output.ToString();
        }

        /// <summary>
        ///  Method is used to convert excel file to dataset 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>retun dataset</returns>
        public DataSet ConvertExcelToDataSet(string filePath)
        {   
            FileStream stream = null;
            IExcelDataReader excelReader = null;
            DataSet dataSet;

            try
            {
                log.Info("FileConvertor ConvertExcelToDataSet : Method start");
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                excelReader = ExcelReaderFactory.CreateReader(stream);
                
                dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true,
                        ReadHeaderRow = rowReader =>
                        {
                            while ((rowReader.GetValue(0) == null || (string.IsNullOrEmpty(rowReader.GetValue(0).ToString()))) && (rowReader.GetValue(1) == null || (string.IsNullOrEmpty(rowReader.GetValue(1).ToString()))))
                            {
                                rowReader.Read();
                            }
                        }
                    }
                });
                log.Info("FileConvertor ConvertExcelToDataSet : Method end");
                return dataSet;
            }
            catch (Exception ex)
            {
                log.Error("FileConvertor ConvertExcelToDataSet : Exception " + ex);
                throw;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
                if (excelReader != null)
                {
                    excelReader.Dispose();
                }
            }
        }
    }
}
