using System.Collections.Generic;
using System.Linq;

namespace AppAudioSwitcherUtility.Utils
{
// Source - https://stackoverflow.com/a
// Posted by weeksdev
// Retrieved 2026-01-10, License - CC BY-SA 4.0

    public class CommandLineParser
    {
        private readonly List<string> _args;

        public CommandLineParser(string[] args)
        {
            _args = args.ToList();
        }

        public string GetStringArgument(string key, char shortKey, out bool found)
        {
            var index = _args.IndexOf("--" + key);

            if (index >= 0 && _args.Count > index)
            {
                found = true;
                return _args[index + 1];
            }

            index = _args.IndexOf("-" + shortKey);

            if (index >= 0 && _args.Count > index)
            {
                found = true;
                return _args[index + 1];
            }

            found = false;
            return "";
        }

        public bool HasStringKey(string key, char shortKey)
        {
            int index = _args.IndexOf("--" + key);

            if (index >= 0 && _args.Count > index)
            {
                return true;
            }
            
            index = _args.IndexOf("-" + shortKey);
            
            return index >= 0;
        }

        public string GetFirstKey()
        {
            if (_args.Count <= 0) return "";
            foreach (string s in _args)
            {
                if (s.StartsWith("--"))
                {
                    string key = s.Substring(2);
                    return key;
                }
                else if (s.StartsWith("-"))
                {
                    string key = s.Substring(1);
                    return key;
                }
            }

            return "";
        }

        public bool GetSwitchArgument(bool value, char shortKey)
        {
            return _args.Contains("--" + value) || _args.Contains("-" + shortKey);
        }
    }
}