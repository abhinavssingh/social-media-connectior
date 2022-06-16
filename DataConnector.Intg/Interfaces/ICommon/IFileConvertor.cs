using System.Collections.Generic;
using System.Data;

namespace DataConnector.Intg.Interfaces.ICommon
{
    public interface IFileConvertor
    {
        string WriteDataTableToFile(DataTable dataTable, string fileExtension = ".CSV", string existingFilePath = null, bool flgUseExistingData = true);
        DataTable ToDataTable<T>(List<T> items);
        string ExportCSVExcelToFile(string csvFilePath, string fileExtension);
        string ReadCSVFile(string csvFilePathUri, string fileExtension, out string status);
        string ExcelToCSV(string excelFilePathUri, string fileExtension, out string status);
        string ConvertDataTableToJSONWithJSONNet(DataTable table);
        DataTable ConvertCSVtoDataTable(string strFilePath);
        string RemoveColumnDelimitersInsideValues(string input);
        DataTable ConvertExcelToDataTable(string filePath);
        DataSet ConvertExcelToDataSet(string filePath);
        DataTable ConvertSpecialCSVtoDataTable(string strFilePath);
    }
}
