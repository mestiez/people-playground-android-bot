using Discord.WebSocket;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public abstract class MessageListener
    {
        public virtual ulong Server { get; private set; } = AndroidBot.Server.Channels.Any;
        public virtual ulong User { get; private set; } = AndroidBot.Server.Users.Any;

        public abstract Task OnMessage(SocketMessage arg);
    }

    public class SuggestionListener : MessageListener
    {
        public override ulong Server => AndroidBot.Server.Channels.Suggestions;

        public override Task OnMessage(SocketMessage arg)
        {
            throw new System.NotImplementedException();
        }
    }
}
