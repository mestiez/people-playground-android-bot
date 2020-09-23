using AndroidBot.Listeners;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

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
            if (args.Length >= 4)
                WsIP = args[3];

            Instance = new Android();
            Console.WriteLine(WsIP);
            Instance.WebsocketServer = new WatsonWsServer(WsIP ?? "127.0.0.1", 4050, false);
            Instance.WebsocketServer.ServerStopped += (o,e) => { Console.WriteLine("WS SERVER STOPPED"); };
            Instance.WebsocketServer.ClientConnected += async (o, e) => await Instance.WebsocketServer_ClientConnected(o, e);
            Instance.WebsocketServer.ClientDisconnected += async (o, e) => await Instance.WebsocketServer_ClientDisconnected(o, e);
            Instance.WebsocketServer.MessageReceived += async (o, e) => await Instance.WebsocketServer_MessageReceived(o, e);

            Instance.MainAsync().GetAwaiter().GetResult();
        }

        public static Android Instance { private set; get; }
        public readonly DiscordSocketClient Client = new DiscordSocketClient();
        public readonly List<MessageListener> Listeners = new List<MessageListener>();

        public WatsonWsServer WebsocketServer;
        private WebsocketResponder responder;

        public SocketGuild MainGuild => Client.GetGuild(603649973510340619);
        public static string Path { get; private set; }
        public static MuteSystem MuteSystem { get; private set; }

        private static string setStorage = null;
        private static string argToken = null;
        private static string WsIP;

        private bool shouldShutDown = false;

        public static string ApiKey { get; private set; } = null;
        public string DiscordToken => argToken ?? Environment.GetEnvironmentVariable("ANDROID_TOKEN", EnvironmentVariableTarget.Machine);

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
            responder = new WebsocketResponder(this);

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

            await Client.LoginAsync(TokenType.Bot, DiscordToken);
            await Client.StartAsync();

            MuteSystem = new MuteSystem(this);
            await MuteSystem.Initialise();

            Listeners.AddRange(ActiveListeners);

            foreach (MessageListener listener in Listeners)
            {
                await listener.Initialise(this);
                Console.WriteLine(listener.GetType().Name + " initialised");
            }

            WebsocketServer.Start();

            await Task.Run(() =>
            {
                while (true)
                {
                    if (shouldShutDown && Client.ConnectionState == ConnectionState.Disconnected)
                        break;
                }
            });
        }

        private async Task WebsocketServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (Client.ConnectionState != ConnectionState.Connected) await Task.CompletedTask;

            try
            {
                await responder.Execute(e.Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task WebsocketServer_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (Client.ConnectionState != ConnectionState.Connected) await Task.CompletedTask;

            Console.WriteLine("WS REMOTE DISCONNECTED: " + e.IpPort);
        }

        private async Task WebsocketServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if (Client.ConnectionState != ConnectionState.Connected) await Task.CompletedTask;

            Console.WriteLine("WS REMOTE CONNECTED: " + e.IpPort);

            var channelDict = new Dictionary<string, string>();
            var channels = MainGuild.TextChannels.ToList().OrderBy(c => c.Position);

            foreach (var channel in channels)
                channelDict.Add(channel.Name, channel.Id.ToString());

            await WebsocketServer.SendAsync(e.IpPort, JsonConvert.SerializeObject(channelDict));
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

    public class WebsocketResponder
    {
        public Android Android { get; }

        public WebsocketResponder(Android android)
        {
            Android = android;
        }

        public async Task Execute(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var message = JsonConvert.DeserializeObject<WebsocketMessage>(json);

            if (message.Token != Android.DiscordToken)
            {
                Console.WriteLine("invalid token");
                await Task.CompletedTask;
                return;
            }

            if (!ulong.TryParse(message.Channel, out var channelID))
            {
                Console.WriteLine("websocket message with invalid channel ID received");
                await Task.CompletedTask;
                return;
            }

            var channel = Android.MainGuild.GetTextChannel(channelID);
            if (channel == null)
            {
                Console.WriteLine("websocket message with invalid channel ID received");
                await Task.CompletedTask;
                return;
            }

            var decodedContent = System.Convert.FromBase64String(message.ContentBase64);
            string content = Encoding.UTF8.GetString(decodedContent);
            Console.WriteLine($"I am about to send a message to #{channel.Name} with the text: \n{content}\n\n");

            await channel.SendMessageAsync(content);
        }

        private struct WebsocketMessage
        {
            public string Token;
            public string Channel;
            public string ContentBase64;
        }
    }
}
