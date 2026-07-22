using System;
using UnityEngine;

namespace TRnK.Localization
{
    [Serializable]
    public struct LocaleValue
    {
        public string LocaleCode;

        [TextArea(1, 4)]
        public string Value;
    }
}
