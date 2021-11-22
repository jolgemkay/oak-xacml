namespace oak_xacml.Data
{
    public class LogEntry
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public string TimeStamp {get; set;}
        public string requestFile {get; set;}
        public string decision {get; set;}
    }
}