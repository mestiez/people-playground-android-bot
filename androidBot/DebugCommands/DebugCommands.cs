using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer(roles: new[] { Server.Roles.Administrators, Server.Roles.Developers, Server.Roles.Moderators })]
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

        [Command]
        public static async Task Violations(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(string.Join("\n", parameters.Android.GetListener<ViolationListener>().Violations));
        }

        [Command(default, roles: new[] { Server.Roles.Developers })]
        public static async Task ClearSuggestions(CommandParameters parameters)
        {
            int newMaxSize = 1000;
            if (parameters.Arguments.Length > 0)
                if (int.TryParse(parameters.Arguments[0], out var i))
                    newMaxSize = i;

            var listener = parameters.Android.GetListener<SuggestionListener>();
            int size = listener.Suggestions.Count;
            await listener.GlobalRefresh(parameters.Android, newMaxSize);
            await parameters.SocketMessage.Channel.SendMessageAsync($"cleared {size} suggestions, new list has {listener.Suggestions.Count} entries");
        }

        [Command(new[] { "top", "show top", "show the top", "show me the top" })]
        public static async Task TopSuggestions(CommandParameters parameters)
        {
            const int MaxSuggestionCount = 25;
            if (!int.TryParse(parameters.Arguments[0], out int count)) return;
            Order order = Order.Best;

            switch (parameters.Arguments[1])
            {
                case "worst":
                    order = Order.Worst;
                    if (!parameters.Arguments[2].StartsWith("suggestion")) return;
                    break;
                case "best":
                    order = Order.Best;
                    if (!parameters.Arguments[2].StartsWith("suggestion")) return;
                    break;
                case "suggestions":
                    order = Order.Best;
                    break;
                case "suggestion":
                    order = Order.Best;
                    break;
                default: return;
            }

            if (count <= 0)
            {
                await parameters.SocketMessage.Channel.SendMessageAsync($"{count} isn't a valid amount. it has to be equal or more than 1");
                return;
            }

            var suggestions = parameters.Android.GetListener<SuggestionListener>().Suggestions;

            if (count > Math.Min(suggestions.Count, MaxSuggestionCount))
            {
                count = Math.Min(suggestions.Count, MaxSuggestionCount);
                await parameters.SocketMessage.Channel.SendMessageAsync($"i can only show {MaxSuggestionCount} entries");
            }

            var values = parameters.Android.GetListener<SuggestionListener>().Suggestions.Values;
            IEnumerable<SuggestionListener.Suggestion> topSuggestions;
            if (order == Order.Worst)
                topSuggestions = values.OrderBy(s => s.Score).Take(count);
            else
                topSuggestions = values.OrderByDescending(s => s.Score).Take(count);

            var builder = new EmbedBuilder();
            builder.Color = Color.Teal;
            int index = 0;
            foreach (var suggestion in topSuggestions)
                builder.AddField($"#{index}: {suggestion.Score} points", suggestion.EllipsedContent);
            var embed = builder.Build();

            await parameters.SocketMessage.Channel.SendMessageAsync($"the top {count} {order.ToString().ToLower()} suggestions are", false, embed);
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.ModCleaningAliases), roles: new[] { Server.Roles.Developers })]
        public static async Task CleanMods(CommandParameters parameters)
        {
            CrudeModdingStorage.Current = new CrudeModdingStorage();
            await parameters.Android.GetListener<CrudeModListener>().SaveToDisk();
        }

        private enum Order { Best, Worst }
    }
}
