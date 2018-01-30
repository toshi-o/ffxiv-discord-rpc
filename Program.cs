using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Sharlayan;
using Sharlayan.Core.Enums;
using Sharlayan.Models;

namespace FFXIV_discord_rpc
{
    public static class Program
    {
        public enum STATUS
        {
            NOTFOUND,
            READING,
            CANTREADCHARACTER,
            CANTFINDCHARACTER,
            RUNNING
        }

        //App registered to my Discord account. "FINAL FANTASY XIV"
        private const string APPID = "401183593273098252";
        //the signatures have been working for a really long time but this is just to be safe
        private const string REQUIRED_VERSION = "2017.12.06.0000.0000";
        //version
#if asdf
        private const string VERSION = "TEST";
#else
        private const string VERSION = "1.2.2";
#endif

        public static RegistryKey AutostartKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        public static Icon icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        public static DiscordRpc.RichPresence Presence;
        public static DiscordRpc.EventHandlers handlers;

        private static void Exit(string message = "")
        {
            DiscordRpc.Shutdown();
            #if !asdf
            MemoryHandler.Instance.UnsetProcess();
            #endif
            if(message != "") MessageBox.Show(message);
            Application.Exit();
        }

        private static void UpdatePresence()
        {
            var onlinestatus = Player.Status.ToString();

            var location = Player.Location == "Mordion Gaol" ? "https://support.na.square-enix.com/" : Player.Location;

            Presence.details = Player.Name;

            Presence.state = TrayContext.LocationUnderNameItem.Checked ? location : string.Empty;

            if (TrayContext.FfxivIconItem.Checked)
            {
                Presence.largeImageKey = "ffxiv";
                Presence.largeImageText = string.Empty;
                Presence.smallImageKey = string.Empty;
                Presence.smallImageText = string.Empty;
            }
            else
            {
                Presence.largeImageKey = onlinestatus.ToLower();
                Presence.largeImageText = location;
                Presence.smallImageKey = Player.Job.ToString().ToLower();
                Presence.smallImageText = $"{Player.Job.ToString()} Lv{Player.Level}";
            }

            if (Player._locationChanged)
                Presence.startTimestamp = (Player.Status == Actor.Icon.InDuty || Player.Status == Actor.Icon.PvP) ? ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() : 0;

            DiscordRpc.UpdatePresence(ref Presence);
        }

        public static bool Attached()
        {
#if asdf
            return true;
#else
            return MemoryHandler.Instance.IsAttached;
#endif
        }

#if asdf
        public static Random rand = new Random();

        //feel free to add more!!! these are just for testing things in case the game is unavailable to me (like 4.2 maintenance >_>)
        static string[] names =
        {
            "Sigmascape V4.0", "Cyberspace", "Reaper's Underground", "Transparency o_o", "Aperture Science"
        };

        public static string GetRandomLocation()
        {
            return names[rand.Next(0, names.Length)];
        }

        private static void Log(string message)
        {
            File.AppendAllText("log.txt", $"{DateTime.Now:hh:mm:ss tt} | {message}\r\n");
        }
#endif

        public static void MainLoop()
        {
#if asdf
            Player.Location = GetRandomLocation();
#endif
            TrayContext.UpdateCharacterName();

            DiscordRpc.Initialize(APPID, ref handlers, false, null);

            while (Attached())
            {
                var characterFound = false;

#if asdf
                Player.Job = Actor.Job.FSH;
                Player.Level = rand.Next(1, 70);
                Player.Status = Actor.Icon.InDuty;
                if (Player._changed)
                    Log(Player.ToString());

                characterFound = true;
#else
                foreach (var pc in Reader.GetActors().PCEntities.Values)
                {
                    if (pc.Name != Player.Name) continue;
                    Player.Job = pc.Job;
                    Player.Level = pc.Level;
                    //This becomes ??? if you're loading something
                    Player.Location = pc.Location;
                    //None and Online are the same thing pretty much
                    Player.Status = pc.OnlineStatus == Actor.Icon.None ? Actor.Icon.Online : pc.OnlineStatus;
                    characterFound = Player.Job != 0 && Player.Level != 0 && Player.Status != 0 && !string.IsNullOrEmpty(Player.Location);
                    break;
                }
#endif

                if (!characterFound)
                {
                    TrayContext.UpdateStatus(STATUS.CANTFINDCHARACTER);
                    Thread.Sleep(5000);
                    continue;
                }

                TrayContext.UpdateStatus(STATUS.RUNNING);

                if (Player._changed)
                {
                    UpdatePresence();
                }

                Player._changed = false;

                Player._locationChanged = false;

                Thread.Sleep(1000);
            }
        }

        public static void FFXIVCheckThread()
        {
            Thread.CurrentThread.IsBackground = true;

            while (true)
            {
#if !asdf
                if (MemoryHandler.Instance.IsAttached) continue;
#endif
                TrayContext.UpdateStatus(STATUS.NOTFOUND);

#if asdf
                Player.Name = "Ffxiv Finalfantasy";
                MainLoop();
#else
                //check for any FFXIV processes and make sure that they're actually running the game and not something like ffxiv_mediaplayer
                foreach (var process in Process.GetProcesses())
                {
                    if (process.MainWindowTitle != "FINAL FANTASY XIV") continue;
                    switch (process.ProcessName)
                    {
                        case "ffxiv":
                            Exit("DirectX 9 mode not supported.");
                            break;
                        case "ffxiv_dx11":
                            //check version
                            var gamedir = Path.GetDirectoryName(process.MainModule.FileName);
                            if (string.IsNullOrEmpty(gamedir))
                            {
                                Exit($"Unable to use GetDirectoryName on {process.MainModule.FileName} (report this on github)");
                                break;
                            }

                            var verfile = Path.Combine(gamedir, "ffxivgame.ver");
                            if (!File.Exists(verfile))
                            {
                                Exit("Unable to find version file - are you sure you're running the real game?");
                                break;
                            }

                            var version = File.ReadAllText(verfile);
                            if (version != REQUIRED_VERSION)
                            {
                                Exit($"Version mismatch: only works with {REQUIRED_VERSION} and your client is {version}");
                                continue;
                            }

                            MemoryHandler.Instance.SetProcess(new ProcessModel
                            {
                                Process = process,
                            });

                            READNAME:
                            Player.Name = Reader.GetPlayerInfo().PlayerEntity.Name;
                            if (string.IsNullOrEmpty(Player.Name))
                            {
                                TrayContext.UpdateStatus(STATUS.CANTREADCHARACTER);
                                Thread.Sleep(1000);
                                goto READNAME;
                            }

                            MainLoop();

                            break;
                        default:
                            Exit($"This isn't FINAL FANTASY XIV?\n{process.MainWindowTitle}\n{Path.GetDirectoryName(process.MainModule.FileName)}");
                            break;
                    }

                    break;
                }
#endif
                Thread.Sleep(5000);
            }
        }
        
        //https://stackoverflow.com/a/10250051
        public class TrayContext : ApplicationContext
        {
            static readonly MenuItem seperatorItem = new MenuItem("-");
            private static MenuItem statusItem = new MenuItem
            {
                Enabled = false
            };
            private static MenuItem characterItem = new MenuItem("Character: Unknown")
            {
                Enabled = false
            };
            public static MenuItem FfxivIconItem = new MenuItem("Show FFXIV icon instead of job")
            {
                Checked = false
            };
            public static MenuItem LocationUnderNameItem = new MenuItem("Show location under character name")
            {
                Checked = false
            };
            private static MenuItem AutostartItem = new MenuItem("Automatically start with Windows")
            {
                Checked = AutostartKey.GetValue("ffxiv-discord-rpc") != null
            };

            private static MenuItem name = new MenuItem("ffxiv-discord-rpc v"+ VERSION);

            public static MenuItem ChangeNameItem = new MenuItem("Change name");
            private static MenuItem ForceUpdateItem = new MenuItem("Force presence update");
#if asdf
            private static MenuItem ChangeLocation = new MenuItem("change location");
#endif

            private readonly NotifyIcon _trayIcon;

            public static void UpdateCharacterName()
            {
                characterItem.Text = "Character: "+ (string.IsNullOrEmpty(Player.Name) ? "Unknown" : Player.Name);
            }

            public static void UpdateStatus(STATUS status)
            {
                string fix = string.Empty;
                switch (status)
                {
                    case STATUS.NOTFOUND:
                        fix = "FFXIV not found";
                        UpdateCharacterName();
                        break;
                    case STATUS.CANTREADCHARACTER:
                        fix = "Can't read character name";
                        UpdateCharacterName();
                        break;
                    case STATUS.CANTFINDCHARACTER:
                        fix = "Character not found";
                        break;
                    case STATUS.RUNNING:
                        fix = "Running";
                        break;
                }
                statusItem.Text = "Status: " + fix;
            }

            public TrayContext()
            {
                name.Click += (sender, args) => { Process.Start("https://github.com/Poliwrath/ffxiv-discord-rpc"); };
                AutostartItem.Click += (sender, args) =>
                {
                    if (AutostartItem.Checked)
                        AutostartKey.SetValue("ffxiv-discord-rpc", Assembly.GetExecutingAssembly().Location);
                    else if(AutostartKey.GetValue("ffxiv-discord-rpc") != null)
                        AutostartKey.DeleteValue("ffxiv-discord-rpc");
                };
                FfxivIconItem.Click += (sender, args) =>
                {
                    //TODO saving and loading from file
                    FfxivIconItem.Checked = !FfxivIconItem.Checked;
                    UpdatePresence();
                };
                LocationUnderNameItem.Click += (sender, args) =>
                {
                    //TODO saving and loading from file
                    LocationUnderNameItem.Checked = !LocationUnderNameItem.Checked;
                    UpdatePresence();
                };
#if asdf
                ChangeLocation.Click += (sender, args) =>
                {
                    Player.Location = GetRandomLocation();
                };
#endif
                ChangeNameItem.Click += (sender, args) =>
                {
                    var prompt = new Form
                    {
                        Width = 100,
                        Height = 100,
                        FormBorderStyle = FormBorderStyle.FixedToolWindow,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    var textBox = new TextBox { Width=105 };
                    textBox.KeyDown += (a, b) =>
                    {
                        if (b.KeyCode == Keys.Enter)
                        {
                            b.SuppressKeyPress = true;
                            var emptyBox = string.IsNullOrEmpty(textBox.Text);
                            if (!emptyBox)
                            {
                                Player.Name = textBox.Text;
                            }                                                    
                            
                            MessageBox.Show(emptyBox
                                ? "No name entered."
                                : "New name: " + textBox.Text);
                            
                            characterItem.Text = "Character: " + Player.Name;
                            
                            prompt.Close();
                        }
                    };
                    prompt.Controls.Add(textBox);
                    prompt.ShowDialog();
                };
                ForceUpdateItem.Click += (sender, args) =>
                {
                    UpdatePresence();
                };
                _trayIcon = new NotifyIcon
                {
                    Icon = icon,
                    ContextMenu = new ContextMenu(new[] {
                        name, 
                        statusItem,
                        characterItem,
                        seperatorItem,
                        FfxivIconItem,
                        LocationUnderNameItem,
                        AutostartItem,
                        ChangeNameItem,
                        ForceUpdateItem,
                        #if asdf
                        ChangeLocation,
                        #endif
                        new MenuItem("Exit", delegate
                        {
                            _trayIcon.Visible = false;
                            Exit();
                        })
                    }),
                    Visible = true
                };
                new Thread(FFXIVCheckThread).Start();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (!File.Exists("discord-rpc.dll"))
            {
                MessageBox.Show("discord-rpc.dll missing! (please extract all the files, or if you're a developer read README.md)");
                Application.Exit();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }
}
