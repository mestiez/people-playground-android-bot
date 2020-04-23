using AndroidBot.Listeners;
using Discord;
using Discord.WebSocket;
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
            if (args.Length >= 1)
                setStorage = args[0];
            if (args.Length >= 2)
                argToken = args[1];
            Instance = new Android();
            Instance.MainAsync().GetAwaiter().GetResult();
        }

        public static Android Instance { private set; get; }
        public readonly DiscordSocketClient Client = new DiscordSocketClient();
        public readonly List<MessageListener> Listeners = new List<MessageListener>();

        public SocketGuild MainGuild => Client.GetGuild(603649973510340619);
        public static string Path { get; private set; }
        public static MuteSystem MuteSystem { get; private set; }

        private static string setStorage = null;
        private static string argToken = null;

        public async Task MainAsync()
        {
            Path = setStorage ?? Environment.GetEnvironmentVariable("ANDROID_STORAGE", EnvironmentVariableTarget.Machine);
            if (Path == null)
            {
                Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/android_bot/";
                Console.WriteLine("%ANDROID_STORAGE% has not been set, falling back to " + Path);
            }
            if (!Path.EndsWith("/"))
            {
                Console.WriteLine("%ANDROID_STORAGE% does not end with a backslash");
                Path += "/";
            }

            Client.Log += Log;
            Client.MessageReceived += MessageReceived;
            Client.MessageUpdated += MessageUpdated;

            await Client.LoginAsync(TokenType.Bot, argToken ?? Environment.GetEnvironmentVariable("ANDROID_TOKEN", EnvironmentVariableTarget.Machine));
            await Client.StartAsync();

            MuteSystem = new MuteSystem(this);
            await MuteSystem.Initialise();

            Listeners.Add(new DebugListener());
            Listeners.Add(new ViolationListener());
            Listeners.Add(new SuggestionListener());
            //Listeners.Add(new CrudeModListener());
            Listeners.Add(new UserJoinLeaveListener());
            Listeners.Add(new MessageDeletionListener());
            Listeners.Add(new ShareWorkshopListener());
            Listeners.Add(new RuleRecallListener());

            foreach (MessageListener listener in Listeners)
            {
                await listener.Initialise(this);
                Console.WriteLine(listener.GetType().Name + " initialised");
            }

            await Task.Delay(-1);
        }

        private Task MessageUpdated(Cacheable<IMessage, ulong> messageId, SocketMessage message, ISocketMessageChannel channel)
        {
            MessageReceived(message);
            return Task.CompletedTask;
        }

        public T GetListener<T>() where T : MessageListener
        {
            return (T)Listeners.Find(l => l is T);
        }

        private Task MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id == Client.CurrentUser.Id)
                return Task.CompletedTask;

            foreach (MessageListener listener in Listeners)
            {
                var guildUser = MainGuild.GetUser(arg.Author.Id);
                if (guildUser == null) continue;
                if (!Utils.IsAuthorised(listener, arg.Channel.Id, arg.Author.Id, guildUser.Roles)) continue;

                _ = Task.Run(async () => await listener.OnMessage(arg, this));
            }
            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
