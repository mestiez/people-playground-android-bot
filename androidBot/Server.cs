using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public struct Server
    {
        public struct Channels
        {
            public const ulong Any = 0;

            public const ulong Information = 603876092079636499;
            public const ulong Updates = 603934956380749834;
            public const ulong Announcements = 621306506708516889;
            public const ulong Suggestions = 605732562492456970;
            public const ulong BugReports = 612279652404428800;

            public const ulong General = 603649974042886296;
            public const ulong PeoplePlayground = 603849809178132501;
            public const ulong UnfunnyMemes = 611284040036646922;
        }

        public struct Users
        {
            public const ulong Any = 0;

            public const ulong Mestiez = 158883055367487488;
            public const ulong Vincent = 209640476775677952;
            public const ulong Vila = 298068138698735617;
            public const ulong JoeLouis = 187872460232851467;
            public const ulong Besm = 150739418481688576;
        }

        public struct Roles
        {
            public const ulong Any = 0;

            public const ulong Everyone = 603649973510340619;
            public const ulong Muted = 604692303550087207;
            public const ulong Bots = 621670270717132801;
            public const ulong Boosters = 619331382484926465;
            public const ulong Moderators = 603650422884007956;
            public const ulong Developers = 603657996614107138;
        }

        public struct Emotes
        {
            public static Emote YES => Emote.Parse("<:YES:604730173379706888>");
            public static Emote NO => Emote.Parse("<:NO:604730173236969472>");
        }
    }
}
