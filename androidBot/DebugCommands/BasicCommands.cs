using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer]
    public struct BasicCommands
    {
        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.NevermindTriggers))]
        public static async Task Nothing(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("ok");
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.GreetingTriggers))]
        public static async Task Hi(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.GreetingResponses.PickRandom());
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Play(CommandParameters parameters)
        {
            var activityType = ActivityType.Playing;
            await SetStatus(parameters, activityType);
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Watch(CommandParameters parameters)
        {
            var activityType = ActivityType.Watching;
            await SetStatus(parameters, activityType);
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Listen(CommandParameters parameters)
        {
            var activityType = ActivityType.Listening;
            await SetStatus(parameters, activityType);
        }

        [Command(roles: new[] { Server.Roles.Developers, Server.Roles.Administrators })]
        public static async Task ListPins(CommandParameters parameters)
        {
            string result = "All pinned messages\n\n";
            foreach (var item in parameters.Android.MainGuild.TextChannels)
            {
                var pins = await item.GetPinnedMessagesAsync();
                foreach (var pin in pins)
                {
                    string entry = $"{pin.Timestamp.ToString()} by {pin.Author.Username}({pin.Author.Discriminator}) [{pin.GetJumpUrl()}]\n\t{pin.Content}\n\n";
                    result += entry;
                }
            }

            await Utils.SendTextAsFile(parameters.SocketMessage.Channel, result, "pins.txt");
        }

        [Command(roles: new[] { Server.Roles.Developers, Server.Roles.Administrators })]
        public static async Task Archive(CommandParameters parameters)
        {
            const int MaxMessageCount = 10000;

            Regex regex = new Regex(@"(<#\d+>)");
            var matches = regex.Matches(parameters.SocketMessage.Content);
            await parameters.SocketMessage.Channel.SendMessageAsync($"okay! working on it");
            var files = new List<ChannelArchive>();

            if (matches.Count == 0)
                await addToFiles(parameters.SocketMessage.Channel);

            foreach (Match match in matches)
            {
                bool successfulParse = ulong.TryParse(new string(match.Value.Where(c => char.IsDigit(c)).ToArray()), out var channelId);
                if (!successfulParse) continue;
                var channel = parameters.Android.Client.GetChannel(channelId);
                if (channel == null) continue;
                try
                {
                    IMessageChannel tc = (IMessageChannel)channel;
                    await addToFiles(tc);
                }
                catch (Exception)
                {
                    Console.WriteLine(channel.Id + " is not a text channel");
                }
            }

            foreach (var archive in files)
                await Utils.SendTextAsFile(parameters.SocketMessage.Channel, archive.Data, $"{archive.Name}.txt");

            async Task addToFiles(IMessageChannel tc)
            {
                var messages = tc.GetMessagesAsync(limit: MaxMessageCount, mode: CacheMode.AllowDownload);
                string file = "";
                await messages.ForEachAwaitAsync(m =>
                {
                    foreach (var item in m)
                        file = file.Insert(0, $"{item.Author} ({item.Timestamp.UtcDateTime})\n\t{item.Content}\n\n");
                    return Task.CompletedTask;
                });

                file = file.Insert(0, $"#{tc.Name} at {DateTime.UtcNow}\nBiscuit can only archive the last {MaxMessageCount} messages\n\n");

                files.Add(new ChannelArchive(tc.Name, file));
            }
        }

        private static async Task SetStatus(CommandParameters parameters, ActivityType activityType)
        {
            Console.WriteLine("Status set: " + activityType.ToString() + " " + string.Join(" ", parameters.Arguments));
            if (parameters.Arguments.Length == 0) await Task.CompletedTask;
            string game = string.Join(" ", parameters.Arguments);

            if (string.IsNullOrWhiteSpace(game)) return;

            await parameters.Android.Client.SetActivityAsync(new Game(game, activityType));
        }

        private struct ChannelArchive
        {
            public string Name;
            public string Data;

            public ChannelArchive(string name, string data)
            {
                Name = name;
                Data = data;
            }
        }
    }
}
