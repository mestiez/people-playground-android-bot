using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public partial class DebugListener : MessageListener
    {
        private readonly string[] greetings = { "hi", ":)", "hello", "o/", "o//", "hi :)" };

        [Command]
        public async Task Ping(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("pong");
        }

        [Command(new[] { "nevermind", "shut", "cancel", "nothing", "nvm", "ignore", "no" })]
        public async Task Nothing(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("ok");
        }

        [Command(new[] { "hello", "hey", "o/", "oi", "hey", ":)", "greetings" })]
        public async Task Hi(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(greetings.PickRandom());
        }

        [Command]
        public async Task Play(CommandParameters parameters)
        {
            var activityType = ActivityType.Playing;
            await SetStatus(parameters, activityType);
        }

        [Command]
        public async Task Watch(CommandParameters parameters)
        {
            var activityType = ActivityType.Watching;
            await SetStatus(parameters, activityType);
        }

        [Command]
        public async Task Listen(CommandParameters parameters)
        {
            var activityType = ActivityType.Listening;
            await SetStatus(parameters, activityType);
        }

        [Command]
        public async Task Mute(CommandParameters parameters)
        {
            TimeSpan duration = TimeSpan.FromMinutes(15);

            //find a specified time
            var match = Regex.Match(parameters.SocketMessage.Content, @"(for)\s(\d*)\s(\w*\b)");
            if (match.Success)
            {
                try
                {
                    int parsedNumber = int.Parse(new string(match.Value.Where(c => char.IsDigit(c)).ToArray()));

                    if (match.Value.Contains("second"))
                        duration = TimeSpan.FromSeconds(parsedNumber);
                    else if (match.Value.Contains("minute"))
                        duration = TimeSpan.FromMinutes(parsedNumber);
                    else if (match.Value.Contains("hour"))
                        duration = TimeSpan.FromHours(parsedNumber);
                    else
                    {
                        duration = TimeSpan.FromMinutes(parsedNumber);
                        await parameters.SocketMessage.Channel.SendMessageAsync("i don't know what unit of time that is, so i'll do minutes");
                        await Task.Delay(TimeSpan.FromSeconds(0.5f));
                    }
                }
                catch (Exception)
                {
                    await parameters.SocketMessage.Channel.SendMessageAsync("i don't know what \"" + match.Value + "\" means, so I will fall back to the default of 15 minutes");
                    await Task.Delay(TimeSpan.FromSeconds(0.5f));
                }
            }

            await SetMuteStatus(parameters, true);
            _ = Task.Run(async () =>
              {
                  await Task.Delay(duration);
                  await SetMuteStatus(parameters, false);
              });
        }

        [Command]
        public async Task Silence(CommandParameters parameters)
        {
            await Mute(parameters);
        }

        [Command]
        public async Task Unmute(CommandParameters parameters)
        {
            await SetMuteStatus(parameters, false);
        }

        private static async Task SetMuteStatus(CommandParameters parameters, bool muted)
        {
            var mutedRole = parameters.Android.MainGuild.GetRole(Server.Roles.Muted);
            var matches = Regex.Matches(parameters.SocketMessage.Content, "<@(.*?)>");
            if (!matches.Any())
            {
                await parameters.SocketMessage.Channel.SendMessageAsync("you didn't specify any users");
                return;
            }

            List<SocketGuildUser> usersToMute = new List<SocketGuildUser>();

            foreach (Match match in matches)
            {
                string toParse = new string(match.ToString().Where(c => char.IsDigit(c)).ToArray());
                if (!ulong.TryParse(toParse, out ulong result)) continue;
                try
                {
                    usersToMute.Add(parameters.Android.MainGuild.GetUser(result));
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot retrieve user with id " + result);
                }
            }

            string message = (muted ? "muting " : "unmuting ") + string.Join(", ", usersToMute.Select(u => u.Username));
            foreach (var user in usersToMute)
                try
                {
                    if (muted)
                        await user.AddRoleAsync(mutedRole);
                    else
                        await user.RemoveRoleAsync(mutedRole);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not set mute status on " + user.Username);
                }

            await parameters.SocketMessage.Channel.SendMessageAsync(message);
        }

        private async Task SetStatus(CommandParameters parameters, ActivityType activityType)
        {
            Console.WriteLine("Status set: " + activityType.ToString() + " " + string.Join(" ", parameters.Arguments));
            if (parameters.Arguments.Length == 0) await Task.CompletedTask;
            string game = string.Join(" ", parameters.Arguments);

            if (string.IsNullOrWhiteSpace(game)) return;

            await parameters.Android.Client.SetActivityAsync(new Game(game, activityType));
        }

        [Command]
        public async Task List(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(
                string.Join("\n",
                parameters.Android.Listeners.
                Where(l => l.Channels.Contains(parameters.SocketMessage.Channel.Id) || l.Channels.Contains(Server.Channels.Any)).
                Select(l => l.GetType().Name)));
        }
    }
}
