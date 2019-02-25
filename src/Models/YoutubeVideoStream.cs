namespace YoutuBot.Models
{
    public class YoutubeVideoStream
    {
        public string ITag { get; set; }
        public string Url { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string LastModified { get; set; }
        public string ContentLength { get; set; }
        public string Quality { get; set; }
        public string QualityLabel { get; set; }
        public string ProjectionType { get; set; }
        public string AverageBitrate { get; set; }
        public string AudioQuality { get; set; }
        public string ApproxDurationMs { get; set; }
        public string AudioSampleRate { get; set; }
        public string MimeType { get; set; }
    }
}
