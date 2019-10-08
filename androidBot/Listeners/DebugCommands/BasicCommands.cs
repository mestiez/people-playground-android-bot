using Discord;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer]
    public struct BasicCommands
    {
        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.NevermindTriggers))]
        public static async Task Nothing(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("ok");
        }

        [ReflectiveCommand(nameof(DebugResponseConfiguration.Current.GreetingTriggers))]
        public static async Task Hi(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync(DebugResponseConfiguration.Current.GreetingResponses.PickRandom());
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Play(CommandParameters parameters)
        {
            var activityType = ActivityType.Playing;
            await SetStatus(parameters, activityType);
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Watch(CommandParameters parameters)
        {
            var activityType = ActivityType.Watching;
            await SetStatus(parameters, activityType);
        }

        [Command(roles: new[] { Server.Roles.Administrators, Server.Roles.Moderators, Server.Roles.Developers })]
        public static async Task Listen(CommandParameters parameters)
        {
            var activityType = ActivityType.Listening;
            await SetStatus(parameters, activityType);
        }

        private static async Task SetStatus(CommandParameters parameters, ActivityType activityType)
        {
            Console.WriteLine("Status set: " + activityType.ToString() + " " + string.Join(" ", parameters.Arguments));
            if (parameters.Arguments.Length == 0) await Task.CompletedTask;
            string game = string.Join(" ", parameters.Arguments);

            if (string.IsNullOrWhiteSpace(game)) return;

            await parameters.Android.Client.SetActivityAsync(new Game(game, activityType));
        }
    }
}
