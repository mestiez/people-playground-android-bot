using Discord;
using Discord.Rest;
using Discord.WebSocket;
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
            var reactions = message.Reactions;
            var hasUpvotes = reactions.TryGetValue(Upvote, out var upvoteMetadata);
            var hasDownvotes = reactions.TryGetValue(Downvote, out var downvoteMetadata);
            if ((!hasUpvotes || !hasDownvotes) && doReactionCheck) return;

            Suggestions.Add(message.Id, new Suggestion(upvoteMetadata.ReactionCount, downvoteMetadata.ReactionCount, message.Author.Id, message.Content));
        }

        private bool IsSuggestion(string content) => (content.Trim().ToLower().StartsWith("suggestion"));

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

            public override string ToString()
            {
                string ellipsed = Content;
                if (Content.Length > 250)
                    ellipsed = Content.Substring(0, 250) + "...";

                return $"```{ellipsed}```{Score} points";
            }

            public async Task<string> ToString(Android android)
            {
                var author = (android.MainGuild.GetUser(AuthorId)?.Username ?? "unknown user");
                return $"{this}\nby {author}";
            }
        }
    }
}
