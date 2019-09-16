using Discord.WebSocket;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public abstract class MessageListener
    {
        public virtual ulong[] SpecificChannels { get; private set; } = { AndroidBot.Server.Channels.Any };
        public virtual ulong[] SpecificUsers { get; private set; } = { AndroidBot.Server.Users.Any };

        public abstract Task Initialise();
        public abstract Task OnMessage(SocketMessage arg, Android android);
    }
}
