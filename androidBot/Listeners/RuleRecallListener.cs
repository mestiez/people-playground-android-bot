using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public class RuleRecallListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Users => new[] { Server.Users.Any };
        public override ulong[] Roles => new[] { Server.Roles.Moderators, Server.Roles.Developers, Server.Roles.TrialMods, };

        private Regex ruleRegex;

        public override async Task Initialise(Android android)
        {
            ruleRegex = new Regex("rule ?((# ?)|(number )?)\\d+", RegexOptions.IgnoreCase);
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            var message = arg.Content;
            var rules = await Utils.ReadAllRules();
            string response = "";
            foreach (Match match in ruleRegex.Matches(message))
            {
                string digitMessage = new string(match.Value.Where(c => char.IsDigit(c)).ToArray()).Trim();
                bool parseSuccess = int.TryParse(digitMessage, out int ruleNr);
                bool validRange = rules.Length >= ruleNr && ruleNr > 0;

                if (parseSuccess && validRange)
                    response += $"rule {rules[ruleNr - 1].ToLower()}\n";

            }
            await arg.Channel.SendMessageAsync(response);
        }
    }
}
