namespace YoutuBot.Models
{
    public class YoutubeVideoCommentInfo
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string AuthorName { get; set; }
        public string[] AuthorThumbnails { get; set; }
        public string AuthorChannelId { get; set; }
        public string PublishedTime { get; set; }
        public string LikeCount { get; set; }
        public string AuthorIsChannelOwner { get; set; }
        public string ReplyCount { get; set; }

        //----------------------------
        //these two parameters will be used to fetch inner comments
        public string TokenContinuation { get; set; }
        public string TokenClickTrackingParams { get; set; }
        //----------------------------


        public override string ToString()
        {
            return AuthorName + " : " + Text;
        }
    }
}