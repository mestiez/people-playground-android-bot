using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace AndroidBot
{
    public static class Utils
    {
        private static Random random = new Random();

        public static T PickRandom<T>(this IList<T> collection) where T : class
        {
            if (collection == null)
                return null;
            if (collection.Count == 0)
                return null;
            return collection[random.Next(0, collection.Count)];
        }

        public static bool IsAuthorised(IPermissions permissions, ulong channel, ulong user, IEnumerable<IRole> roles)
        {
            bool c = permissions.Channels.Contains(Server.Channels.Any) || permissions.Channels.Contains(channel);
            bool u = permissions.Users.Contains(Server.Users.Any) || permissions.Users.Contains(user);
            bool r = permissions.Roles.Contains(Server.Roles.Any) || permissions.Roles.Any(i => roles.Any(role => role.Id == i));

            return c && u && r;
        }

        public static async Task<string[]> ReadAllRules()
        {
            const ulong RULE_MESSAGE = 604051493599051786;
            var channel = Android.Instance.Client.GetChannel(Server.Channels.Information) as ISocketMessageChannel;
            var message = await channel.GetMessageAsync(RULE_MESSAGE);
            string messageContent = message.Content;
            Regex ruleRegex = new Regex("\\d+ - .+");
            MatchCollection ruleLines = ruleRegex.Matches(messageContent);
            return ruleLines.Select(r => r.Value).ToArray();
        }

        public static MatchCollection GetUserCodesFromText(string text) => Regex.Matches(text, "<@(.*?)>");

        public static async Task<T> HttpGet<T>(string uri)
        {
            using (HttpClient http = new HttpClient())
            {
                var body = await http.GetStringAsync(uri);
                var tree = JsonConvert.DeserializeObject<T>(body);
                return tree;
            }
        }
    }
}