using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndroidBot
{
    public static class Utils
    {
        private static Random random = new Random();

        public static T PickRandom<T>(this IList<T> collection) where T : class
        {
            if (collection == null)
                return null;
            if (collection.Count == 0)
                return null;
            return collection[random.Next(0, collection.Count)];
        }

        public static bool IsAuthorised(IPermissions permissions, ulong channel, ulong user, IEnumerable<IRole> roles)
        {
            bool c = permissions.Channels.Contains(Server.Channels.Any) || permissions.Channels.Contains(channel);
            bool u = permissions.Users.Contains(Server.Users.Any) || permissions.Users.Contains(user);
            bool r = permissions.Roles.Contains(Server.Roles.Any) || permissions.Roles.Any(i => roles.Any(role => role.Id == i));

            return c && (u || r);
        }
    }
}