using AndroidBot.Listeners;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AndroidBot
{
    public class Android
    {
        public static void Main(string[] args)
        {
            var android = new Android();
            android.MainAsync().GetAwaiter().GetResult();
        }

        public readonly DiscordSocketClient Client = new DiscordSocketClient();
        public readonly List<MessageListener> Listeners = new List<MessageListener>();

        public SocketGuild MainGuild => Client.GetGuild(603649973510340619);
        public static string Path { get; private set; }
        public static MuteManager MuteManager { get; private set; }

        public async Task MainAsync()
        {
            Path = Environment.GetEnvironmentVariable("ANDROID_STORAGE", EnvironmentVariableTarget.Machine);
            if (Path == null)
            {
                Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\android_bot\\";
                Console.WriteLine("%ANDROID_STORAGE% has not been set, falling back to " + Path);
            }
            if (!Path.EndsWith("\\"))
            {
                Console.WriteLine("%ANDROID_STORAGE% does not end with a backslash");
                Path += "\\";
            }


            Client.Log += Log;
            Client.MessageReceived += MessageReceived;

            await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("android-token", EnvironmentVariableTarget.Machine));
            await Client.StartAsync();

            MuteManager = new MuteManager(this);
            await MuteManager.Initialise();

            Listeners.Add(new DebugListener());
            Listeners.Add(new ViolationListener());
            Listeners.Add(new SuggestionListener());
            Listeners.Add(new CrudeModListener());

            foreach (MessageListener listener in Listeners)
            {
                await listener.Initialise(this);
                Console.WriteLine(listener.GetType().Name + " initialised");
            }

            await Task.Delay(-1);
        }

        public T GetListener<T>() where T : MessageListener
        {
            return (T)Listeners.Find(l => l is T);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id == Client.CurrentUser.Id)
                return;

            foreach (MessageListener listener in Listeners)
            {
                var guildUser = MainGuild.GetUser(arg.Author.Id);
                if (!Utils.IsAuthorised(listener, arg.Channel.Id, arg.Author.Id, guildUser.Roles)) continue;

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
