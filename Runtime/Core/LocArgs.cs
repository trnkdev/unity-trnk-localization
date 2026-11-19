namespace TRnK.Localization
{
    /// <summary>Up to eight positional arguments for {0}–{7}, passed without allocating an array.</summary>
    internal readonly struct LocArgs
    {
        internal const int MaxCount = 8;

        internal readonly int Count;

        private readonly string _arg0;
        private readonly string _arg1;
        private readonly string _arg2;
        private readonly string _arg3;
        private readonly string _arg4;
        private readonly string _arg5;
        private readonly string _arg6;
        private readonly string _arg7;

        // Count is explicit because a null argument is still an argument
        internal LocArgs(int count, string arg0, string arg1 = null, string arg2 = null, string arg3 = null,
                         string arg4 = null, string arg5 = null, string arg6 = null, string arg7 = null)
        {
            Count = count;
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
            _arg7 = arg7;
        }

        internal string this[int index] => index switch
        {
            0 => _arg0,
            1 => _arg1,
            2 => _arg2,
            3 => _arg3,
            4 => _arg4,
            5 => _arg5,
            6 => _arg6,
            7 => _arg7,
            _ => null,
        };
    }
}
