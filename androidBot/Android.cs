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
        public static void Main()
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
                var guildUser = MainGuild.GetUser(arg.Author.Id);
                if (!IsAuthorised(listener, arg.Channel.Id, arg.Author.Id, guildUser.Roles)) continue;


                await listener.OnMessage(arg, this);
            }
        }

        private bool IsAuthorised(IPermissions permissions, ulong channel, ulong user, IEnumerable<IRole> roles)
        {
            bool c = permissions.Channels.Contains(Server.Channels.Any) || permissions.Channels.Contains(channel);
            bool u = permissions.Users.Contains(Server.Users.Any) || permissions.Users.Contains(user);
            bool r = permissions.Roles.Contains(Server.Roles.Any) || permissions.Roles.Any(i => roles.Any(role => role.Id == i));

            return c && (u || r);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
