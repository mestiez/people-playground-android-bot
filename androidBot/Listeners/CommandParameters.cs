using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public partial class DebugListener
    {
        public struct CommandParameters
        {
            public SocketMessage SocketMessage;
            public Android Android;
            public string[] Arguments;

            public CommandParameters(SocketMessage socketMessage, Android android, string[] arguments)
            {
                SocketMessage = socketMessage;
                Android = android;
                Arguments = arguments;
            }
        }
    }
}
