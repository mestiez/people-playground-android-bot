using Discord;
using Discord.Rest;
using Discord.WebSocket;
using F23.StringSimilarity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public class SuggestionListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Suggestions };
        public override ulong[] Roles => new[] { Server.Roles.Any };
        public override ulong[] Users => new[] { Server.Users.Any };

        public Dictionary<ulong, Suggestion> Suggestions { get; set; }
        public const string Path = "Suggestions.json";
        public string FullPath => Android.Path + Path;

        public static readonly IEmote Upvote = Server.Emotes.YES;
        public static readonly IEmote Downvote = Server.Emotes.NO;

        private NormalizedLevenshtein Levenshtein = new NormalizedLevenshtein();

        public override async Task Initialise(Android android)
        {
            android.Client.ReactionAdded += OnReactionAdd;
            android.Client.ReactionRemoved += OnReactionRemoved; ;
            await Load();
            await Task.CompletedTask;
        }

        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel.Id != Server.Channels.Suggestions) return;
            if (!Suggestions.TryGetValue(message.Id, out var suggestion)) return;
            if (reaction.Emote.Equals(Upvote))
                suggestion.Upvotes--;
            else if (reaction.Emote.Equals(Downvote))
                suggestion.Downvotes--;
            Save();
            await Task.CompletedTask;
        }

        private async Task OnReactionAdd(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel.Id != Server.Channels.Suggestions) return;
            if (!Suggestions.TryGetValue(message.Id, out var suggestion)) return;
            if (reaction.Emote.Equals(Upvote))
                suggestion.Upvotes++;
            else if (reaction.Emote.Equals(Downvote))
                suggestion.Downvotes++;

            Save();
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            await CheckNewSuggestion(arg);
        }

        private async Task CheckNewSuggestion(SocketMessage arg)
        {
            if (!IsSuggestion(arg.Content)) return;

            RestUserMessage restMessage = (RestUserMessage)await arg.Channel.GetMessageAsync(arg.Id);
            if (IsDuplicate(arg.Content))
            {
                await restMessage.Channel.SendMessageAsync("I will respectfully ignore your suggestion because it's been said before.");
                return;
            }

            if (arg.Content.Length > 1024)
                await restMessage.Channel.SendMessageAsync("A suggestion shouldn't exceed 1024 characters. Try not to group multiple suggestions into a single message.");

            await restMessage.AddReactionsAsync(new IEmote[] { Upvote, Downvote });
            AddSuggestion((IUserMessage)arg, false);
            Save();
        }

        public async Task Load()
        {
            if (File.Exists(FullPath))
            {
                try
                {
                    var raw = await File.ReadAllTextAsync(FullPath);
                    Suggestions = JsonConvert.DeserializeObject<Dictionary<ulong, Suggestion>>(raw);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading suggestions: " + e.Message);
                }
            }

            Suggestions = new Dictionary<ulong, Suggestion>();

        }

        public void Save()
        {
            var raw = JsonConvert.SerializeObject(Suggestions ?? new Dictionary<ulong, Suggestion>(), Formatting.Indented);
            File.WriteAllText(FullPath, raw);
        }

        public async Task GlobalRefresh(Android android, int maxMessages = 1000)
        {
            Suggestions.Clear();

            var channel = android.MainGuild.GetTextChannel(Server.Channels.Suggestions);
            var allMessages = await channel.GetMessagesAsync(maxMessages).FlattenAsync();

            foreach (IMessage message in allMessages)
            {
                try
                {
                    var n = (IUserMessage)message;
                    AddSuggestion(n);
                }
                catch (Exception)
                {
                    Console.WriteLine("Can't cast message to IUserMessage: " + message.Content);
                }
            }

            Save();
        }

        public void AddSuggestion(IUserMessage message, bool doReactionCheck = true)
        {
            if (!IsSuggestion(message.Content)) return;

            if (IsDuplicate(message.Content))
            {
                Console.WriteLine("Duplicate omitted");
                return;
            }

            var reactions = message.Reactions;
            var hasUpvotes = reactions.TryGetValue(Upvote, out var upvoteMetadata);
            var hasDownvotes = reactions.TryGetValue(Downvote, out var downvoteMetadata);
            if ((!hasUpvotes || !hasDownvotes) && doReactionCheck) return;

            Suggestions.Add(message.Id, new Suggestion(upvoteMetadata.ReactionCount, downvoteMetadata.ReactionCount, message.Author.Id, message.Content));
        }

        private bool IsSuggestion(string content) => (content.Trim().ToLower().StartsWith("suggestion"));

        private bool IsDuplicate(string suggestion)
        {
            return Suggestions.Any(s =>
            s.Value.Content == suggestion ||
            Levenshtein.Distance(s.Value.Content, suggestion) < .16f
            );
        }

        [Serializable]
        public class Suggestion
        {
            public int Upvotes;
            public int Downvotes;
            public ulong AuthorId;
            public string Content;

            public Suggestion(int upvotes, int downvotes, ulong authorId, string content)
            {
                Upvotes = upvotes;
                Downvotes = downvotes;
                AuthorId = authorId;
                Content = content;
            }

            [JsonIgnore]
            public int Score => Upvotes - Downvotes;

            [JsonIgnore]
            public string EllipsedContent
            {
                get
                {
                    string ellipsed = Content;
                    if (Content.Length > 1024)
                        ellipsed = Content.Substring(0, 1021) + "...";
                    return ellipsed;
                }
            }

            public string FindAuthorName(Android android)
            {

                return android.MainGuild.GetUser(AuthorId)?.Username ?? "a user that left the server or deleted their account";
            }
        }
    }
}
