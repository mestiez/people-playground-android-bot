using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public struct MentionSpamViolation : IViolation
    {
        public int MentionThreshold;
        public int MuteDurationInDays;

        public MentionSpamViolation(int mentionThreshold, int muteDurationInDays)
        {
            MentionThreshold = mentionThreshold;
            MuteDurationInDays = muteDurationInDays;
        }

        public async Task Consequence(SocketMessage message, Android android)
        {
            await message.Channel.SendMessageAsync($"mentioning over {MentionThreshold} users in one message is against the rules\nyou will be muted for {MuteDurationInDays} days");
            await MuteSystem.Mute(message.Author.Id, message.Channel.Id, TimeSpan.FromDays(MuteDurationInDays));
        }

        public bool Violates(SocketMessage message, Android android)
        {
            return message.MentionedUsers.Count > MentionThreshold;
        }
    }
}
