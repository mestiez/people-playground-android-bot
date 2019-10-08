using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
    public struct ModerationCommands
    {
        [Command]
        public static async Task Mute(CommandParameters parameters)
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
                        await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.MinuteUnitFallbackResponse.PickRandom());
                        await Task.Delay(TimeSpan.FromSeconds(0.5f));
                    }
                }
                catch (Exception)
                {
                    await parameters.SocketMessage.Channel.SendMessageAsync(string.Format(DebugResponseConfiguration.Current.FifteenMinuteFallbackResponse.PickRandom(), match.Value));
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
        public static async Task Silence(CommandParameters parameters)
        {
            await Mute(parameters);
        }

        [Command]
        public static async Task Unmute(CommandParameters parameters)
        {
            await SetMuteStatus(parameters, false);
        }

        private static async Task SetMuteStatus(CommandParameters parameters, bool muted)
        {
            var mutedRole = parameters.Android.MainGuild.GetRole(Server.Roles.Muted);
            var matches = Regex.Matches(parameters.SocketMessage.Content, "<@(.*?)>");
            if (!matches.Any())
            {
                await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.NoUserSpecifiedResponse.PickRandom());
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

            string message = (muted ? DebugResponseConfiguration.Current.MutingNotification.PickRandom() : DebugResponseConfiguration.Current.UnmutingNotification.PickRandom()) + string.Join(", ", usersToMute.Select(u => u.Username));
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
    }
}
