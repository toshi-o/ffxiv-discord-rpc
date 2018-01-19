using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sharlayan;
using Sharlayan.Core.Enums;
using Sharlayan.Models;

public static class Program
{
    //App registered to my Discord account. "FINAL FANTASY XIV"
    private const string APPID = "401183593273098252";
    //the signatures have been working for a really long time but this is just to be safe
    private const string VERSION = "2017.12.06.0000.0000";
    private static bool changed;
    private static int _level = 0;
    public static int Level
    {
        get { return _level; }
        set
        {
            if (!_level.Equals(value))
            {
                var old = _level;
                _level = value;
                Console.WriteLine($"{old} -> {value}");
                changed = true;
            }
        }
    }

    private static Actor.Job _job = 0;
    public static Actor.Job Job
    {
        get { return _job; }
        set
        {
            if (!_job.Equals(value))
            {
                var old = _job;
                _job = value;
                Console.WriteLine($"{old} -> {value}");
                changed = true;
            }
        }
    }

    private static string _location = string.Empty;
    public static string Location
    {
        get { return _location; }
        set
        {
            if (_location != value && value != "???")
            {
                var old = _location;
                _location = value;
                Console.WriteLine($"{old} -> {value}");
                changed = true;

            }
        }
    }

    private static Actor.Icon _status = 0;
    public static Actor.Icon Status
    {
        get { return _status; }
        set
        {
            if (!_status.Equals(value))
            {
                var old = _status;
                _status = value;
                Console.WriteLine($"{old} -> {value}");
                changed = true;
            }
        }
    }
    static void Main()
    {
        foreach (var process in Process.GetProcesses())
        {
            //check for any FFXIV processes and make sure that they're actually running the game and not something like ffxiv_mediaplayer
            if (process.MainWindowTitle == "FINAL FANTASY XIV")
            {
                if (process.ProcessName == "ffxiv")
                {
                    Console.WriteLine("DirectX 9 mode not supported.");
                    return;
                }
                if (process.ProcessName == "ffxiv_dx11")
                {
                    //check version
                    var gamedir = Path.GetDirectoryName(process.MainModule.FileName);
                    var verfile = Path.Combine(gamedir, "ffxivgame.ver");
                    if (!File.Exists(Path.Combine(gamedir, "ffxivgame.ver")))
                    {
                        Console.WriteLine("Are you running the game?");
                        return;
                    }

                    var ver = File.ReadAllText(verfile);
                    if (ver != VERSION)
                    {
                        Console.WriteLine("This only works with " + VERSION + " and your game is " + ver);
                        return;
                    }

                    MemoryHandler.Instance.SetProcess(new ProcessModel
                    {
                        Process = process,
                    });
                }
                else
                {
                    Console.WriteLine($"?_?\n{Path.GetDirectoryName(process.MainModule.FileName)}");
                    return;
                }

                break;
            }
        }

        if (!MemoryHandler.Instance.IsAttached)
        {
            Console.WriteLine("where is FINAL FANTASY XIV !?");
            return;
        }

        Console.CancelKeyPress += delegate {
            DiscordRpc.Shutdown();
            MemoryHandler.Instance.UnsetProcess();
        };

        string character = Reader.GetPlayerInfo().PlayerEntity.Name;
        if (character == string.Empty)
        {
            Console.WriteLine("Are you logged in?");
            return;
        }

        var handlers = new DiscordRpc.EventHandlers();
        DiscordRpc.Initialize(APPID, ref handlers, true, null);
        var presence = new DiscordRpc.RichPresence();
        bool found = false;

        while (MemoryHandler.Instance.IsAttached)
        {
            foreach (var pc in Reader.GetActors().PCEntities.Values)
            {
                if (pc.Name == character)
                {
                    Job = pc.Job;
                    Level = pc.Level;
                    //This becomes ??? if you're loading something
                    Location = pc.Location;
                    //None and Online are the same thing pretty much
                    Status = pc.OnlineStatus == Actor.Icon.None ? Actor.Icon.Online : pc.OnlineStatus;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Console.WriteLine("Your character is gone?");
                break;
            }

            if (changed)
            {
                string onlinestatus2 = Status.ToString();
                if (onlinestatus2 != string.Empty)
                {
                    presence.details = character;
                    presence.largeImageKey = Status.ToString().ToLower();
                    presence.largeImageText = Location;
                    presence.smallImageKey = Job.ToString().ToLower();
                    presence.smallImageText = $"{Job.ToString()} Lv{Level}";
                }
                else
                {
                    Console.WriteLine("Something's wrong?");
                    return;
                }

                DiscordRpc.UpdatePresence(ref presence);
            }

            Thread.Sleep(1000);
        }
    }
}
