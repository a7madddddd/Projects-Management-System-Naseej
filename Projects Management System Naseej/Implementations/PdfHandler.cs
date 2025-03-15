using Projects_Management_System_Naseej.Repositories;
using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Office.Interop.Excel;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace Projects_Management_System_Naseej.Implementations
{
    public class PdfHandler : IFileTypeHandler
    {
        public async Task<bool> CanHandleAsync(Stream fileStream, string fileName)
        {
            return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<byte[]> ConvertToAsync(Stream fileStream, string targetExtension)
        {
            if (targetExtension.Equals("txt", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new PdfReader(fileStream))
                using (var writer = new StringWriter())
                {
                    var pdfDocument = new PdfDocument(reader);
                    var document = new iText.Layout.Document(pdfDocument);

                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var strategy = new LocationTextExtractionStrategy();
                        var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                        writer.WriteLine(text);
                    }

                    return System.Text.Encoding.UTF8.GetBytes(writer.ToString());
                }
            }
            else
            {
                throw new NotSupportedException("Conversion to the specified format is not supported.");
            }
        }
    }
}