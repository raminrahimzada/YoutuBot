using System.Collections.Generic;

namespace YoutuBot.Models
{
    public class YoutubeVideoInfo
    {
        public string Id { get; set; }

        public string IsHD;
        public string CCLicense;
        public string IsCC;
        public string CommentsCount { get; set; }
        public string ViewsCount { get; set; }
        public string Rating { get; set; }
        public string Keywords { get; set; }
        public string LikesCount { get; set; }
        public string CategoryId { get; set; }
        public string DislikesCount { get; set; }
        public string Title { get; set; }
        public string AddedOn { get; set; }
        public string TimeCreated { get; set; }
        public string Description { get; set; }
        public string Length { get; set; }
        public string UserId { get; set; }
        public string Thumbnail { get; set; }
        public string Privacy { get; set; }
        public string Duration { get; set; }
        public string Author { get; set; }
        public string PublishedTime { get; set; }
        public List<YoutubeVideoStreamInfo> VideoStreams { get; set; }
        public string Watermark { get; set; }
        public string IsLiveContent { get; set; }
        public string ChannelId { get; set; }
        public List<YoutubeVideoInfo> NextVideos { get; set; }
        public string[] Thumbnails { get; set; }
        public string RichThumbnail { get; set; }
        public string[] ChannelThumbnail { get; set; }


        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Title)) return Title;
            if (!string.IsNullOrEmpty(Description)) return Description;
            return Id;
        }
    }
}
