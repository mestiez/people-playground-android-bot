using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidBot
{
    public class MuteManager
    {
        public static MuteManager Main { get; private set; }
        public MuteManager(Android android)
        {
            this.android = android;
            Main = this;
        }

        public class MutedUser
        {
            public ulong UserID;
            public ulong ChannelID;
            public bool IsMuted;
            public DateTime UnmuteTime = DateTime.MaxValue;

            public MutedUser(ulong userID, ulong channelID, DateTime unmuteTime)
            {
                UserID = userID;
                ChannelID = channelID;
                IsMuted = false;
                UnmuteTime = unmuteTime;
            }
        }

        private Timer timer;
        private Android android;
        private HashSet<MutedUser> users = new HashSet<MutedUser>();
        private bool requireWriteToDisk = false;

        public const string Path = "MuteTable.json";
        public static string FullPath => Android.Path + Path;

        public static void Mute(ulong user, ulong channel, TimeSpan duration)
        {
            var existing = Main.users.FirstOrDefault(u => u.UserID == user);
            if (existing != null)
            {
                var updatedTime = DateTime.Now + duration;
                bool isLonger = updatedTime > existing.UnmuteTime;
                existing.UnmuteTime = updatedTime;
                _ = Task.Run(async () =>
                {
                    await Main.android.MainGuild.GetTextChannel(channel).SendMessageAsync(isLonger ? "extending mute..." : "shortening mute...");
                });
            }
            else
                Main.users.Add(new MutedUser(user, channel, DateTime.Now + duration));

            Main.requireWriteToDisk = true;
        }

        public static void Unmute(ulong user)
        {
            foreach (var u in Main.users)
                if (u.UserID == user) u.UnmuteTime = DateTime.MinValue;

            Main.requireWriteToDisk = true;
        }

        public async Task Initialise()
        {
            await LoadUsers();
            timer = new Timer(Tick, null, 0, 1000);
            Console.WriteLine($"{nameof(MuteManager)} initialised");
        }

        private void Tick(object stateInfo)
        {
            _ = Task.Run(UpdateRoles);
        }

        private async Task UpdateRoles()
        {
            var now = DateTime.Now;

            foreach (var user in users)
            {
                if (now < user.UnmuteTime && !user.IsMuted)
                {
                    await SetMuteStatus(user.UserID, user.ChannelID, true);
                    user.IsMuted = true;
                    requireWriteToDisk = true;
                }
                else if (now >= user.UnmuteTime && user.IsMuted)
                {
                    await SetMuteStatus(user.UserID, user.ChannelID, false);
                    user.IsMuted = false;
                    requireWriteToDisk = true;
                }
            }

            users.RemoveWhere(u => !u.IsMuted);

            if (requireWriteToDisk) await SaveUsers();
        }

        private async Task LoadUsers()
        {
            if (File.Exists(FullPath))
            {
                try
                {
                    var raw = await File.ReadAllTextAsync(FullPath);
                    users = JsonConvert.DeserializeObject<HashSet<MutedUser>>(raw);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't access {FullPath}: {e.Message}");
                    users = new HashSet<MutedUser>();
                }
            }
            else users = new HashSet<MutedUser>();
        }

        private async Task SaveUsers()
        {
            requireWriteToDisk = false;
            var raw = JsonConvert.SerializeObject(users);
            await File.WriteAllTextAsync(FullPath, raw);
        }

        private async Task SetMuteStatus(ulong userId, ulong channelId, bool muted)
        {
            var mutedRole = android.MainGuild.GetRole(Server.Roles.Muted);
            SocketGuildUser user = android.MainGuild.GetUser(userId);

            string message = (muted ? DebugResponseConfiguration.Current.MutingNotification.PickRandom() : DebugResponseConfiguration.Current.UnmutingNotification.PickRandom()) + user.Username;

            try
            {
                if (muted)
                    await user.AddRoleAsync(mutedRole);
                else
                    await user.RemoveRoleAsync(mutedRole);
            }
            catch (Exception)
            {
                Console.WriteLine("Could not set mute status on " + user.Username);
                return;
            }

            await android.MainGuild.GetTextChannel(channelId).SendMessageAsync(message);
        }
    }
}
