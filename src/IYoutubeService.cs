using System.Collections.Generic;

namespace YoutuBot
{
    public interface IYoutubeService
    {
        YoutubeChannelInfo GetChannelInfo(string channelId);
        YoutubePlayList GetPlayList(string playlistId, int maxItemsCount = 0);
        IEnumerable<YoutubePlayList> GetUserPlayLists(string userId);
        YoutubeVideoInfo GetVideo(string videoId);
    }
}
