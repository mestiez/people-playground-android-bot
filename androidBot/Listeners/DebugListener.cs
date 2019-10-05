using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public partial class DebugListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Roles => new[] { Server.Roles.Moderators, Server.Roles.Developers, Server.Roles.Bots };

        public readonly string[] Prefixes = {
            "",
            "hey ",
            "ay ",
            "oy ",
            "yo ",
            "okay ",
            "oi ",
            "ok ",
            "mr ",
            "mr.",
            "mr. ",
            "mister ",
            "...",
            "lmao ",
            "lol ",
            "our ",
            "the ",
            "my ",
        };

        public readonly string[] Suffixes = {
            "",
            ".",
            "..",
            "...",
            ",",
            "?",
            "??",
            "???",
            "!",
            "!!",
            "!!!",
            "san",
            "chan",
            "kun",
            " san",
            " chan",
            " kun",
            "-san",
            "-chan",
            "-kun",
            "さん",
            "ちゃん",
            "くん",
        };

        public readonly string[] Names = {
            "android",
            "bot",
            "droid",
            "biscuit",
            "biscuit #1",
            "biscuit #2",
            "biscuit 1",
            "biscuit 2",
            "robotboy",
            "r2",
            "r2d2",
            "computer",
            "slave",
            "c3po",
            "3po",
            "xj9",
            "nano",
            "robot",
            "ロボット",
        };

        private List<string> generatedTriggers = new List<string>();

        private readonly List<CommandReference> commands = new List<CommandReference>();
        private bool isWaitingForCommand;
        private ulong waitingFor;

        public async override Task Initialise()
        {
            LoadConfig();

            LoadCommands();

            LoadTriggers();
            Console.WriteLine(string.Join("\n", generatedTriggers));
            Console.WriteLine("Android is addressable with a total of " + generatedTriggers.Count + " different phrases");
            Console.WriteLine(GetType().Name + " initialised");
            await Task.CompletedTask;
        }

        private void LoadTriggers()
        {
            foreach (var middle in Names)
                foreach (var begin in Prefixes)
                    foreach (var end in Suffixes)
                        generatedTriggers.Add(begin + middle + end);

            generatedTriggers = generatedTriggers.OrderByDescending(s => s.Length).ToList();
        }

        private void LoadCommands()
        {
            var type = GetType();
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                var commandAttributes = method.GetCustomAttributes(typeof(CommandAttribute), true) as CommandAttribute[];
                if (!commandAttributes.Any()) continue;

                CommandReference reference = new CommandReference();

                string name = method.Name.ToLower();
                List<string> aliases = new List<string>(commandAttributes.SelectMany(c => c.Aliases).Append(name));

                reference.Aliases = aliases.ToArray();
                reference.Permissions = commandAttributes;
                reference.Delegate = Delegate.CreateDelegate(typeof(Func<CommandParameters, Task>), this, method);

                commands.Add(reference);
                Console.WriteLine("Command registered: " + name);
            }
        }

        private void LoadConfig()
        {
            throw new NotImplementedException();
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

            foreach (string trigger in generatedTriggers)
            {
                if (!content.StartsWith(trigger)) continue;

                content = content.Remove(0, trigger.Length);
                if (content.Trim().Length == 0)
                {
                    // user just addressed the bot, so their next message is a command unless otherwise is specified
                    Console.WriteLine("Waiting for command...");
                    await WaitForNextCommand(arg);
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

        private async Task WaitForNextCommand(SocketMessage arg)
        {
            waitingFor = arg.Author.Id;
            string[] responses = { "what is up", "?", "??", "what", "yes", "hm?", "yes sir", "AT YOUR SERVICE", "present", "何" };
            await arg.Channel.SendMessageAsync(responses.PickRandom());
            isWaitingForCommand = true;
        }

        private async Task Execute(string command, CommandParameters parameters)
        {
            foreach (CommandReference commandReference in commands)
            {
                if (!commandReference.Aliases.Contains(command)) continue;

                var m = parameters.SocketMessage;
                if (!commandReference.IsAuthorised(m.Channel.Id, m.Author.Id, parameters.Android.MainGuild.GetUser(m.Author.Id).Roles)) continue;

                await (commandReference.Delegate.DynamicInvoke(parameters) as Task);
            }

            await Task.CompletedTask;
        }
    }
}
