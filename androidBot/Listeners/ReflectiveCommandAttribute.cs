using System;
using System.Reflection;

namespace AndroidBot.Listeners
{
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
