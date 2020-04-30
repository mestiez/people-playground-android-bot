using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer(roles: new[] { Server.Roles.Administrators, Server.Roles.TrialMods, Server.Roles.Moderators, Server.Roles.Developers })]
    public struct ModerationCommands
    {
        [Command(aliases: new[] { "shadow ban", "shadowban", "ian", "limit", "handicap", })]
        public static async Task Lock(CommandParameters parameters)
        {
            await SetRole(parameters.SocketMessage, Server.Roles.Clown);
        }

        [Command(aliases: new[] { "pardon", "unshadowban", "un-shadowban", "grace", "forgive" })]
        public static async Task Unlock(CommandParameters parameters)
        {
            await RemoveRole(parameters.SocketMessage, Server.Roles.Clown);
        }

        [Command]
        public static async Task Interrogate(CommandParameters parameters)
        {
            await SetRole(parameters.SocketMessage, Server.Roles.Cowboy);
        }

        [Command]
        public static async Task Release(CommandParameters parameters)
        {
            await RemoveRole(parameters.SocketMessage, Server.Roles.Cowboy);
        }

        private static async Task SetRole(SocketMessage message, ulong roleId, bool removeRole = false)
        {
            var matches = Utils.GetUserCodesFromText(message.Content);
            if (!matches.Any())
            {
                await message.Channel.SendMessageAsync(DebugResponseConfiguration.Current.NoUserSpecifiedResponse.PickRandom());
                return;
            }

            List<SocketGuildUser> relevantUsers = new List<SocketGuildUser>();
            foreach (Match match in matches)
            {
                string toParse = new string(match.ToString().Where(c => char.IsDigit(c)).ToArray());
                if (!ulong.TryParse(toParse, out ulong result)) continue;
                try
                {
                    relevantUsers.Add(Android.Instance.MainGuild.GetUser(result));
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot retrieve user with id " + result);
                }
            }
            var role = Android.Instance.MainGuild.GetRole(roleId);
            foreach (var user in relevantUsers)
            {
                if (removeRole)
                    await user.RemoveRoleAsync(role);
                else
                    await user.AddRoleAsync(role);
            }
        }

        private static async Task RemoveRole(SocketMessage message, ulong roleId) => await SetRole(message, roleId, true);

        [Command]
        public static async Task Mute(CommandParameters parameters)
        {
            TimeSpan duration = TimeSpan.FromMinutes(15);

            var match = Regex.Match(parameters.SocketMessage.Content, @"(for)\s(\d+)\s*(\w*\b)");
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
                    else if (match.Value.Contains("day"))
                        duration = TimeSpan.FromDays(parsedNumber);
                    else if (match.Value.Contains("week"))
                        duration = TimeSpan.FromDays(parsedNumber * 7);
                    else
                    {
                        duration = TimeSpan.FromMinutes(parsedNumber);
                        await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.MinuteUnitFallbackResponse.PickRandom());
                    }
                }
                catch (Exception)
                {
                    await parameters.SocketMessage.Channel.SendMessageAsync(string.Format(DebugResponseConfiguration.Current.FifteenMinuteFallbackResponse.PickRandom(), match.Value));
                }
            }
            else
            {
                await parameters.SocketMessage.Channel.SendMessageAsync("no duration specified, falling back to 15 minutes");
            }

            await ParseAndMute(parameters, true, duration);
        }

        [Command]
        public static async Task Silence(CommandParameters parameters)
        {
            await Mute(parameters);
        }

        [Command]
        public static async Task Unmute(CommandParameters parameters)
        {
            await ParseAndMute(parameters, false);
        }

        //public static async Task Ban(CommandParameters parameters)
        //{
        //    var matches = Utils.GetUserCodesFromText(parameters.SocketMessage.Content);
        //}

        private static async Task ParseAndMute(CommandParameters parameters, bool muted, TimeSpan duration = default)
        {
            var matches = Utils.GetUserCodesFromText(parameters.SocketMessage.Content);
            if (!matches.Any())
            {
                await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.NoUserSpecifiedResponse.PickRandom());
                return;
            }

            List<SocketGuildUser> relevantUsers = new List<SocketGuildUser>();

            foreach (Match match in matches)
            {
                string toParse = new string(match.ToString().Where(c => char.IsDigit(c)).ToArray());
                if (!ulong.TryParse(toParse, out ulong result)) continue;
                try
                {
                    relevantUsers.Add(parameters.Android.MainGuild.GetUser(result));
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot retrieve user with id " + result);
                }
            }

            foreach (var user in relevantUsers)
            {
                if (muted)
                    await MuteSystem.Mute(user.Id, parameters.SocketMessage.Channel.Id, duration);
                else
                    await MuteSystem.Unmute(user.Id);
            }
        }
    }
}
