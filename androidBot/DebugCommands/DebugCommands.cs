using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer(roles: new[] { Server.Roles.Administrators, Server.Roles.TrialMods, Server.Roles.Developers, Server.Roles.Moderators })]
    public struct DebugCommands
    {
        [Command(roles: new[] { Server.Roles.Developers })]
        public static async Task Quit(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("goodbye");
            await parameters.Android.Shutdown();
        }

        [Command]
        public static async Task Compare(CommandParameters parameters)
        {
            var levenshtein = new F23.StringSimilarity.NormalizedLevenshtein();
            var adverbs = new HashSet<string>(await File.ReadAllLinesAsync("all-adverbs.txt"));

            var A = GetSignificantContent(parameters.Arguments[0]);
            var B = GetSignificantContent(parameters.Arguments[1]);

            var duplicate = (A == B) || levenshtein.Distance(A, B) < .25f;

            await parameters.SocketMessage.Channel.SendMessageAsync($"{A}≈{B}={duplicate}");

            string GetSignificantContent(string content)
            {
                string lower = " " + content.Normalize().ToLower() + " ";

                foreach (var adverb in adverbs)
                {
                    lower = lower.Replace(" " + adverb + " ", " ");
                }

                return lower.Trim();
            }
        }

        [Command]
        public static async Task Ping(CommandParameters parameters)
        {
            var thenTicks = parameters.SocketMessage.Timestamp.UtcTicks;
            var nowTicks = DateTimeOffset.UtcNow.Ticks;
            var distance = TimeSpan.FromTicks(nowTicks - thenTicks);
            await parameters.SocketMessage.Channel.SendMessageAsync("pong (" + Math.Round(distance.TotalMilliseconds, 3) + "ms)");
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

        [Command(aliases: new[] { "print message buffer", "mbuffer" })]
        public static async Task PrintMessageBuffer(CommandParameters parameters)
        {
            string message = string.Join("\n", parameters.Android.GetListener<MessageDeletionListener>().GetBuffer());
            ASCIIEncoding encoder = new ASCIIEncoding();
            var bytes = encoder.GetBytes(message);
            MemoryStream stream = new MemoryStream(bytes);
            await parameters.SocketMessage.Channel.SendFileAsync(stream, "message buffer.txt");
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
            await parameters.SocketMessage.Channel.SendMessageAsync($"going through {size} entries...");
            await listener.GlobalRefresh(parameters.Android, newMaxSize);
            await parameters.SocketMessage.Channel.SendMessageAsync($"cleared {size} suggestions, new list has {listener.Suggestions.Count} entries");
        }

        [Command(default, roles: new[] { Server.Roles.Developers })]
        public static async Task SuggestionPurge(CommandParameters parameters)
        {
            await parameters.Android.GetListener<SuggestionListener>().ResetPeriodicBoard();
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

            var values = suggestions.Values;
            IEnumerable<SuggestionListener.Suggestion> topSuggestions;
            if (order == Order.Worst)
                topSuggestions = values.OrderBy(s => s.Score).Take(count);
            else
                topSuggestions = values.OrderByDescending(s => s.Score).Take(count);

            var builder = new EmbedBuilder();
            builder.Color = new Color(0x7289da);
            int index = 0;
            foreach (var suggestion in topSuggestions)
            {
                index++;
                builder.AddField($"#{index}: {suggestion.Score} points", suggestion.EllipsedContent);
            }
            var embed = builder.Build();

            await parameters.SocketMessage.Channel.SendMessageAsync($"the top {count} {order.ToString().ToLower()} suggestions are", false, embed);
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.ModCleaningAliases), roles: new[] { Server.Roles.Developers })]
        public static async Task CleanMods(CommandParameters parameters)
        {
            CrudeModdingStorage.Current = new CrudeModdingStorage();
            await parameters.Android.GetListener<CrudeModListener>().SaveToDisk();
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Developers })]
        public static async Task RetrieveSpreadsheet(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync($"here goes...");
            try
            {
                parameters.Android.GetListener<DebugListener>().RetrieveReponseSpreadsheet();
                await parameters.SocketMessage.Channel.SendMessageAsync($"... did it! (´• ω •`)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await parameters.SocketMessage.Channel.SendMessageAsync($"... i messed up ( ; ω ; )");
            }
        }

        private enum Order { Best, Worst }
    }
}
