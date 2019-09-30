using AndroidBot.Listeners;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot
{
    public class Android
    {
        public static void Main(string[] args)
            => new Android().MainAsync().GetAwaiter().GetResult();

        public readonly DiscordSocketClient Client = new DiscordSocketClient();
        public readonly List<MessageListener> Listeners = new List<MessageListener>();

        public SocketGuild MainGuild => Client.GetGuild(603649973510340619);

        public async Task MainAsync()
        {
            Client.Log += Log;
            Client.MessageReceived += MessageReceived;

            await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("android-token", EnvironmentVariableTarget.Machine));
            await Client.StartAsync();
            Console.WriteLine(MainGuild);
            Listeners.Add(new DebugListener());
            Listeners.Add(new SuggestionListener());

            foreach (MessageListener listener in Listeners)
                await listener.Initialise();


            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            Console.WriteLine("Message received");

            foreach (MessageListener listener in Listeners)
            {
                if (!listener.SpecificChannels.Contains(Server.Channels.Any))
                    if (!listener.SpecificChannels.Contains(arg.Channel.Id)) continue;

                if (!listener.SpecificUsers.Contains(Server.Users.Any))
                    if (!listener.SpecificUsers.Contains(arg.Author.Id)) continue;

                await listener.OnMessage(arg, this);
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
