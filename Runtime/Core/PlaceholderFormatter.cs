using System.Text;
using TRnK.Logger;

namespace TRnK.Localization
{
    /// <summary>Replaces named {placeholder} tokens in a localized string with argument values.</summary>
    internal static class PlaceholderFormatter
    {
        private const int InitialCapacity = 128;

        // Per-thread: a shared builder would interleave output if a job thread ever formats
        [System.ThreadStatic] private static StringBuilder t_builder;

        internal static string Format(string format, LocArg[] args)
        {
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format;

            if (format.IndexOf('{') < 0)
                return format;

            var sb = t_builder ??= new StringBuilder(InitialCapacity);
            sb.Clear();

            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];

                if (c == '{')
                {
                    // {{ escapes a literal brace
                    if (i + 1 < format.Length && format[i + 1] == '{')
                    {
                        sb.Append('{');
                        i++;
                        continue;
                    }

                    int close = format.IndexOf('}', i + 1);
                    if (close < 0)
                    {
                        // Unclosed brace stays literal
                        sb.Append(c);
                        continue;
                    }

                    if (TryGetValue(args, format, i + 1, close, out string value))
                    {
                        sb.Append(value);
                        i = close;
                        continue;
                    }

#if UNITY_EDITOR
                    Log.Warn($"Unknown placeholder '{format.Substring(i, close - i + 1)}' in localized string.");
#endif
                    // Unknown placeholder stays literal; the loop appends its remaining characters
                    sb.Append(c);
                }
                else if (c == '}')
                {
                    // }} escapes a literal brace
                    if (i + 1 < format.Length && format[i + 1] == '}')
                        i++;
                    sb.Append('}');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // Matches arg names against format[start..end) without allocating a substring.
        private static bool TryGetValue(LocArg[] args, string format, int start, int end, out string value)
        {
            int length = end - start;

            foreach (var arg in args)
            {
                string name = arg.Name;
                if (name == null || name.Length != length) continue;

                bool match = true;
                for (int i = 0; i < length; i++)
                {
                    if (name[i] != format[start + i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    value = arg.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
