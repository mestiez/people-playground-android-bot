﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public struct DebugResponseConfiguration
    {
        public const string Path = "DebugResponseConfiguration.json";
        [NonSerialized]
        public static DebugResponseConfiguration Current;

        public float MaxWaitingTimeInSeconds;

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

        public string[] ModCleaningAliases;

        public string[] RoadmapAliases;
        public string RoadmapLink;

        public string[] ChangelogAliases;
        public string ChangelogLink;
    }
}
