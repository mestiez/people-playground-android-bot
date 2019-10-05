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

        public const string Filename = "violation_log.txt";
        public HashSet<IViolation> Violations { get; } = new HashSet<IViolation>();

        public override Task Initialise()
        {
            Violations.Add(new MentionViolation(Server.Users.zooi));

            return Task.CompletedTask;
        }

        public override Task OnMessage(SocketMessage arg, Android android)
        {
            if (arg.Author.IsBot) return Task.CompletedTask;

            foreach (var violation in Violations)
            {
                if (!violation.Violates(arg, android)) continue;

                const string hBreak = "---------------------------------------------------------------------";
                var entry = $"\n{hBreak}\n{violation.GetType().Name} at {DateTime.Now.ToString()} by {arg.Author.Username}({arg.Author.Discriminator})\n\n{arg.Content}\n{hBreak}\n";
                Console.WriteLine(entry);
                File.AppendAllTextAsync(Environment.GetEnvironmentVariable("ANDROID_STORAGE") + Filename, entry);
            }

            return Task.CompletedTask;
        }
    }
}
