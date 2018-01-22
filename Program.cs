using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Sharlayan;
using Sharlayan.Core.Enums;
using Sharlayan.Models;
using String = System.String;

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
        private const string VERSION = "1.2";

        public static RegistryKey AutostartKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        public static Icon icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        public static DiscordRpc.RichPresence Presence;
        public static DiscordRpc.EventHandlers handlers;

        private static void Log(string message)
        {
            File.AppendAllText("log.txt", $"{DateTime.Now:hh:mm:ss tt} | {message}\r\n");
        }

        private static void Exit(string message = "")
        {
            DiscordRpc.Shutdown();
            MemoryHandler.Instance.UnsetProcess();
            if(message != "") MessageBox.Show(message);
            Application.Exit();
        }

        private static void UpdatePresence()
        {
            var onlinestatus = Player.Status.ToString();
            if (string.IsNullOrEmpty(onlinestatus))
            {
                Exit("Online status is nothing? (report this on github)");
            }

            Presence.details = Player.Name;

            var location = Player.Location == "Mordion Gaol" ? "https://support.na.square-enix.com/" : Player.Location;

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

            Presence.startTimestamp = (Player.Status == Actor.Icon.InDuty || Player.Status == Actor.Icon.PvP) ? ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() : 0;

            DiscordRpc.UpdatePresence(ref Presence);
        }

        public static void FFXIVCheckThread()
        {
            Thread.CurrentThread.IsBackground = true;

            while (true)
            {
                if (MemoryHandler.Instance.IsAttached) continue;

                TrayContext.UpdateStatus(STATUS.NOTFOUND);

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
                            Player.Name = string.IsNullOrEmpty(Player.Name) ? Reader.GetPlayerInfo().PlayerEntity.Name : Player.Name;
                            if (string.IsNullOrEmpty(Player.Name))
                            {
                                TrayContext.UpdateStatus(STATUS.CANTREADCHARACTER);
                                Thread.Sleep(1000);
                                //this is a very scary line
                                goto READNAME;
                            }

                            TrayContext.UpdateCharacterName();

                            DiscordRpc.Initialize(APPID, ref handlers, false, null);
                            while (MemoryHandler.Instance.IsAttached)
                            {
                                var characterFound = false;

                                foreach (var pc in Reader.GetActors().PCEntities.Values)
                                {
                                    if (pc.Name != Player.Name) continue;
                                    Player.Job = pc.Job;
                                    Player.Level = pc.Level;
                                    //This becomes ??? if you're loading something
                                    Player.Location = pc.Location;
                                    //None and Online are the same thing pretty much
                                    Player.Status = pc.OnlineStatus == Actor.Icon.None ? Actor.Icon.Online : pc.OnlineStatus;
                                    characterFound = true;
                                    break;
                                }

                                if (!characterFound)
                                {
                                    TrayContext.UpdateStatus(STATUS.CANTFINDCHARACTER);
                                    //this is also a very scary line
                                    goto SLEEP;
                                }
                                
                                TrayContext.UpdateStatus(STATUS.RUNNING);

                                if (Player._changed)
                                {
                                    UpdatePresence();
                                }

                                Player._changed = false;

                                Thread.Sleep(1000);
                            }

                            break;
                        default:
                            Exit($"This isn't FINAL FANTASY XIV?\n{process.MainWindowTitle}\n{Path.GetDirectoryName(process.MainModule.FileName)}");
                            break;
                    }

                    break;
                }

                SLEEP:
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
                    //DiscordRpc.ClearPresence();
                    UpdatePresence();
                };
                LocationUnderNameItem.Click += (sender, args) =>
                {
                    //TODO saving and loading from file
                    LocationUnderNameItem.Checked = !LocationUnderNameItem.Checked;
                    //DiscordRpc.ClearPresence();
                    UpdatePresence();
                };
                ChangeNameItem.Click += (sender, args) =>
                {
                    var prompt = new Form
                    {
                        Width = 100,
                        Height = 50,
                        FormBorderStyle = FormBorderStyle.FixedToolWindow,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    var textBox = new TextBox { Width=105 };
                    textBox.KeyDown += (a, b) =>
                    {
                        if (b.KeyCode == Keys.Enter)
                        {
                            b.SuppressKeyPress = true;
                            
                            if (!string.IsNullOrEmpty(textBox.Text))
                            {
                                Player.Name = textBox.Text;
                            }                                                    
                            
                            MessageBox.Show(string.IsNullOrEmpty(textBox.Text)
                                ? "No name entered."
                                : "New name: " + textBox.Text);
                            
                            characterItem.Text = "Character: " + Player.Name;
                            
                            prompt.Close();
                        }
                    };
                    prompt.Controls.Add(textBox);
                    prompt.ShowDialog();
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
                MessageBox.Show("discord-rpc.dll missing!");
                Application.Exit();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }
}
