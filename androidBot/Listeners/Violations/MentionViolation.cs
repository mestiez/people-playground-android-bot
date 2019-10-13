using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public struct MentionViolation : IViolation
    {
        public IEnumerable<ulong> Users;

        public MentionViolation(params ulong[] users)
        {
            Users = users;
        }

        public bool Violates(SocketMessage message, Android android)
        {
            return Users.Intersect(message.MentionedUsers.Select(u => u.Id)).Any();
        }

        public async Task Consequence(SocketMessage message, Android android)
        {
            //no consequences except logging
            await Task.CompletedTask;
        }
    }

    public struct MentionSpamViolation : IViolation
    {
        public int MentionThreshold;
        public int MuteDurationInMinutes;

        public MentionSpamViolation(int mentionThreshold, int muteDurationInMinutes)
        {
            MentionThreshold = mentionThreshold;
            MuteDurationInMinutes = muteDurationInMinutes;
        }

        public async Task Consequence(SocketMessage message, Android android)
        {
            int timespan = MuteDurationInMinutes;
            await message.Channel.SendMessageAsync($"mentioning over {MentionThreshold} users in one message is against the rules, you will be muted for {MuteDurationInMinutes} minutes");
            var mutedRole = android.MainGuild.GetRole(Server.Roles.Muted);
            SocketGuildUser toMute = android.MainGuild.GetUser(message.Author.Id);

            await toMute.AddRoleAsync(mutedRole);
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(timespan));
                await message.Channel.SendMessageAsync($"unmuting {toMute.Username}");
                await toMute.RemoveRoleAsync(mutedRole);
            });
        }

        public bool Violates(SocketMessage message, Android android)
        {
            return message.MentionedUsers.Count > MentionThreshold;
        }
    }
}
