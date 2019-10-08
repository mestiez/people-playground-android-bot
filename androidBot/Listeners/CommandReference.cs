using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidBot.Listeners
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

        public bool IsAuthorised(ulong channel, ulong user, IEnumerable<IRole> roles)
        {
            return Permissions.All(p =>
            {
                var auth = Utils.IsAuthorised(p, channel, user, roles);
                return auth;
            });
        }
    }
}
