using System;
using System.Text;
using TRnK.Logger;

namespace TRnK.Localization
{
    /// <summary>Replaces positional {0}–{7} placeholders in a localized string with argument values.</summary>
    internal static class PlaceholderFormatter
    {
        private const int InitialCapacity = 128;
        private const char FirstIndex = '0';
        private const char LastIndex = (char)(FirstIndex + LocArgs.MaxCount - 1);

        private static readonly char[] Braces = { '{', '}' };

        // Per-thread: a shared builder would interleave output if a job thread ever formats
        [ThreadStatic] private static StringBuilder t_builder;

        internal static string Format(string format, in LocArgs args)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOfAny(Braces) < 0)
                return format;

            var sb = t_builder ??= new StringBuilder(InitialCapacity);
            sb.Clear();

            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];
                bool hasNext = i + 1 < format.Length;

                // {{ and }} escape literal braces
                if ((c == '{' || c == '}') && hasNext && format[i + 1] == c)
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                if (c == '{' && i + 2 < format.Length && format[i + 2] == '}'
                    && format[i + 1] >= FirstIndex && format[i + 1] <= LastIndex)
                {
                    AppendArg(sb, format, format[i + 1] - FirstIndex, args);
                    i += 2;
                    continue;
                }

                // Any other brace stays literal
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static void AppendArg(StringBuilder sb, string format, int index, in LocArgs args)
        {
            if (index < args.Count)
            {
                sb.Append(args[index]);
                return;
            }

            Log.Warn($"Placeholder {{{index}}} has no argument ({args.Count} given) in \"{format}\".");
            sb.Append('{').Append(index).Append('}');
        }
    }
}
