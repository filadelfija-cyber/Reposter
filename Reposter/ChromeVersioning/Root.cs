using Newtonsoft.Json;
using System.Collections.Generic;

namespace Reposter.ChromeVersioning
{

    public class Root
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("channels")]
        public Channels Channels { get; set; }
    }

    public class Chrome
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Chromedriver
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class ChromeHeadlessShell
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Downloads
    {
        [JsonProperty("chrome")]
        public List<Chrome> Chrome { get; set; }

        [JsonProperty("chromedriver")]
        public List<Chromedriver> Chromedriver { get; set; }

        [JsonProperty("chrome-headless-shell")]
        public List<ChromeHeadlessShell> ChromeHeadlessShell { get; set; }
    }

    public class Stable
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }
    }

    public class Beta
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }
    }

    public class Dev
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }
    }

    public class Canary
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }
    }

    public class Channels
    {
        public Stable Stable { get; set; }

        public Beta Beta { get; set; }

        public Dev Dev { get; set; }

        public Canary Canary { get; set; }
    }
}
