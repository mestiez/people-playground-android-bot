using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public class PinListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Users => new[] { Server.Users.Any };
        public override ulong[] Roles => new[] { Server.Users.Any };

        private HashSet<ulong> ignoreFurtherEdit = new HashSet<ulong>();

        public override async Task Initialise(Android android)
        {
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage message, Android android)
        {
            if (message.IsPinned && !ignoreFurtherEdit.Contains(message.Id))
            {
                var embed = new EmbedBuilder().
                    WithTitle("Pinned message").
                    WithDescription($"[Jump to message]({message.GetJumpUrl()})").
                    AddField("Channel", message.Channel.Name).
                    AddField("Original content", message.Content).
                    Build();

                await (android.Client.GetChannel(Server.Channels.Log) as ISocketMessageChannel).SendMessageAsync(embed: embed);
                ignoreFurtherEdit.Add(message.Id);
            }
            await Task.CompletedTask;
        }
    }
}
