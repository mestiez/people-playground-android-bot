using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidBot
{
    public class MuteSystem
    {
        public static MuteSystem Main { get; private set; }
        public const string Path = "MuteEntries.json";
        public static string FullPath => Android.Path + Path;
        private Dictionary<ulong, MuteEntry> entries;

        private class MuteEntry
        {
            public ulong User { get; set; }
            public ulong ChannelID { get; set; }
            public DateTime Expiration { get; set; }

            public MuteEntry(ulong user, ulong channelID, DateTime expiration)
            {
                User = user;
                ChannelID = channelID;
                Expiration = expiration;
            }
        }

        private Android android;
        private Timer timer;
        private bool requireWriteToDisk;

        public MuteSystem(Android android)
        {
            this.android = android;
            Main = this;
        }

        public static async Task Initialise()
        {
            await Main.LoadEntries();

            Main.timer = new Timer(async (ob) =>
            {
                await Main.CheckForExpiration();
                if (Main.requireWriteToDisk)
                    await Main.SaveEntries();
            }, null, 2000, 1000);

            Console.WriteLine($"{nameof(MuteSystem)} initialised");
        }

        public static async Task Mute(ulong userId, ulong channelId, TimeSpan duration)
        {
            Main.requireWriteToDisk = true;
            bool userIsAlreadyMuted = Main.entries.TryGetValue(userId, out var entry);
            if (userIsAlreadyMuted)
            {
                var newExpiration = DateTime.UtcNow + duration;
                var isLonger = newExpiration > entry.Expiration;
                entry.Expiration = newExpiration;
                entry.ChannelID = channelId;
                await Main.android.MainGuild.GetTextChannel(channelId).SendMessageAsync(isLonger ? "extending mute..." : "shortening mute...");
            }
            else
            {
                Main.entries.Add(userId, new MuteEntry(userId, channelId, DateTime.UtcNow + duration));
                var user = Main.android.Client.GetUser(userId);
                if (user == null)
                {
                    Console.WriteLine("User with ID " + userId + " is null");
                    return;
                }
                await Main.android.MainGuild.GetTextChannel(channelId).SendMessageAsync(DebugResponseConfiguration.Current.MutingNotification.PickRandom() + user.Username);
            }
            await Main.SetRole(userId, true);
        }

        public static async Task Unmute(ulong userId)
        {
            Main.requireWriteToDisk = true;
            var entry = Main.entries[userId];
            var removalSuccess = Main.entries.Remove(userId);
            if (!removalSuccess)
                Console.WriteLine("Could not remove " + userId + " from the mute entry list");

            var user = Main.android.Client.GetUser(userId);
            if (user == null)
            {
                Console.WriteLine("User with ID " + userId + " is null");
                return;
            }
            await Main.SetRole(userId, false);
            await Main.android.MainGuild.GetTextChannel(entry.ChannelID).SendMessageAsync(DebugResponseConfiguration.Current.UnmutingNotification.PickRandom() + user.Username);
        }

        private async Task CheckForExpiration()
        {
            if (entries.Count == 0) return;
            foreach (var pair in entries.ToArray())
            {
                var expired = DateTime.UtcNow > pair.Value.Expiration;
                if (expired)
                    await Unmute(pair.Key);
            }
        }

        private async Task LoadEntries()
        {
            if (!File.Exists(FullPath))
                entries = new Dictionary<ulong, MuteEntry>();
            else
            {
                try
                {
                    var raw = await File.ReadAllTextAsync(FullPath);
                    entries = JsonConvert.DeserializeObject<Dictionary<ulong, MuteEntry>>(raw);
                    if (entries == null)
                        entries = new Dictionary<ulong, MuteEntry>();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't access {FullPath}: {e.Message}");
                    entries = new Dictionary<ulong, MuteEntry>();
                }
            }
        }

        private async Task SaveEntries()
        {
            requireWriteToDisk = false;
            var raw = JsonConvert.SerializeObject(entries);
            try
            {
                await File.WriteAllTextAsync(FullPath, raw);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't write to {FullPath}: {e.Message}");
            }
        }

        private async Task SetRole(ulong userId, bool muted)
        {
            var mutedRole = android.MainGuild.GetRole(Server.Roles.Muted);
            SocketGuildUser user = android.MainGuild.GetUser(userId);
            if (user == null)
            {
                Console.WriteLine("Could not retrieve user " + userId);
                return;
            }
            bool alreadyMuted = user.Roles.Any(r => r.Id == mutedRole.Id);

            if (muted && alreadyMuted)
            {
                Console.WriteLine("User is already muted: " + user.Username);
                return;
            }
            else if (!muted && !alreadyMuted)
            {
                Console.WriteLine("User is already unmuted: " + user.Username);
                return;
            }

            try
            {
                if (muted)
                    await user.AddRoleAsync(mutedRole);
                else
                    await user.RemoveRoleAsync(mutedRole);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not set mute status on " + user.Username);
                Console.WriteLine(e);
                return;
            }
        }
    }
}
