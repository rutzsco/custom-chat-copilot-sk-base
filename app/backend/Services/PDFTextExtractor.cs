// Copyright (c) Microsoft. All rights reserved.

using UglyToad.PdfPig;
namespace MinimalApi.Services;

public class PDFTextExtractor
{
    public static string ExtractTextFromPdf(byte[] content)
    {
        StringBuilder text = new StringBuilder();
        using (PdfDocument document = PdfDocument.Open(content))
        {
            // Loop through each page in the PDF
            foreach (var page in document.GetPages())
            {
                // Append the text from each page
                text.AppendLine(page.Text);
            }
        }
        // Return the extracted text
        return text.ToString();
    }
}
