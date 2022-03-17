using YamlDotNet.Serialization;

namespace KodiAddonLogCollector
{
    internal class CrashLogV1
    {
        public string Addon { get; set; }
        public string Version { get; set; }
        public string IPAddress { get; set; }
        public string Error { get; set; }
        [YamlMember(ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal)]
        public string Traceback { get; set; }
        public HostInfo HostInfo { get; set; }

        public string GetNotificationText() =>
            $"*Addon:* {Addon}\n" +
            $"*Version:* {Version}\n" +
            $"*IPAddress:* {IPAddress}\n" +
            $"*Error:* {Error}";
    }

    internal class HostInfo
    {
        public Host Host { get; set; }
        public Python Python { get; set; }
    }

    internal class Host
    {
        public string System { get; set; }
        public string Node { get; set; }
        public string Release { get; set; }
        public string Version { get; set; }
        public string Machine { get; set; }
        public string Processor { get; set; }
    }

    internal class Python
    {
        public string Implementation { get; set; }
        public string Version { get; set; }
        public string Compiler { get; set; }
    }
}
