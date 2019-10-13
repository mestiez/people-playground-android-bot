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
        public override ulong[] Roles => new[] { Server.Users.Any };

        public const string Filename = "violation_log.html";
        public HashSet<IViolation> Violations { get; } = new HashSet<IViolation>();

        public override async Task Initialise(Android android)
        {
            Violations.Add(new MentionViolation(Server.Users.zooi));
            Violations.Add(new MentionSpamViolation(10, 15));
            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            SocketGuildUser author = android.MainGuild.GetUser(arg.Author.Id);
            if (author.IsBot) await Task.CompletedTask;
            //if (author.Roles.Any(c => c.Id == Server.Roles.Moderators || c.Id == Server.Roles.Administrators || c.Id == Server.Roles.Developers)) await Task.CompletedTask;

            foreach (var violation in Violations)
            {
                if (!violation.Violates(arg, android)) continue;

                const string hBreak = "<hr>";
                var entry = $"<br><b>{violation.GetType().Name} at {DateTime.Now.ToString()} by {arg.Author.Username}({arg.Author.Discriminator})</b><br>{arg.Content}<br>{hBreak}";
                await File.AppendAllTextAsync(Android.Path + Filename, entry);
                Console.WriteLine(entry);

                await violation.Consequence(arg, android);
            }

            await Task.CompletedTask;
        }
    }
}
