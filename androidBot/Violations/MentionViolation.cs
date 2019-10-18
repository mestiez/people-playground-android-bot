using Discord.WebSocket;
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
}
