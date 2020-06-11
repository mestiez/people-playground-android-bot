using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

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

        public override async Task OnMessage(SocketMessage arg, Android android, bool editedMessage)
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
                //else
                //{
                //    //holy shit, a valid workshop link!
                //    var trimmedUrl = content[0].Trim();
                //    if (arg.Content.Trim() == trimmedUrl)
                //    {
                //        //ok so it has nothing extra, just the link itself. time to embed a thing to make it clearer
                //        if (long.TryParse(trimmedUrl.Split('=').Last(), out long id))
                //            await PostMessageAboutPublishedFile(id, trimmedUrl, arg);
                //        else
                //            Console.WriteLine("non valid workshop link got through somehow wtf");
                //    }
                //}
            }
        }

        private bool UserCanBypass(SocketUser user, Android android)
        {
            var roles = android.MainGuild.GetUser(user.Id).Roles.Select(r => r.Id);
            return roles.Contains(Server.Roles.TrialMods) || roles.Contains(Server.Roles.Moderators) || roles.Contains(Server.Roles.Administrators) || roles.Contains(Server.Roles.Developers);
        }

        private bool IsValidSteamWorkshopLink(string url)
        {
            bool isValidUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
            bool isToSteamWorkshop = url.StartsWith(WorkshopPath) && url.LastIndexOf(WorkshopPath) == 0;
            return isValidUrl && isToSteamWorkshop;
        }

        private async Task PostMessageAboutPublishedFile(long fileId, string url, SocketMessage message)
        {
            //string request = $@"https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/?key={Android.ApiKey}&itemcount=1&publishedfileids%5B0%5D={fileId}";
            string request = $@"https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1";
            SteamApiResponseDeep response;

            try
            {
                var data = new Dictionary<string, string>{
                    { "key", Android.ApiKey },
                    { "itemcount", "1" },
                    { "publishedfileids[0]", fileId.ToString() },
                };
                var rawResponse = await Utils.HttpPost(request, data);
                response = JsonConvert.DeserializeObject<SteamApiResponse>(rawResponse).response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            if (response.resultcount < 1)
            {
                Console.WriteLine($"Workshop resultcount for {fileId} is {response.resultcount}");
                return;
            }

            PublishedFile publishedFile = response.publishedfiledetails[0];
            if (publishedFile.creator_app_id != 1118200) // is this even a PPG workshop item?
                Console.WriteLine($"{fileId} isnt actually a PPG item");

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = publishedFile.title;
            embed.ThumbnailUrl = publishedFile.preview_url;
            embed.Url = url;
            embed.Color = new Color(0x1b2838);
            embed.Description = publishedFile.description;

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private struct SteamApiResponse
        {
            public SteamApiResponseDeep response;
        }

        private struct SteamApiResponseDeep
        {
            public int result;
            public int resultcount;
            public PublishedFile[] publishedfiledetails;
        }

        private struct PublishedFile
        {
            public string publishedfileid;
            public string creator;
            public long creator_app_id;
            public string preview_url;
            public string title;
            public string description;
            // This is missing many members but I only need these... so who cares lmao
        }
    }
}
