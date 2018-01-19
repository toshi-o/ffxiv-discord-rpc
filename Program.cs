using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Sharlayan;
using Sharlayan.Core.Enums;
using Sharlayan.Models;

namespace FFXIV_discord_rpc
{
    public static class Program
    {
        //App registered to my Discord account. "FINAL FANTASY XIV"
        private const string APPID = "401183593273098252";
        //the signatures have been working for a really long time but this is just to be safe
        private const string VERSION = "2017.12.06.0000.0000";
        private static bool _changed;

        private static void Msg(string message, bool waitforinput = false)
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss tt} | {message}");
            if (waitforinput) Console.ReadKey();
        }

        private static int _level;
        public static int Level
        {
            get => _level;
            set
            {
                if (_level == value) return;
                var old = _level;
                _level = value;
                Msg($"Level: {old} -> {value}");
                _changed = true;
            }
        }

        private static Actor.Job _job = 0;
        public static Actor.Job Job
        {
            get => _job;
            set
            {
                if (_job == value) return;
                var old = _job;
                _job = value;
                Msg($"Job: {old} -> {value}");
                _changed = true;
            }
        }

        private static string _location = string.Empty;
        public static string Location
        {
            get => _location;
            set
            {
                if (_location == value || value == "???") return;
                var old = _location;
                _location = value;
                Msg($"Location: {old} -> {value}");
                _changed = true;
            }
        }

        private static Actor.Icon _status = 0;
        public static Actor.Icon Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                var old = _status;
                _status = value;
                Msg($"Status: {old} -> {value}");
                _changed = true;
            }
        }

        private static void Main()
        {
            foreach (var process in Process.GetProcesses())
            {
                //check for any FFXIV processes and make sure that they're actually running the game and not something like ffxiv_mediaplayer
                if (process.MainWindowTitle != "FINAL FANTASY XIV") continue;
                switch (process.ProcessName)
                {
                    case "ffxiv":
                        Msg("DirectX 9 mode not supported.", waitforinput: true);
                        return;
                    case "ffxiv_dx11":
                        //check version
                        var gamedir = Path.GetDirectoryName(process.MainModule.FileName);
                        if (string.IsNullOrEmpty(gamedir))
                        {
                            Msg($"Unable to use GetDirectoryName on {process.MainModule.FileName}");
                            return;
                        }

                        var verfile = Path.Combine(gamedir, "ffxivgame.ver");
                        if (!File.Exists(Path.Combine(gamedir, "ffxivgame.ver")))
                        {
                            Msg("Are you running the game?", waitforinput: true);
                            return;
                        }

                        var ver = File.ReadAllText(verfile);
                        if (ver != VERSION)
                        {
                            Msg("This only works with " + VERSION + " and your game is " + ver, waitforinput: true);
                            return;
                        }

                        MemoryHandler.Instance.SetProcess(new ProcessModel
                        {
                            Process = process,
                        });
                        break;
                    default:
                        Msg($"?_?\n{Path.GetDirectoryName(process.MainModule.FileName)}", waitforinput: true);
                        return;
                }

                break;
            }

            if (!MemoryHandler.Instance.IsAttached)
            {
                Msg("where is FINAL FANTASY XIV !?", waitforinput: true);
                return;
            }

            Console.CancelKeyPress += delegate {
                DiscordRpc.Shutdown();
                MemoryHandler.Instance.UnsetProcess();
            };

            var character = Reader.GetPlayerInfo().PlayerEntity.Name;
            if (string.IsNullOrEmpty(character))
            {
                Msg("Are you logged in?", waitforinput: true);
                return;
            }

            var handlers = new DiscordRpc.EventHandlers();
            DiscordRpc.Initialize(APPID, ref handlers, true, null);
            var presence = new DiscordRpc.RichPresence();

            while (MemoryHandler.Instance.IsAttached)
            {
                var characterFound = false;
            
                foreach (var pc in Reader.GetActors().PCEntities.Values)
                {
                    if (pc.Name != character) continue;
                    Job = pc.Job;
                    Level = pc.Level;
                    //This becomes ??? if you're loading something
                    Location = pc.Location;
                    //None and Online are the same thing pretty much
                    Status = pc.OnlineStatus == Actor.Icon.None ? Actor.Icon.Online : pc.OnlineStatus;
                    characterFound = true;
                    break;
                }

                if (!characterFound)
                {
                    Msg("Your character is gone?", waitforinput: true);
                    break;
                }

                if (_changed)
                {
                    var onlinestatus2 = Status.ToString();
                    if (string.IsNullOrEmpty(onlinestatus2))
                    {
                        presence.details = character;
                        presence.largeImageKey = Status.ToString().ToLower();
                        presence.largeImageText = Location;
                        presence.smallImageKey = Job.ToString().ToLower();
                        presence.smallImageText = $"{Job.ToString()} Lv{Level}";
                    }
                    else
                    {
                        Msg("Something's wrong? onlinestatus2 is nothing.", waitforinput: true);
                        return;
                    }

                    DiscordRpc.UpdatePresence(ref presence);
                }

                _changed = false;

                Thread.Sleep(1000);
            }
        }
    }
}
