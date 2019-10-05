using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public struct DebugResponseConfiguration
    {
        public string[] Prefixes;
        public string[] Names;
        public string[] Suffixes;

        public string[] GreetingResponses;
        public string[] TaskAwaitResponses;

        public string[] NevermindTriggers;
        public string[] GreetingTriggers;

        public string[] MinuteUnitFallbackResponse;
        public string[] FifteenMinuteFallbackResponse;
        public string[] NoUserSpecifiedResponse;

        public string[] MutingNotification;
        public string[] UnmutingNotification;
    }
}
