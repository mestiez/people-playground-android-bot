using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public class SuggestionListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Suggestions };
        public override ulong[] Roles => new[] { Server.Roles.Any };
        public override ulong[] Users => new[] { Server.Users.Any };

        public override async Task Initialise()
        {
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            await CheckNewSuggestion(arg);
        }

        public override async Task Stop()
        {
            await Task.CompletedTask;
        }

        private async Task CheckNewSuggestion(SocketMessage arg)
        {
            string content = arg.Content;
            if (!content.Trim().ToLower().StartsWith("suggestion")) return;

            Console.WriteLine("Suggestion found: " + content);

            RestUserMessage restMessage = (RestUserMessage)await arg.Channel.GetMessageAsync(arg.Id);
            await restMessage.AddReactionsAsync(new IEmote[]
            {
                AndroidBot.Server.Emotes.YES,
                AndroidBot.Server.Emotes.NO,
            }, RequestOptions.Default);
        }
    }
}
