using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public class DebugListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Roles => new[] { Server.Roles.Any };
        public override ulong[] Users => new[] { Server.Users.Any };

        private List<string> generatedTriggers = new List<string>();

        private readonly List<CommandReference> commands = new List<CommandReference>();
        private bool isWaitingForCommand;
        private ulong waitingFor;

        public async override Task Initialise()
        {
            await LoadConfiguration();
            LoadCommands();

            LoadTriggers();
            Console.WriteLine(string.Join("\n", generatedTriggers));
            Console.WriteLine("Android is addressable with a total of " + generatedTriggers.Count + " different phrases");
        }

        public async override Task Stop()
        {
            await Task.CompletedTask;
        }

        private async Task LoadConfiguration()
        {
            try
            {
                var rawJson = await File.ReadAllTextAsync(Android.Path + DebugResponseConfiguration.Path);
                DebugResponseConfiguration.Current = JsonConvert.DeserializeObject<DebugResponseConfiguration>(rawJson);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to read debug config file ({e.Message})... can't continue");
                await Task.Delay(-1);
            }
        }

        private void LoadTriggers()
        {
            foreach (var middle in DebugResponseConfiguration.Current.Names)
                foreach (var begin in DebugResponseConfiguration.Current.Prefixes)
                    foreach (var end in DebugResponseConfiguration.Current.Suffixes)
                        generatedTriggers.Add(begin + middle + end);

            generatedTriggers = generatedTriggers.OrderByDescending(s => s.Length).ToList();
        }

        private void LoadCommands()
        {
            var allTypes = GetType().Assembly.GetTypes();

            foreach (var type in allTypes)
            {
                var containerAttributes = type.GetCustomAttributes(typeof(CommandContainerAttribute), true) as CommandContainerAttribute[];
                if (!containerAttributes.Any()) continue;
                var methods = type.GetMethods();

                foreach (var method in methods)
                {
                    var commandAttributes = method.GetCustomAttributes(typeof(CommandAttribute), true) as CommandAttribute[];
                    if (!commandAttributes.Any()) continue;

                    foreach (var cmdA in commandAttributes)
                        cmdA.Initialise();

                    CommandReference reference = new CommandReference();

                    string name = method.Name.ToLower();
                    List<string> aliases = new List<string>(commandAttributes.SelectMany(c => c.Aliases).Append(name));

                    reference.Aliases = aliases.ToArray();
                    reference.Permissions = containerAttributes.Cast<IPermissions>().Concat(commandAttributes).ToArray();
                    reference.Delegate = Delegate.CreateDelegate(typeof(Func<CommandParameters, Task>), method);

                    commands.Add(reference);
                    Console.WriteLine("Command registered: " + name);
                }
            }
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
                    // user just addressed the bot, so their next message is a command
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

                await Execute(contentWithoutPrefix.TrimStart(), arg, android);
            }
        }

        private async Task WaitForNextCommand(SocketMessage arg)
        {
            waitingFor = arg.Author.Id;
            await arg.Channel.SendMessageAsync(DebugResponseConfiguration.Current.TaskAwaitResponses.PickRandom());
            isWaitingForCommand = true;
        }

        private async Task Execute(string contentWithoutPrefix, SocketMessage message, Android android)
        {
            foreach (CommandReference commandReference in commands)
                foreach (string alias in commandReference.Aliases)
                {
                    if (!contentWithoutPrefix.StartsWith(alias)) continue;
                    contentWithoutPrefix = contentWithoutPrefix.Remove(0, alias.Length).Trim();

                    string[] parts = contentWithoutPrefix.Split(' ').Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
                    string[] arguments = parts.ToArray();

                    CommandParameters parameters = new CommandParameters(message, android, arguments);

                    if (!commandReference.IsAuthorised(message.Channel.Id, message.Author.Id, parameters.Android.MainGuild.GetUser(message.Author.Id).Roles)) continue;
                    await (commandReference.Delegate.DynamicInvoke(parameters) as Task);

                    break;
                }

            await Task.CompletedTask;
        }
    }
}
