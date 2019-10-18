using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public struct MentionSpamViolation : IViolation
    {
        public int MentionThreshold;
        public int MuteDurationInHours;

        public MentionSpamViolation(int mentionThreshold, int muteDurationInHours)
        {
            MentionThreshold = mentionThreshold;
            MuteDurationInHours = muteDurationInHours;
        }

        public async Task Consequence(SocketMessage message, Android android)
        {
            await message.Channel.SendMessageAsync($"mentioning over {MentionThreshold} users in one message is against the rules, you will be muted for {MuteDurationInHours} hours");
            MuteManager.Mute(message.Author.Id, message.Channel.Id, TimeSpan.FromHours(MuteDurationInHours));
        }

        public bool Violates(SocketMessage message, Android android)
        {
            return message.MentionedUsers.Count > MentionThreshold;
        }
    }
}
