using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public class CrudeModdingStorage
    {
        public const string Path = "CrudeModdingStorage.json";
        [NonSerialized]
        public static CrudeModdingStorage Current;

        public Dictionary<string, ulong> IdentityMessageMap = new Dictionary<string, ulong>();
    }
}
