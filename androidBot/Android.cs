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
            if (args.Length >= 3)
                ApiKey = args[2];
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
        private bool shouldShutDown = false;

        public static string ApiKey { get; private set; } = null;

        public readonly MessageListener[] ActiveListeners =
        {
            new DebugListener(),
            new ViolationListener(),
            new SuggestionListener(),
            //new CrudeModListener(),
            new UserJoinLeaveListener(),
            //new MessageDeletionListener(),
            new ShareWorkshopListener(),
            new RuleRecallListener(),
            new PinListener(),
        };

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

            ApiKey = ApiKey ?? Environment.GetEnvironmentVariable("ANDROID_STEAM_API", EnvironmentVariableTarget.Machine);

            Client.Log += Log;
            Client.MessageReceived += (arg) => MessageReceived(arg, false);
            Client.MessageUpdated += MessageUpdated;

            await Client.LoginAsync(TokenType.Bot, argToken ?? Environment.GetEnvironmentVariable("ANDROID_TOKEN", EnvironmentVariableTarget.Machine));
            await Client.StartAsync();

            MuteSystem = new MuteSystem(this);
            await MuteSystem.Initialise();

            Listeners.AddRange(ActiveListeners);

            foreach (MessageListener listener in Listeners)
            {
                await listener.Initialise(this);
                Console.WriteLine(listener.GetType().Name + " initialised");
            }

            await Task.Run(() =>
            {
                while (true)
                {
                    if (shouldShutDown && Client.ConnectionState == ConnectionState.Disconnected)
                        break;
                }
            });
        }

        public async Task Shutdown()
        {
            await Client.StopAsync();
            shouldShutDown = true;
        }

        private Task MessageUpdated(Cacheable<IMessage, ulong> messageId, SocketMessage message, ISocketMessageChannel channel)
        {
            MessageReceived(message, true);
            return Task.CompletedTask;
        }

        public T GetListener<T>() where T : MessageListener
        {
            return (T)Listeners.Find(l => l is T);
        }

        private Task MessageReceived(SocketMessage arg, bool edited)
        {
            if (arg.Author.Id == Client.CurrentUser.Id)
                return Task.CompletedTask;

            foreach (MessageListener listener in Listeners)
            {
                var guildUser = MainGuild.GetUser(arg.Author.Id);
                if (guildUser == null) continue;
                if (!Utils.IsAuthorised(listener, arg.Channel.Id, arg.Author.Id, guildUser.Roles)) continue;

                _ = Task.Run(async () => await listener.OnMessage(arg, this, edited));
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
