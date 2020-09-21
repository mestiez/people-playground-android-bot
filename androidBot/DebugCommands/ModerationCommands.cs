using Discord;
using Discord.Rest;
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

        [Command(aliases: new[] { "christen", "nerd" })]
        public static async Task Tech(CommandParameters parameters)
        {
            await SetRole(parameters.SocketMessage, Server.Roles.TechBan);
        }

        [Command(aliases: new[] { "shame", "unnerd" })]
        public static async Task Untech(CommandParameters parameters)
        {
            await RemoveRole(parameters.SocketMessage, Server.Roles.TechBan);
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

        [Command(aliases: new[] { "banish", "evict", "exile", "deport" })]
        public static async Task Ban(CommandParameters parameters)
        {
            await SetChannelBan(parameters.SocketMessage, true);
        }
        [Command(aliases: new[] { "unbanish", "welcome", "allow", "permit" })]
        public static async Task Unban(CommandParameters parameters)
        {
            await SetChannelBan(parameters.SocketMessage, false);
        }

        private static async Task SetChannelBan(SocketMessage message, bool activeBan)
        {
            if (!message.MentionedUsers.Any())
            {
                await message.Channel.SendMessageAsync(DebugResponseConfiguration.Current.NoUserSpecifiedResponse.PickRandom());
                return;
            }

            ulong[] ignoreRoles = {
                Server.Roles.Muted,
                Server.Roles.Cowboy,
            };

            var channels = message.MentionedChannels;

            foreach (var channel in channels)
            {
                var applicableRoles = channel.PermissionOverwrites.Where(p => p.TargetType == PermissionTarget.Role && !ignoreRoles.Contains(p.TargetId) && p.Permissions.SendMessages == PermValue.Deny);
                if (!applicableRoles.Any())
                {
                    await message.Channel.SendMessageAsync($"no valid roles found for a <#{channel.Id}> ban (｡•́︿•̀｡)");
                    continue;
                }

                await SetRole(message, applicableRoles.First().TargetId, !activeBan);
            }
        }

        private static async Task SetRole(SocketMessage message, ulong roleId, bool removeRole = false)
        {
            var role = Android.Instance.MainGuild.GetRole(roleId);
            var relevantUsers = message.MentionedUsers;
            foreach (SocketGuildUser user in relevantUsers)
            {
                Console.WriteLine("Attempt to update role for " + user.Username);
                if (removeRole)
                {
                    Console.WriteLine("Removed role" + role.Name);
                    await user.RemoveRoleAsync(role);
                }
                else
                {
                    Console.WriteLine("Added role" + role.Name);
                    await user.AddRoleAsync(role);
                }
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

        private static async Task ParseAndMute(CommandParameters parameters, bool muted, TimeSpan duration = default)
        {
            var relevantUsers = parameters.SocketMessage.MentionedUsers;
            if (!relevantUsers.Any())
            {
                await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.NoUserSpecifiedResponse.PickRandom());
                return;
            }

            foreach (SocketGuildUser user in relevantUsers)
            {
                if (muted)
                    await MuteSystem.Mute(user.Id, parameters.SocketMessage.Channel.Id, duration);
                else
                    await MuteSystem.Unmute(user.Id);
            }
        }
    }
}
