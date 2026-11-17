#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Networking;

namespace TRnK.Localization
{
    /// <summary>Downloads a Google spreadsheet as .xlsx and reads the cell grid of every tab.</summary>
    internal static class SheetWorkbook
    {
        private const int TimeoutSeconds = 30;
        private const string ExportUrlFormat = "https://docs.google.com/spreadsheets/d/{0}/export?format=xlsx";
        private const string WorkbookPath = "xl/workbook.xml";
        private const string WorkbookRelsPath = "xl/_rels/workbook.xml.rels";
        private const string SharedStringsPath = "xl/sharedStrings.xml";
        private const string WorkbookFolder = "xl/";
        private const int ColumnBase = 26;

        private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly Regex SpreadsheetIdPattern = new(@"/spreadsheets/d/([a-zA-Z0-9_\-]+)");

        /// <summary>The spreadsheet id from any of its URLs, or null.</summary>
        internal static string ExtractSpreadsheetId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = SpreadsheetIdPattern.Match(url);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>Downloads the spreadsheet and returns its tabs in sheet order.</summary>
        internal static async Task<List<(string Name, List<List<string>> Rows)>> DownloadAsync(string spreadsheetId)
        {
            using var request = UnityWebRequest.Get(string.Format(ExportUrlFormat, spreadsheetId));
            request.timeout = TimeoutSeconds;

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new IOException($"The spreadsheet could not be downloaded: {request.error}");

            byte[] bytes = request.downloadHandler.data;

            // Google answers with a sign-in page instead of the file when the sheet is not link-shared
            if (bytes == null || bytes.Length < 2 || bytes[0] != 'P' || bytes[1] != 'K')
                throw new IOException("The spreadsheet was not returned as a file. Share it as 'Anyone with the link → Viewer'.");

            return Read(bytes);
        }

        /// <summary>Reads every tab of an .xlsx file, in sheet order.</summary>
        internal static List<(string Name, List<List<string>> Rows)> Read(byte[] xlsx)
        {
            using var archive = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);

            var sharedStrings = ReadSharedStrings(archive);
            var tabs = new List<(string, List<List<string>>)>();

            foreach (var (name, path) in ReadSheetPaths(archive))
            {
                var entry = archive.GetEntry(path);
                if (entry != null) tabs.Add((name, ReadRows(entry, sharedStrings)));
            }

            return tabs;
        }

        private static List<(string Name, string Path)> ReadSheetPaths(ZipArchive archive)
        {
            var pathById = new Dictionary<string, string>();
            foreach (var rel in Load(archive, WorkbookRelsPath).Root.Elements())
            {
                string id = rel.Attribute("Id")?.Value;
                string target = rel.Attribute("Target")?.Value;
                if (id == null || target == null) continue;

                pathById[id] = target.StartsWith("/") ? target.Substring(1) : WorkbookFolder + target;
            }

            var sheets = new List<(string, string)>();
            foreach (var sheet in Load(archive, WorkbookPath).Descendants(MainNs + "sheet"))
            {
                string name = sheet.Attribute("name")?.Value;
                string id = sheet.Attribute(RelNs + "id")?.Value;
                if (name != null && id != null && pathById.TryGetValue(id, out string path))
                    sheets.Add((name, path));
            }

            return sheets;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var strings = new List<string>();
            if (archive.GetEntry(SharedStringsPath) == null) return strings;

            foreach (var item in Load(archive, SharedStringsPath).Root.Elements(MainNs + "si"))
                strings.Add(Text(item));

            return strings;
        }

        private static List<List<string>> ReadRows(ZipArchiveEntry entry, List<string> sharedStrings)
        {
            XDocument document;
            using (var stream = entry.Open()) document = XDocument.Load(stream);

            var rows = new List<List<string>>();
            foreach (var rowElement in document.Descendants(MainNs + "row"))
            {
                int rowIndex = int.TryParse(rowElement.Attribute("r")?.Value, out int r) ? r - 1 : rows.Count;
                while (rows.Count <= rowIndex) rows.Add(new List<string>());

                var row = rows[rowIndex];
                foreach (var cell in rowElement.Elements(MainNs + "c"))
                {
                    int column = ColumnIndex(cell.Attribute("r")?.Value, row.Count);
                    while (row.Count <= column) row.Add(string.Empty);
                    row[column] = CellValue(cell, sharedStrings);
                }
            }

            return rows;
        }

        private static string CellValue(XElement cell, List<string> sharedStrings)
        {
            string type = cell.Attribute("t")?.Value;
            if (type == "inlineStr") return Text(cell.Element(MainNs + "is"));

            string value = cell.Element(MainNs + "v")?.Value;
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (type == "s")
                return int.TryParse(value, out int index) && index >= 0 && index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;

            return value;
        }

        // Plain and rich-text runs only; phonetic runs (furigana) would otherwise be glued onto the text
        private static string Text(XElement container)
        {
            if (container == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var element in container.Elements())
            {
                if (element.Name == MainNs + "t") sb.Append(element.Value);
                else if (element.Name == MainNs + "r") sb.Append(element.Element(MainNs + "t")?.Value);
            }

            return sb.ToString();
        }

        // "B7" -> 1; cells without a reference follow the previous one
        private static int ColumnIndex(string cellReference, int fallback)
        {
            if (string.IsNullOrEmpty(cellReference)) return fallback;

            int value = 0;
            foreach (char c in cellReference)
            {
                if (c < 'A' || c > 'Z') break;
                value = value * ColumnBase + (c - 'A' + 1);
            }

            return value > 0 ? value - 1 : fallback;
        }

        private static XDocument Load(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"The spreadsheet file has no '{path}'.");
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }
    }
}
#endif
