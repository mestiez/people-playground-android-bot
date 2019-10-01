using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidBot.Listeners
{
    public partial class DebugListener
    {
        public struct CommandReference
        {
            public string[] Aliases;
            public Delegate Delegate;
            public IPermissions[] Permissions;

            public CommandReference(string[] aliases, Delegate @delegate, IPermissions[] permissions)
            {
                Aliases = aliases;
                Delegate = @delegate;
                Permissions = permissions;
            }

            public bool IsAuthorised(ulong channel, ulong user, IEnumerable<IRole> roles) => Permissions.All(p => Utils.IsAuthorised(p, channel, user, roles));
        }
    }
}
