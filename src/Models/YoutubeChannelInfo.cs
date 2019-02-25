using System.Collections.Generic;

namespace YoutuBot.Models
{
    public class YoutubeChannelInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Keywords { get; set; }
        public string Description { get; set; }
        public List<YoutubeChannelInfo> RelatedChannels { get; set; }
        public string SubscriptionCount { get; set; }
        public string[] Thumbnails { get; set; }
        public List<YoutubeChannelInfo> FriendChannels { get; set; }
        public string[] Tags { get; set; }
        public List<YoutubeVideoInfo> Uploads { get; set; }
        public string UserId { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name)) return Name;
            if (!string.IsNullOrEmpty(Description)) return Description;
            return Id;
        }
    }
}
