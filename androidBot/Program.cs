using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace androidBot
{
    public class ActualBot
    {
        public static void Main(string[] args)
            => new ActualBot().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();

            client.Log += Log;
            client.MessageReceived += MessageReceived;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("android-token", EnvironmentVariableTarget.Machine));
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            Console.WriteLine("Message received");
            switch (arg.Channel.Id)
            {
                // #suggestions
                case 605732562492456970:
                    string content = arg.Content;
                    if (!content.Trim().ToLower().StartsWith("suggestion:")) break; //yep this sure is a suggestion

                    Console.WriteLine("Suggestion found: " + content);

                    RestUserMessage restMessage = (RestUserMessage)await arg.Channel.GetMessageAsync(arg.Id);
                    await restMessage.AddReactionsAsync(new IEmote[]
                    {
                        Emote.Parse("<:YES:604730173379706888>"),
                        Emote.Parse("<:NO:604730173236969472>")
                    }, RequestOptions.Default);

                    break;
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
