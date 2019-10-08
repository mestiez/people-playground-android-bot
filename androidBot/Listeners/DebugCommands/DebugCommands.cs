using System;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer(roles: new[] { Server.Roles.Developers })]
    public struct DebugCommands
    {
        [Command]
        public static async Task Ping(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("pong");
        }

        [Command]
        public static async Task List(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(
                string.Join("\n",
                parameters.Android.Listeners.
                Where(l => l.Channels.Contains(parameters.SocketMessage.Channel.Id) || l.Channels.Contains(Server.Channels.Any)).
                Select(l => l.GetType().Name)));
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.ModCleaningAliases))]
        public static async Task CleanMods(CommandParameters parameters)
        {
            CrudeModdingStorage.Current = new CrudeModdingStorage();
            await parameters.Android.GetListener<CrudeModListener>().SaveToDisk();
        }
    }
}
