using System.Collections.Generic;
using YoutuBot.Models;

namespace YoutuBot
{
    public interface IYoutubeService
    {
        YoutubeChannelInfo GetChannelInfo(string channelId);
        YoutubePlayList GetPlayList(string playlistId, int maxItemsCount = 0);
        IEnumerable<YoutubePlayList> GetUserPlayLists(string userId);
        YoutubeVideoInfo GetVideo(string videoId);
        /// <summary>
        /// Comments are fetching 20 by 20 so each enumeration causes new request and takes time
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        IEnumerable<YoutubeVideoComment[]> GetRootComments(string videoId);
    }
}
