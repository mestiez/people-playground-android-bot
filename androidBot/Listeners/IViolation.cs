using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace AndroidBot.Listeners
{
    public interface IViolation
    {
        bool Violates(SocketMessage message, Android android);
    }

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
    }
}
