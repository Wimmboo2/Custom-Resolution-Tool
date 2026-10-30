using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Crt
{
    internal sealed class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string Get(string section, string key, string fallback)
        {
            Dictionary<string, string> values;
            if (!_sections.TryGetValue(section, out values))
            {
                return fallback;
            }

            string value;
            if (!values.TryGetValue(key, out value))
            {
                return fallback;
            }

            return value;
        }

        public int GetInt(string section, string key, int fallback)
        {
            int value;
            if (int.TryParse(Get(section, key, string.Empty), out value))
            {
                return value;
            }

            return fallback;
        }

        public void Set(string section, string key, string value)
        {
            Dictionary<string, string> values;
            if (!_sections.TryGetValue(section, out values))
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _sections[section] = values;
            }

            values[key] = value;
        }

        public void SetInt(string section, string key, int value)
        {
            Set(section, key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public bool HasSection(string section)
        {
            return _sections.ContainsKey(section);
        }

        public void RemoveSection(string section)
        {
            _sections.Remove(section);
        }

        public static IniFile Load(string path)
        {
            IniFile ini = new IniFile();
            if (!File.Exists(path))
            {
                return ini;
            }

            string section = string.Empty;
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                ini.Set(section, key, value);
            }

            return ini;
        }

        public void Save(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                List<string> sections = new List<string>(_sections.Keys);
                sections.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (string section in sections)
                {
                    if (section.Length > 0)
                    {
                        writer.WriteLine("[" + section + "]");
                    }

                    Dictionary<string, string> values = _sections[section];
                    List<string> keys = new List<string>(values.Keys);
                    keys.Sort(StringComparer.OrdinalIgnoreCase);

                    foreach (string key in keys)
                    {
                        writer.WriteLine(key + "=" + values[key]);
                    }

                    writer.WriteLine();
                }
            }
        }
    }

    internal static class StateStore
    {
        public const string Section = "last";

        public static string DirectoryPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "crt");
            }
        }

        public static string FilePath
        {
            get
            {
                return Path.Combine(DirectoryPath, "state.ini");
            }
        }

        public static IniFile Load()
        {
            return IniFile.Load(FilePath);
        }

        public static void Save(IniFile ini)
        {
            ini.Save(FilePath);
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
