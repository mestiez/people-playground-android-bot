using System;
using System.Reflection;

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
            public virtual ulong[] Channels { get; protected set; } = { Server.Channels.Any };
            public virtual ulong[] Users { get; protected set; } = { Server.Users.Any };
            public virtual ulong[] Roles { get; protected set; } = { Server.Users.Any };

            public virtual void Initialise() { }
        }

        public class ReflectiveCommandAttribute : CommandAttribute
        {
            private readonly string configFieldName;

            public ReflectiveCommandAttribute(string configFieldName, ulong[] channels = default, ulong[] users = default, ulong[] roles = default)
            {
                Aliases = new string[] { };
                Channels = channels ?? new[] { Server.Channels.Any };
                Users = users ?? new[] { Server.Users.Any };
                Roles = roles ?? new[] { Server.Users.Any };
                this.configFieldName = configFieldName;
            }

            public override void Initialise()
            {
                base.Initialise();

                try
                {
                    FieldInfo foundField = typeof(DebugResponseConfiguration).GetField(configFieldName);
                    Aliases = (string[])foundField.GetValue(DebugResponseConfiguration.Current);
                    Console.WriteLine("Command reading aliases from " + configFieldName + " read " + string.Join(", ", Aliases));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Command reading aliases from " + configFieldName + " is unable to do so: " + e.Message);
                }
            }
        }
    }
}
