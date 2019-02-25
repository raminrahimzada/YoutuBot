using System.Collections.Generic;
using YoutuBot.Models;

namespace YoutuBot
{
    public interface IYoutubeService
    {
        YoutubeChannelInfo GetChannelInfo(string channelId);
        YoutubePlayListInfo GetPlayList(string playlistId, int maxItemsCount = 0);
        IEnumerable<YoutubePlayListInfo> GetUserPlayLists(string userId);
        YoutubeVideoInfo GetVideo(string videoId);
        /// <summary>
        /// Comments are fetching 20 by 20 so each enumeration causes new request and takes time
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        IEnumerable<YoutubeVideoCommentInfo[]> GetRootComments(string videoId);

        IEnumerable<YoutubeVideoInfo> SearchForVideo(string query, int page);
        IEnumerable<YoutubeVideoInfo> GetTrending(string countryCode);
        IEnumerable<KeyValuePair<string, YoutubeChannelInfo[]>> BrowseChannels();
    }
}
