using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace AndroidBot.Listeners
{
    public class CrudeModListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.CrudeModding, Server.Channels.BotTesting, Server.Channels.Secret };
        public override ulong[] Roles => new[] { Server.Roles.Any };
        public override ulong[] Users => new[] { Server.Users.Any };

        public readonly string[] ModReleasePrefixes = new[] {
            "mod",
            "mod:",
            "mod;",
            "mod :",
            "mod ;",
        };

        public override async Task Initialise()
        {
            await LoadFromDisk();
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            if (arg.Author.IsBot) return;

            foreach (var prefix in ModReleasePrefixes.OrderByDescending(c => c.Length))
            {
                var simplifiedContent = arg.Content.Normalize().Trim().ToLower();
                if (!simplifiedContent.StartsWith(prefix)) continue;
                var strippedDown = simplifiedContent.Remove(0, prefix.Length);
                string modName = "";
                try
                {
                    modName = strippedDown.Substring(0, strippedDown.IndexOf("\n"));
                }
                catch (Exception)
                {
                    await arg.Channel.SendMessageAsync("you need to give your mod a description or something under it");
                    break;
                }

                if (CrudeModdingStorage.Current.IdentityMessageMap.TryGetValue(modName, out var messageId))
                {
                    IUserMessage existingReleaseMessage = (IUserMessage)await arg.Channel.GetMessageAsync(messageId);
                    if (existingReleaseMessage.Author.Id != arg.Author.Id)
                    {
                        await arg.Channel.SendMessageAsync("mod name has been claimed by " + existingReleaseMessage.Author.Username);
                        break;
                    }
                    await existingReleaseMessage.UnpinAsync();
                    CrudeModdingStorage.Current.IdentityMessageMap[modName] = arg.Id;
                }
                else
                {
                    CrudeModdingStorage.Current.IdentityMessageMap.Add(modName, arg.Id);
                }

                await ((IUserMessage)arg).PinAsync();
                await SaveToDisk();
                Console.WriteLine("Mod release/update for");
                break;
            }
            await Task.CompletedTask;
        }

        public override async Task Stop()
        {
            await SaveToDisk();
        }

        public async Task LoadFromDisk()
        {
            try
            {
                var raw = await File.ReadAllTextAsync(Android.Path + CrudeModdingStorage.Path);
                CrudeModdingStorage.Current = JsonConvert.DeserializeObject<CrudeModdingStorage>(raw);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid or non-existent CrudeModdingStorage file, creating a new one...");
                CrudeModdingStorage.Current = new CrudeModdingStorage
                {
                    IdentityMessageMap = new Dictionary<string, ulong>()
                };
                await SaveToDisk();
            }
        }

        public async Task SaveToDisk()
        {
            try
            {
                var raw = JsonConvert.SerializeObject(CrudeModdingStorage.Current);
                await File.WriteAllTextAsync(Android.Path + CrudeModdingStorage.Path, raw);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to save CrudeModdingStorage to disk: " + e.Message);
            }
        }
    }
}
