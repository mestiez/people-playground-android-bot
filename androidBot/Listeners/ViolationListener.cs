﻿using System;
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

        public const string Path = "violation_log.txt";
        public HashSet<IViolation> Violations { get; } = new HashSet<IViolation>();

        public override Task Initialise()
        {
            Violations.Add(new MentionViolation(Server.Users.zooi));

            return Task.CompletedTask;
        }

        public override Task OnMessage(SocketMessage arg, Android android)
        {
            if (arg.Author.IsBot) return Task.CompletedTask;

            if (arg.MentionedUsers.Any(s => s.Id == Server.Users.zooi))
                File.AppendAllTextAsync(Path,  $"{arg.Author.Username}({arg.Author.Discriminator}): {arg.Content}{Environment.NewLine}");

            return Task.CompletedTask;
        }
    }
}