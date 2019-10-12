using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{
    public class ViolationListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Users => new[] { Server.Users.Any };

        public const string Filename = "violation_log.html";
        public HashSet<IViolation> Violations { get; } = new HashSet<IViolation>();

        public override async Task Initialise(Android android)
        {
            Violations.Add(new MentionViolation(Server.Users.zooi));
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            if (arg.Author.IsBot) await Task.CompletedTask;

            foreach (var violation in Violations)
            {
                if (!violation.Violates(arg, android)) continue;

                const string hBreak = "<hr>";
                var entry = $"<br><b>{violation.GetType().Name} at {DateTime.Now.ToString()} by {arg.Author.Username}({arg.Author.Discriminator})</b><br>{arg.Content}<br>{hBreak}";
                Console.WriteLine(entry);
                await File.AppendAllTextAsync(Android.Path + Filename, entry);
            }

            await Task.CompletedTask;
        }
    }
}
