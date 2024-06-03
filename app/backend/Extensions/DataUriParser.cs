// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class DataUriParser
{
    public string MediaType { get; private set; }
    public bool IsBase64 { get; private set; }
    public byte[] Data { get; private set; }

    public DataUriParser(string dataUri)
    {
        Parse(dataUri);
    }

    private void Parse(string dataUri)
    {
        if (!dataUri.StartsWith("data:"))
        {
            throw new ArgumentException("Invalid Data URI.");
        }

        // Remove the initial "data:" prefix
        string content = dataUri.Substring(5);

        // Find the first comma, which ends the header
        int commaIndex = content.IndexOf(',');
        if (commaIndex == -1)
        {
            throw new ArgumentException("Invalid Data URI.");
        }

        // Extract header and data parts
        string header = content.Substring(0, commaIndex);
        string dataPart = content.Substring(commaIndex + 1);

        // Check for Base64 encoding
        IsBase64 = header.EndsWith(";base64");
        if (IsBase64)
        {
            // Remove ";base64" from header
            MediaType = header.Substring(0, header.Length - 7);
        }
        else
        {
            MediaType = header;
        }

        // Decode the data part
        if (IsBase64)
        {
            Data = Convert.FromBase64String(dataPart);
        }
        else
        {
            // URL decode if not Base64
            string decodedData = Uri.UnescapeDataString(dataPart);
            Data = System.Text.Encoding.UTF8.GetBytes(decodedData);
        }
    }
}
