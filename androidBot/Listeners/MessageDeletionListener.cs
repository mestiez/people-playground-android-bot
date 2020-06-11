using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public class MessageDeletionListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Users => new[] { Server.Users.Any };
        public override ulong[] Roles => new[] { Server.Roles.Any };

        public int BufferSize = 4069;

        public const ulong LogChannel = Server.Channels.Log;

        private Android android;
        private Dictionary<ulong, LightMessage> buffer = new Dictionary<ulong, LightMessage>();

        public override async Task Initialise(Android android)
        {
            this.android = android;
            android.Client.MessageDeleted += Client_MessageDeleted;

            await Task.CompletedTask;
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            if (!buffer.TryGetValue(arg1.Id, out var message))
            {
                Console.WriteLine($"{arg1.Id} not found in buffer({buffer.Count})");
                return;
            }

            var user = android.Client.GetUser(message.Author);
            if (user == null)
            {
                Console.WriteLine($"User {message.Author} not found");
                return;
            }

            string newContent = Regex.Replace(message.Content, "<@(.*?)>", "[REPLACED MENTION]");
            string username = $"{user.Username}({user.Discriminator})";
            await (android.Client.GetChannel(LogChannel) as ISocketMessageChannel).SendMessageAsync($"Message by {username} deleted in {arg2.Name}:\n{newContent}");
            buffer.Remove(arg1.Id);
        }

        public override async Task OnMessage(SocketMessage message, Android android, bool editedMessage)
        {
            Add(message);
            await Task.CompletedTask;
        }

        private void Add(SocketMessage message)
        {
            if (buffer.Count >= BufferSize)
                buffer.Remove(buffer.OrderBy(b => b.Value.Timestamp).First().Key);

            buffer.Add(message.Id, new LightMessage()
            {
                Author = message.Author.Id,
                Content = message.Content,
                Timestamp = message.Timestamp.Millisecond
            });
        }

        public List<LightMessage> GetBuffer() => buffer.Values.ToList();

        public struct LightMessage
        {
            public string Content;
            public ulong Author;
            public int Timestamp;

            public override string ToString()
            {
                return $"{Author}: {Content}";
            }
        }
    }
}
