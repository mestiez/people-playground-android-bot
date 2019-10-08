using System;

namespace AndroidBot.Listeners
{
    public class CommandContainerAttribute : Attribute, IPermissions
    {
        public CommandContainerAttribute()
        {
        }

        public CommandContainerAttribute(ulong[] channels = default, ulong[] users = default, ulong[] roles = default)
        {
            Channels = channels ?? new[] { Server.Channels.Any };
            Users = users ?? new[] { Server.Users.Any };
            Roles = roles ?? new[] { Server.Users.Any };
        }

        public virtual ulong[] Channels { get; protected set; } = { Server.Channels.Any };
        public virtual ulong[] Users { get; protected set; } = { Server.Users.Any };
        public virtual ulong[] Roles { get; protected set; } = { Server.Users.Any };
    }
}
