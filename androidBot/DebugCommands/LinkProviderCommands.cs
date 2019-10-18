using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer]
    public struct LinkProviderCommands
    {
        [ReflectiveCommand(nameof(DebugResponseConfiguration.RoadmapAliases))]
        public static async Task Roadmap(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.RoadmapLink);
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.ChangelogAliases))]
        public static async Task Changelog(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.ChangelogLink);
        }
    }
}
