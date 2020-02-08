using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public class ShareWorkshopListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.ShareWorkshop };
        public override ulong[] Roles => new[] { Server.Roles.Any };
        public override ulong[] Users => new[] { Server.Users.Any };
        public const string WorkshopPath = "https://steamcommunity.com/sharedfiles/filedetails/?id=";

        public override Task Initialise(Android android)
        {
            return Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            if (string.IsNullOrWhiteSpace(arg.Content))
                await arg.Channel.DeleteMessageAsync(arg);
            else
            {
                var content = arg.Content.Split("\n ".ToCharArray());
                bool isValidWorkshopLink = IsValidSteamWorkshopLink(content[0]);
                bool userBypass = UserCanBypass(arg.Author, android);
                if (!isValidWorkshopLink && !userBypass)
                    await arg.Channel.DeleteMessageAsync(arg);
            }
        }

        private bool UserCanBypass(SocketUser user, Android android)
        {
            var roles = android.MainGuild.GetUser(user.Id).Roles.Select(r => r.Id);
            return roles.Contains(Server.Roles.Moderators) || roles.Contains(Server.Roles.Administrators) || roles.Contains(Server.Roles.Developers);
        }

        private bool IsValidSteamWorkshopLink(string url)
        {
            bool isValidUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
            bool isToSteamWorkshop = url.StartsWith(WorkshopPath) && url.LastIndexOf(WorkshopPath) == 0;
            return isValidUrl && isToSteamWorkshop;
        }
    }
}
