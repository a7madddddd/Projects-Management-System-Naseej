using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class ExcelHandler : IFileTypeHandler
    {
        public async Task<bool> CanHandleAsync(Stream fileStream, string fileName)
        {
            return fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<byte[]> ConvertToAsync(Stream fileStream, string targetExtension)
        {
            if (targetExtension.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                Application excelApp = new Application();
                string tempFilePath = Path.GetTempFileName();

                try
                {
                    // Save the stream to a temporary file
                    using (var fileStreamCopy = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await fileStream.CopyToAsync(fileStreamCopy);
                    }

                    // Open the temporary file
                    Workbook workbook = excelApp.Workbooks.Open(tempFilePath);
                    Worksheet worksheet = (Worksheet)workbook.Worksheets[1]; // Explicit cast to Worksheet
                    string convertedFilePath = Path.GetTempFileName();

                    try
                    {
                        workbook.ExportAsFixedFormat(XlFixedFormatType.xlTypePDF, convertedFilePath);
                        return await File.ReadAllBytesAsync(convertedFilePath);
                    }
                    finally
                    {
                        workbook.Close();
                        File.Delete(tempFilePath);
                    }
                }
                finally
                {
                    excelApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    File.Delete(tempFilePath);
                }
            }
            else
            {
                throw new NotSupportedException("Conversion to the specified format is not supported.");
            }
        }
    }
}