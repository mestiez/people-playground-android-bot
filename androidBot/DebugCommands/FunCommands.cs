using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer]
    public struct FunCommands
    {
        [Command(new[] { "i love you", "love you", "ilu", "<3" }, includeMethodName: false, users: new[] { Server.Users.zooi, Server.Users.Vincentmario })]
        public static async Task Love(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("<3");
        }

        [Command(new[] { "who is vincents best friend", "who's vincents best friend", "whos vincents best friend", "who is vincent's best friend", "who's vincent's best friend", "whos vincent's best friend" }, includeMethodName: false)]
        public static async Task VincentsBestFriend(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("zooi");
        }

        [Command(new[] { "who is zoois best friend", "who's zoois best friend", "whos zoois best friend", "who is zooi's best friend", "who's zooi's best friend", "whos zooi's best friend" }, includeMethodName: false)]
        public static async Task ZooiBestFriend(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("vincent");
        }

        [Command(new[] { "who is your best friend", "who's your best friend", "whos your best friend" }, includeMethodName: false)]
        public static async Task BestFriend(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(parameters.SocketMessage.Author.Id == Server.Users.Koof ? "you of course, silly <3 also you're short and gay" : "i don't know but it's definitely not you");
        }

        [Command(new[] { "there he is", "disguise", "lego star wars", "lego", "star wars", "at-st", "imperial walker", "Groucho" })]
        public static async Task Atst(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendFileAsync(Android.Path + "atst.png");
        }

        [Command(new[] { "what is ", "whats ", "what's " })]
        public static async Task Define(CommandParameters parameters)
        {
            //https://github.com/meetDeveloper/googleDictionaryAPI
            const string api = @"https://api.dictionaryapi.dev/api/v1/entries/en/";

            var message = parameters.SocketMessage.Content;
            var word = string.Join(" ", parameters.Arguments);
            try
            {
                var resultArray = await Utils.HttpGet<DictionaryApiResponse[]>(api + word);
                if (resultArray == null || resultArray.Length == 0)
                {
                    await parameters.SocketMessage.Channel.SendMessageAsync("i don't know");
                    return;
                }
                var result = resultArray[0];
                if (result.meaning == null || result.meaning.Count == 0)
                {
                    await parameters.SocketMessage.Channel.SendMessageAsync("i don't know");
                    return;
                }
                var reply = $@"**{result.word}**: {result.meaning.First().Value.First().definition}";
                await parameters.SocketMessage.Channel.SendMessageAsync(reply);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nwhile trying to get definition of \'" + word + "\'");
                await parameters.SocketMessage.Channel.SendMessageAsync("i don't know");
                return;
            }
        }

        [Command(users: new[] { Server.Users.zooi, Server.Users.Vincentmario, Server.Users.Dikkiedik, Server.Users.Koof })]
        public static async Task Say(CommandParameters parameters)
        {
            var message = parameters.SocketMessage.Content;
            message = message.Substring(message.IndexOf("say") + 3).Trim();

            bool specifiesChannel = message.EndsWith(">");
            ulong channelId = parameters.SocketMessage.Channel.Id;

            if (specifiesChannel)
            {
                Regex regex = new Regex(@"(in <#\d+>)");
                var matches = regex.Matches(message);
                if (matches.Count != 0)
                {
                    var match = matches.Last();
                    bool successfulParse = ulong.TryParse(new string(match.Value.Where(c => char.IsDigit(c)).ToArray()), out channelId);
                    if (!successfulParse)
                        await parameters.SocketMessage.Channel.SendMessageAsync("something went wrong");
                    else
                        message = message.Substring(0, message.Length - match.Length);
                }
            }

            var channel = parameters.Android.Client.GetChannel(channelId) as ISocketMessageChannel;
            await channel.SendMessageAsync(message);
        }
    }
}
