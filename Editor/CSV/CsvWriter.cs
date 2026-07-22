using System.Collections.Generic;
using System.Text;

namespace TRnK.Localization
{
    /// <summary>CSV writer that escapes fields for round-trip with <see cref="CsvParser"/> and Excel/Sheets compatibility.</summary>
    internal static class CsvWriter
    {
        /// <summary>Writes a grid of fields to a CSV string.</summary>
        internal static string Write(IReadOnlyList<IReadOnlyList<string>> rows,
                                     char separator = ',',
                                     bool emitBom = false)
        {
            if (rows == null || rows.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            if (emitBom) sb.Append('\uFEFF');

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row != null)
                {
                    for (int c = 0; c < row.Count; c++)
                    {
                        if (c > 0) sb.Append(separator);
                        AppendField(sb, row[c], separator);
                    }
                }

                if (r < rows.Count - 1) sb.Append('\n');
            }

            return sb.ToString();
        }

        private static void AppendField(StringBuilder sb, string value, char separator)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Empty field — no quoting needed
                return;
            }

            bool needsQuoting = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == separator || c == '"' || c == '\n' || c == '\r')
                {
                    needsQuoting = true;
                    break;
                }
            }

            if (!needsQuoting)
            {
                sb.Append(value);
                return;
            }

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"') sb.Append('"'); // Escape quote by doubling
                sb.Append(c);
            }
            sb.Append('"');
        }
    }
}
