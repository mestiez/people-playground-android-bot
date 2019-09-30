using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public partial class DebugListener : MessageListener
    {
        public override ulong[] SpecificUsers => new[] { Server.Users.Mestiez, Server.Users.Vila, Server.Users.Vincent, Server.Users.JoeLouis, Server.Users.Besm };

        public readonly string[] Prefixes = {
            "android",
            "android!",
            "android!!",
            "android!!!",
            "android.",
            "android...",
            "android,",

            "bot",
            "bot!",
            "bot!!",
            "bot!!!",
            "bot.",
            "bot...",
            "bot,",

            "robot",
            "robot!",
            "robot!!",
            "robot!!!",
            "robot.",
            "robot...",
            "robot," };

        private readonly Dictionary<string, Delegate> commands = new Dictionary<string, Delegate>();
        private bool isWaitingForCommand;
        private ulong waitingFor;

        public async override Task Initialise()
        {
            Console.WriteLine(GetType().Name + " initialised");

            var type = GetType();
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                if (!method.GetCustomAttributes(typeof(CommandAttribute), true).Any()) continue;

                string name = method.Name.ToLower();
                commands.Add(name, Delegate.CreateDelegate(typeof(Func<CommandParameters, Task>), this, method));
                Console.WriteLine("Command registered: " + name);
            }

            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            string content = arg.Content.ToLower().Trim().Normalize();

            if (isWaitingForCommand && waitingFor == arg.Author.Id)
            {
                isWaitingForCommand = false;
                await handleCommand(content);
                return;
            }

            foreach (string prefix in Prefixes.OrderByDescending(s => s.Length))
            {
                if (!content.StartsWith(prefix)) continue;
                content = content.Remove(0, prefix.Length);
                if (content.Trim().Length == 0)
                {
                    // user just addressed the bot, so their next message is a command unless otherwise is specified
                    Console.WriteLine("WAITING FOR COMMAND...");
                    await WaitForNextCommand(arg, android);
                    return;
                }
                await handleCommand(content);
                return;
            }

            async Task handleCommand(string contentWithoutPrefix)
            {
                if (content.Trim().Length == 0)
                {
                    Console.WriteLine("Empty command...");
                    return;
                }
                string[] parts = content.Split(' ').Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
                string command = parts[0];
                string[] arguments = parts.Skip(1).ToArray();
                await Execute(command, new CommandParameters(arg, android, arguments));
            }
        }

        private async Task WaitForNextCommand(SocketMessage arg, Android android)
        {
            waitingFor = arg.Author.Id;
            string[] responses = { "what is up", "?", "??", "what", "yes", "hm?", "yes sir", "AT YOUR SERVICE", "present", "何" };
            await arg.Channel.SendMessageAsync(responses.PickRandom());
            isWaitingForCommand = true;
        }

        private async Task Execute(string command, CommandParameters parameters)
        {
            if (commands.TryGetValue(command, out Delegate func))
                await (func.DynamicInvoke(parameters) as Task);

            await Task.CompletedTask;
        }

        public class CommandAttribute : Attribute { }

        public struct CommandParameters
        {
            public SocketMessage SocketMessage;
            public Android Android;
            public string[] Arguments;

            public CommandParameters(SocketMessage socketMessage, Android android, string[] arguments)
            {
                SocketMessage = socketMessage;
                Android = android;
                Arguments = arguments;
            }
        }
    }
}
