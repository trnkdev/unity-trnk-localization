using System.Globalization;

namespace TRnK.Localization
{
    /// <summary>A named smart-string argument, created implicitly from a (name, value) tuple.</summary>
    public readonly struct LocArg
    {
        internal readonly string Name;
        internal readonly string Value;

        private LocArg(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public static implicit operator LocArg((string name, string value) arg)
            => new(arg.name, arg.value);

        public static implicit operator LocArg((string name, long value) arg)
            => new(arg.name, arg.value.ToString(CultureInfo.InvariantCulture));

        public static implicit operator LocArg((string name, double value) arg)
            => new(arg.name, arg.value.ToString(CultureInfo.InvariantCulture));
    }
}
