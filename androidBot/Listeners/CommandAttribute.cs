using System;

namespace AndroidBot.Listeners
{
    public class CommandAttribute : Attribute, IPermissions
    {
        public CommandAttribute(string[] aliases = null, ulong[] channels = null, ulong[] users = null, ulong[] roles = null)
        {
            Aliases = aliases ?? Array.Empty<string>();
            Channels = channels ?? new[] { Server.Channels.Any };
            Users = users ?? new[] { Server.Users.Any };
            Roles = roles ?? new[] { Server.Users.Any };
        }

        public CommandAttribute()
        {
            Aliases = Array.Empty<string>();
            Channels = new[] { Server.Channels.Any };
            Users = new[] { Server.Users.Any };
            Roles = new[] { Server.Users.Any };
        }

        public string[] Aliases { get; set; }
        public virtual ulong[] Channels { get; protected set; } = { Server.Channels.Any };
        public virtual ulong[] Users { get; protected set; } = { Server.Users.Any };
        public virtual ulong[] Roles { get; protected set; } = { Server.Users.Any };

        public virtual void Initialise() { }
    }
}
