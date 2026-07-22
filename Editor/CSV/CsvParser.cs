using System;
using System.Collections.Generic;
using System.Text;

namespace TRnK.Localization
{
    /// <summary>CSV parser tailored to real-world Excel and Google Sheets exports (BOM, quoting, mixed line endings, comma/semicolon).</summary>
    internal static class CsvParser
    {
        const char BomChar = '\uFEFF';

        /// <summary>Parses CSV text into a grid of fields, auto-detecting the separator from the first line.</summary>
        /// <exception cref="FormatException">Thrown if a quoted field is unterminated.</exception>
        internal static List<List<string>> Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<List<string>>();
            char separator = DetectSeparator(text);
            return Parse(text, separator);
        }

        /// <summary>Parses CSV text using the given separator.</summary>
        internal static List<List<string>> Parse(string text, char separator)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;

            // Skip BOM if present
            int start = 0;
            if (text[0] == BomChar) start = 1;

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int line = 1;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escaped quote ("") — emit single quote, stay in quotes
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            // Closing quote
                            inQuotes = false;
                        }
                    }
                    else if (c == '\r')
                    {
                        // Normalize \r\n and bare \r to \n inside quoted fields
                        field.Append('\n');
                        if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                        line++;
                    }
                    else
                    {
                        if (c == '\n') line++;
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"' && field.Length == 0)
                    {
                        // Opening quote — only valid at the start of a field
                        inQuotes = true;
                    }
                    else if (c == separator)
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        // End of row — handle \r\n as one line break
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                        line++;
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            if (inQuotes)
                throw new FormatException($"Unterminated quoted field at line {line}.");

            // Emit the final field/row if the file didn't end with a newline
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        /// <summary>Detects comma vs semicolon separator from the first line, ignoring quoted content. Falls back to comma on a tie.</summary>
        internal static char DetectSeparator(string text)
        {
            if (string.IsNullOrEmpty(text)) return ',';

            int start = text[0] == BomChar ? 1 : 0;
            int commas = 0;
            int semicolons = 0;
            bool inQuotes = false;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    // Handle escaped quote inside quoted region
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }
                    inQuotes = !inQuotes;
                    continue;
                }

                if (inQuotes) continue;

                if (c == '\r' || c == '\n') break;
                if (c == ',') commas++;
                else if (c == ';') semicolons++;
            }

            return semicolons > commas ? ';' : ',';
        }
    }
}
