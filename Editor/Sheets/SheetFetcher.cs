#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Networking;

namespace TRnK.Localization
{
    /// <summary>Downloads spreadsheet tabs as CSV, without blocking the editor.</summary>
    internal static class SheetFetcher
    {
        private const int TimeoutSeconds = 20;
        private const string CsvUrlFormat =
            "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}";

        internal sealed class Result
        {
            /// <summary>Tab name to its CSV text; only populated when every tab succeeded.</summary>
            internal readonly Dictionary<string, string> Csv = new();
            internal string Error;
            internal bool Success => Error == null;
        }

        /// <summary>Extracts the spreadsheet id from any of its URLs; null when the URL is not a spreadsheet link.</summary>
        internal static string ExtractSpreadsheetId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = Regex.Match(url, @"/spreadsheets/d/([a-zA-Z0-9_\-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Fetches every tab, reporting progress and honouring cancellation.
        /// All-or-nothing: one failed tab fails the whole result, so a partial
        /// download can never be applied as if tables had been deleted.
        /// </summary>
        internal static void FetchAll(string spreadsheetUrl, IReadOnlyList<string> tabNames, Action<Result> onDone)
        {
            var result = new Result();

            string id = ExtractSpreadsheetId(spreadsheetUrl);
            if (id == null)
            {
                result.Error = "Spreadsheet URL is empty or not a Google Sheets link.";
                onDone(result);
                return;
            }

            if (tabNames == null || tabNames.Count == 0)
            {
                result.Error = "No tabs listed. Add the spreadsheet tab names to sync.";
                onDone(result);
                return;
            }

            int progressId = Progress.Start("Localization Sync", "Fetching sheets…");

            int index = 0;
            bool cancelled = false;
            bool finished = false;
            UnityWebRequest request = null;

            Progress.RegisterCancelCallback(progressId, () =>
            {
                cancelled = true;
                return true;
            });

            void Cleanup()
            {
                EditorApplication.update -= Poll;
                request?.Dispose();
                request = null;

                if (Progress.Exists(progressId))
                    Progress.Remove(progressId);
            }

            void Finish(string error)
            {
                // Poll can re-enter if a callback throws; only the first outcome counts
                if (finished) return;
                finished = true;

                result.Error = error;
                Cleanup();
                onDone(result);
            }

            void StartNext()
            {
                string tab = tabNames[index];
                Progress.Report(progressId, index / (float)tabNames.Count, $"Fetching '{tab}'…");

                request = UnityWebRequest.Get(string.Format(CsvUrlFormat, id, Uri.EscapeDataString(tab)));
                request.timeout = TimeoutSeconds;
                request.SendWebRequest();
            }

            void Poll()
            {
                if (cancelled)
                {
                    request?.Abort();
                    Finish("Sync cancelled.");
                    return;
                }

                if (request == null || !request.isDone) return;

                string tab = tabNames[index];

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Finish($"Tab '{tab}' could not be downloaded: {request.error}");
                    return;
                }

                string text = request.downloadHandler.text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    Finish($"Tab '{tab}' returned no data. It may be empty.");
                    return;
                }

                // Google serves a login/error page as HTML when the sheet is not link-shared
                // or the tab does not exist — parsing it as CSV would fail confusingly.
                if (LooksLikeHtml(text))
                {
                    Finish($"Tab '{tab}' was not returned as data. Check the tab name, and that the " +
                           "spreadsheet is shared as 'Anyone with the link → Viewer'.");
                    return;
                }

                result.Csv[tab] = text;

                request.Dispose();
                request = null;

                index++;
                if (index >= tabNames.Count)
                {
                    if (finished) return;
                    finished = true;

                    Cleanup();
                    onDone(result);
                    return;
                }

                StartNext();
            }

            EditorApplication.update += Poll;
            StartNext();
        }

        private static bool LooksLikeHtml(string text)
        {
            string head = text.TrimStart();
            return head.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                   || head.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
