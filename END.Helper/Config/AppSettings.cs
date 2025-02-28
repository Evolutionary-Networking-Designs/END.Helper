using System.Text.Json;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace END.Helper;

    [DebuggerStepThrough]
    public class AppSettings
    {
        public Logging Logging { get; set; } = new();
        public string AllowedHosts { get; set; } = "*";

        public Dictionary<string, string> DataSource { get; set; } = new();
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();

        public string Environment { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ShowError { get; set; } = "false";
        public string CipherKey { get; set; } = string.Empty;

        public AppSettings()
        {
            var appPath = Config.GetAppPath();
            var settingsFile = Path.Combine(appPath, "bin", "appsettings.json");
            var exists = File.Exists(settingsFile);

            if (exists) return;
            if (Helper.HasWriteAccessToFolder(appPath))
                WriteConfigFile(settingsFile);
        }

        private void WriteConfigFile(string settingsFile)
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            var jsonString = System.Text.Json.JsonSerializer.Serialize(this, options);
            File.WriteAllText(settingsFile, jsonString, System.Text.Encoding.UTF8);
        }
    }