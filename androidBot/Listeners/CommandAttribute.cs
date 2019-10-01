using System;

namespace AndroidBot.Listeners
{
    public partial class DebugListener
    {
        public class CommandAttribute : Attribute, IPermissions
        {
            public CommandAttribute(string[] aliases, ulong[] channels = default, ulong[] users = default, ulong[] roles = default)
            {
                Aliases = aliases;
                Channels = channels ?? new[] { Server.Channels.Any };
                Users = users ?? new[] { Server.Users.Any };
                Roles = roles ?? new[] { Server.Users.Any };
            }

            public CommandAttribute()
            {
                Aliases = new string[] { };
                Channels = new[] { Server.Channels.Any };
                Users = new[] { Server.Users.Any };
                Roles = new[] { Server.Users.Any };
            }

            public string[] Aliases { get; set; }
            public virtual ulong[] Channels { get; private set; } = { Server.Channels.Any };
            public virtual ulong[] Users { get; private set; } = { Server.Users.Any };
            public virtual ulong[] Roles { get; private set; } = { Server.Users.Any };
        }
    }
}
