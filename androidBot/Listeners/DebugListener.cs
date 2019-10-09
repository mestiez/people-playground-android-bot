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
        private Dictionary<ulong, DateTime> waitingForMap = new Dictionary<ulong, DateTime>();

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

            if (waitingForMap.TryGetValue(arg.Author.Id, out var date))
            {
                if ((DateTime.Now - date).TotalSeconds <= DebugResponseConfiguration.Current.MaxWaitingTimeInSeconds)
                {
                    waitingForMap.Remove(arg.Author.Id);
                    await handleCommand(content);
                    return;
                }
                else
                    waitingForMap.Remove(arg.Author.Id);
            }

            foreach (string trigger in generatedTriggers)
            {
                if (!content.StartsWith(trigger)) continue;

                content = content.Remove(0, trigger.Length).Trim();

                if (content.Length == 0)
                {
                    if (waitingForMap.ContainsKey(arg.Author.Id))
                        waitingForMap[arg.Author.Id] = DateTime.Now;
                    else
                        waitingForMap.Add(arg.Author.Id, DateTime.Now);

                    await arg.Channel.SendMessageAsync(DebugResponseConfiguration.Current.TaskAwaitResponses.PickRandom());
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

                await Execute(contentWithoutPrefix, arg, android);
            }
        }

        private async Task Execute(string content, SocketMessage message, Android android)
        {
            foreach (CommandReference commandReference in commands)
                foreach (string alias in commandReference.Aliases)
                {
                    if (!content.StartsWith(alias)) continue;
                    content = content.Remove(0, alias.Length).Trim();

                    string[] parts = content.Split(' ').Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
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
