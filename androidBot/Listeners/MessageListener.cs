using Discord.WebSocket;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public abstract class MessageListener : IPermissions
    {
        public virtual ulong[] Roles { get; private set; } = {  };
        public virtual ulong[] Users { get; private set; } = {  };
        public virtual ulong[] Channels { get; private set; } = {  };

        public abstract Task Initialise();
        public abstract Task OnMessage(SocketMessage arg, Android android);
    }
}
