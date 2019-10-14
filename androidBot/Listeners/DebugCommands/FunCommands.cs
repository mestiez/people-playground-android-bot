using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    [CommandContainer]
    public struct FunCommands
    {
        [Command(new[] { "i love you", "love you", "ilu", "<3" }, includeMethodName: false, users: new[] { Server.Users.zooi, Server.Users.Vincentmario })]
        public static async Task Love(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("<3");
        }

        [Command(new[] { "who is vincents best friend", "who's vincents best friend", "whos vincents best friend", "who is vincent's best friend", "who's vincent's best friend", "whos vincent's best friend" }, includeMethodName: false)]
        public static async Task VincentsBestFriend(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("zooi");
        }

        [Command(new[] { "who is zoois best friend", "who's zoois best friend", "whos zoois best friend", "who is zooi's best friend", "who's zooi's best friend", "whos zooi's best friend" }, includeMethodName: false)]
        public static async Task ZooiBestFriend(CommandParameters parameters)
        {
            await parameters.SocketMessage.Channel.SendMessageAsync("vincent");
        }
    }
}
