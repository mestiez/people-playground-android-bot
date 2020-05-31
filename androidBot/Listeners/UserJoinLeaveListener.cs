using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace AndroidBot.Listeners
{

    public class UserJoinLeaveListener : MessageListener
    {
        public override ulong[] Channels => new[] { Server.Channels.Any };
        public override ulong[] Users => new[] { Server.Users.Any };
        public override ulong[] Roles => new[] { Server.Roles.Any };

        public const ulong LogChannel = Server.Channels.Log;
        public const string Filename = "join_leave_log.html";
        const string Break = "<hr>";

        private Android android;

        public override async Task Initialise(Android android)
        {
            this.android = android;
            android.Client.UserJoined += Client_UserJoined;
            android.Client.UserLeft += Client_UserLeft;
            android.Client.UserBanned += Client_UserBanned;
            android.Client.UserUnbanned += Client_UserUnbanned;

            await Task.CompletedTask;
        }

        private async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            await Append($"{user.Username}({user.Discriminator}) unbanned on {DateTime.Now.ToString()}");
            await Task.CompletedTask;
        }

        private async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            string banReason = "";
            try
            {
                var ban = await guild.GetBanAsync(user);
                banReason = " for " + (string.IsNullOrWhiteSpace(ban.Reason) ? "no reason" : ("\"" + ban.Reason + "\""));
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to get ban reason for " + user.Username);
            }
            await Append($"{user.Username}({user.Discriminator}) banned on {DateTime.Now.ToString()}{banReason}");
            await Task.CompletedTask;
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            string leaveMessage = $"{user.Username}({user.Discriminator}) left on {DateTime.Now.ToString()}";
            var roles = user.Roles.Where(r => !r.IsEveryone).ToList() ;
            if (roles.Count > 0)
                leaveMessage += $"\n**Left with roles:** {string.Join(", ", roles.Select(s => s.Name))}";

            await Append(leaveMessage);
            await Task.CompletedTask;
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            await Append($"{user.Username}({user.Discriminator}) joined on {DateTime.Now.ToString()}");

            if (MuteSystem.IsMuted(user.Id))
                await user.AddRoleAsync(android.MainGuild.GetRole(Server.Roles.Muted));

            await Task.CompletedTask;
        }

        public override async Task OnMessage(SocketMessage arg, Android android)
        {
            await Task.CompletedTask;
        }

        private async Task Append(string content)
        {
            var entry = $"<br>{content}<br>{Break}";
            await File.AppendAllTextAsync(Android.Path + Filename, entry);
            Console.WriteLine(entry);

            await (android.Client.GetChannel(LogChannel) as ISocketMessageChannel).SendMessageAsync(content);

            await Task.CompletedTask;
        }
    }
}
