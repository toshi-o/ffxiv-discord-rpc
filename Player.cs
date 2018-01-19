using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharlayan.Core.Enums;

namespace FFXIV_discord_rpc
{
    public static class Player
    {
        public static bool _changed;
        public static string Name;

        private static int _level;
        public static int Level
        {
            get => _level;
            set
            {
                if (_level == value) return;
                var old = _level;
                _level = value;
                //Program.Msg($"Level: {old} -> {value}");
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
                //Program.Msg($"Job: {old} -> {value}");
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
                //Program.Msg($"Location: {old} -> {value}");
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
                //Program.Msg($"Status: {old} -> {value}");
                _changed = true;
            }
        }
    }
}
