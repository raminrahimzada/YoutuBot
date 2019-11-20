using System.Collections.Generic;

namespace YoutuBot.Models
{
    public class YoutubePlayListInfo
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string AuthorName { get; set; }
        public string Views { get; set; }
        public List<YoutubeVideoInfo> Videos { get; set; }
        public string Id { get; set; }
        public string Thumbnail { get; set; }
        public string VideoCount { get; set; }
        public string PublishedTime { get; set; }
        public string[] SidebarThumbnails { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Title)) return Title;
            if (!string.IsNullOrEmpty(Description)) return Description;
            return Id;
        }
    }
}