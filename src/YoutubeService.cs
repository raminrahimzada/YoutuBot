using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using YoutuBot.Models;

namespace YoutuBot
{
    public class YoutubeService : IYoutubeService
    {
        public IEnumerable<YoutubePlayListInfo> GetUserPlayLists(string userId)
        {
            var url = $"https://www.youtube.com/user/{userId}/playlists";

            var html = url.DownloadHTML();
            var ytInitialData = html.GetYtInitialData();
            var obj = JObject.Parse(ytInitialData);

            var content = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"][2]["tabRenderer"]["content"];
            var itemSectionRenderer =
                content["sectionListRenderer"]; //["itemSectionRenderer"];
            var playLists = itemSectionRenderer["contents"][0]["itemSectionRenderer"]
                ["contents"][0]
                ["gridRenderer"]?["items"].Select(c => c["gridPlaylistRenderer"]);
            if (playLists == null) yield break;

            foreach (var p in playLists)
            {
                YoutubePlayListInfo playListInfo = new YoutubePlayListInfo();
                playListInfo.Id = p["playlistId"] + string.Empty;
                playListInfo.Thumbnail = p["thumbnail"]["thumbnails"][0]["url"] + string.Empty;
                playListInfo.Title = p["title"]["runs"].Select(c => c["text"] + string.Empty).JoinWith("\n");
                playListInfo.VideoCount = p["videoCountText"]["simpleText"] + string.Empty;
                playListInfo.PublishedTime = p["publishedTimeText"]["simpleText"] + string.Empty;
                if (string.IsNullOrEmpty(playListInfo.VideoCount))
                    playListInfo.VideoCount = p["videoCountShortText"]["simpleText"] + string.Empty;
                playListInfo.SidebarThumbnails = p["sidebarThumbnails"]
                    ?.Select(c => c["thumbnails"][0]["url"] + string.Empty)
                    .ToArray();
                yield return playListInfo;
            }
        }

        public YoutubeVideoInfo GetVideo(string videoId)
        {
            var url = "https://www.youtube.com/watch?v=" + videoId;
            var pageSource = url.DownloadHTML();

            const string unavailableContainer = "<div id=\"watch-player-unavailable\">";

            var isUnavailable = pageSource.Contains(unavailableContainer);
            if (isUnavailable)
            {
                throw new Exception("VideoNotAvailable");
            }

            var dataRegex = new Regex(@"ytplayer\.config\s*=\s*(\{.+?\});", RegexOptions.Multiline);

            var m = dataRegex.Match(pageSource);
            if (!m.Success)
            {
                return null;
            }
            var extractedJson = dataRegex.Match(pageSource).Result("$1");
            var obj = JObject.Parse(extractedJson);
            YoutubeVideoInfo video=new YoutubeVideoInfo();
            video.Id = videoId;
            //var thumbnail_url = obj["thumbnail_url"] + string.Empty;
            video.Length = obj["length_seconds"] + string.Empty;
            //var url_encoded_fmt_stream_map = obj["url_encoded_fmt_stream_map"] + string.Empty;
            //var query = url_encoded_fmt_stream_map.ParseQueryString();
            var args = obj["args"];
            video.Author = args["author"]+string.Empty;
            var playerResponse = JObject.Parse(args["player_response"] + string.Empty);

            video.Title = args["title"]+string.Empty;
            video.Thumbnail = args["thumbnail_url"]+string.Empty;
            //var fflags = args["fflags"];
            //var relative_loudness = args["relative_loudness"];
            //var account_playback_token = args["account_playback_token"];
            //var url_encoded_fmt_stream_map = args["url_encoded_fmt_stream_map"];
            //var video_id = args["video_id"];
            //var adaptive_fmts = args["adaptive_fmts"].ToString().ParseQueryString().ToArray();
            video.Watermark = args["watermark"]+string.Empty;
            var videoDetails = playerResponse["videoDetails"];

            video.Title = videoDetails["title"] + string.Empty;
            video.Description = videoDetails["shortDescription"] + string.Empty;
            video.ViewsCount = videoDetails["viewCount"] + string.Empty;
            video.IsLiveContent = videoDetails["isLiveContent"] + string.Empty;
            video.ChannelId = videoDetails["channelId"] + string.Empty;


            var ytInitialData = pageSource.Split("\r\t\n".ToCharArray())
                .FirstOrDefault(l => l.Contains("window[\"ytInitialData\"] ="));
            if (!string.IsNullOrEmpty(ytInitialData))
            {
                ytInitialData = ytInitialData.RemoveThisFirst("window[\"ytInitialData\"] =").RemoveThisLast(";");
                var init = JObject.Parse(ytInitialData);
                var contents = init["contents"]["twoColumnWatchNextResults"]["results"]["results"]["contents"];
                //var videoPrimaryInfoRenderer = contents[0]["videoPrimaryInfoRenderer"];
                var videoSecondaryInfoRenderer = contents[1]["videoSecondaryInfoRenderer"];
                //var itemSectionRenderer = contents[2]["itemSectionRenderer"];


                video.UserId=videoSecondaryInfoRenderer["owner"]["videoOwnerRenderer"]["navigationEndpoint"]["commandMetadata"]
                    ["webCommandMetadata"]["url"].ToString().RemoveThis("/user/");

                video.PublishedTime= videoSecondaryInfoRenderer["dateText"]["simpleText"] + string.Empty;
                video.CategoryId =
                    videoSecondaryInfoRenderer["metadataRowContainer"]["metadataRowContainerRenderer"]["rows"][0]
                        ["metadataRowRenderer"]["contents"].Select(c => c["runs"][0]["text"] + string.Empty).FirstOrDefault();


                var twoColumnWatchNextResults =
                    init["contents"]["twoColumnWatchNextResults"]["secondaryResults"]["secondaryResults"]["results"];

                video.NextVideos=new List<YoutubeVideoInfo>();
                foreach (var twoColumnWatchNextResult in twoColumnWatchNextResults)
                {
                    var compactVideoRenderer = twoColumnWatchNextResult["compactVideoRenderer"];
                    if (compactVideoRenderer != null)
                    {
                        var nextVideo=new YoutubeVideoInfo();
                        nextVideo.Id = compactVideoRenderer["videoId"] + string.Empty;
                        nextVideo.Thumbnails = compactVideoRenderer["thumbnail"]["thumbnails"]
                            .Select(c => c["url"] + string.Empty).ToArray();
                        nextVideo.Title = compactVideoRenderer["title"]["simpleText"] + string.Empty;
                        nextVideo.RichThumbnail =
                            compactVideoRenderer["richThumbnail"]?["movingThumbnailRenderer"]?["movingThumbnailDetails"]?
                                ["thumbnails"]?.Select(c => c["url"] + string.Empty).FirstOrDefault();
                        nextVideo.ViewsCount = compactVideoRenderer["viewCountText"]["simpleText"] + string.Empty;
                        nextVideo.Length = compactVideoRenderer["lengthText"]["simpleText"] + string.Empty;
                        nextVideo.ChannelThumbnail = compactVideoRenderer["channelThumbnail"]["thumbnails"]
                            .Select(c => c["url"] + string.Empty).ToArray();
                        video.NextVideos.Add(nextVideo);
                    }
                }
            }

            var streamingData = playerResponse["streamingData"]["formats"];
            video.VideoStreams = new List<YoutubeVideoStreamInfo>();
            foreach (var vs in streamingData)
            {
                var stream=new YoutubeVideoStreamInfo();
                stream.ITag = vs["itag"] + string.Empty;
                stream.Url = vs["url"] + string.Empty;
                stream.Width = vs["width"] + string.Empty;
                stream.Height = vs["height"] + string.Empty;
                stream.LastModified = vs["lastModified"] + string.Empty;
                stream.ContentLength = vs["contentLength"] + string.Empty;
                stream.Quality = vs["quality"] + string.Empty;
                stream.QualityLabel = vs["qualityLabel"] + string.Empty;
                stream.ProjectionType = vs["projectionType"] + string.Empty;
                stream.AverageBitrate = vs["averageBitrate"] + string.Empty;
                stream.AudioQuality = vs["audioQuality"] + string.Empty;
                stream.ApproxDurationMs = vs["approxDurationMs"] + string.Empty;
                stream.AudioSampleRate = vs["audioSampleRate"] + string.Empty;
                stream.MimeType = vs["mimeType"] + string.Empty;
                video.VideoStreams.Add(stream);
            }
            return video;
        }

        public YoutubeChannelInfo GetChannelInfo(string channelId)
        {
            var url = $"https://www.youtube.com/channel/{channelId}?hl=en";

            YoutubeChannelInfo channel =new YoutubeChannelInfo();
            channel.Id = channelId;
            var html = url.DownloadHTML();
            channel.Name = html.GetStringBetweenAll("<title>", "</title>").FirstOrDefault(s=>s!="YouTube");
            channel.Keywords = html.GetStringBetween("<meta name=\"keywords\" content=\"", "\">");
            channel.Description = html.GetStringBetween("<meta property=\"og:description\" content=\"", "\">");
            channel.Tags = html.GetStringBetweenAll("<meta property=\"og:video:tag\" content=\"", "\">").ToArray();
            channel.UserId = html.GetStringBetween("/user/", "\"");

            var ytInitialData = html.GetYtInitialData();
            var obj = JObject.Parse(ytInitialData);
            var secondaryContents =
                obj["contents"]["twoColumnBrowseResultsRenderer"]["secondaryContents"][
                        "browseSecondaryContentsRenderer"]["contents"]
                    .Select(c => c["verticalChannelSectionRenderer"]["items"]).ToArray();
            var friendChannels = secondaryContents[0].Select(c => c["miniChannelRenderer"]);
            channel.FriendChannels = new List<YoutubeChannelInfo>();
            foreach (var friendChannel in friendChannels)
            {
                YoutubeChannelInfo innerChannel = new YoutubeChannelInfo();
                innerChannel.Id = friendChannel["channelId"] + string.Empty;
                innerChannel.Name = friendChannel["title"]["runs"][0]["text"] + string.Empty;
                innerChannel.SubscriptionCount = friendChannel["subscriberCountText"]["simpleText"] + string.Empty;
                innerChannel.Thumbnails = friendChannel["thumbnail"]["thumbnails"]
                    .Select(c => c["url"] + string.Empty).ToArray();
                channel.FriendChannels.Add(innerChannel);
            }

            if (secondaryContents.Length == 2)
            {
                var relatedChannels = secondaryContents[1].Select(c => c["miniChannelRenderer"]);
                channel.RelatedChannels = new List<YoutubeChannelInfo>();
                foreach (var relatedChannel in relatedChannels)
                {
                    YoutubeChannelInfo innerChannel = new YoutubeChannelInfo();
                    innerChannel.Id = relatedChannel["channelId"] + string.Empty;
                    innerChannel.Name = relatedChannel["title"]["runs"][0]["text"] + string.Empty;
                    innerChannel.SubscriptionCount = relatedChannel["subscriberCountText"]["simpleText"] + string.Empty;
                    innerChannel.Thumbnails = relatedChannel["thumbnail"]["thumbnails"]
                        .Select(c => c["url"] + string.Empty).ToArray();
                    channel.RelatedChannels.Add(innerChannel);
                }
            }

            var tabs = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"].Select(c => c["tabRenderer"])
                .Where(t => t != null);
            foreach (var tab in tabs)
            {
                var _title = tab["title"] + string.Empty;
                if (_title == "Home")
                {
                    channel.Uploads = new List<YoutubeVideoInfo>();
                    var contents = tab["content"]["sectionListRenderer"]["contents"];
                    var items = contents.Select(c =>
                                c["itemSectionRenderer"]["contents"][0])
                            .Select(c => c["channelVideoPlayerRenderer"] ?? c["shelfRenderer"])
                        ;

                    foreach (var channelVideoPlayerRenderer in items)
                    {
                        YoutubeVideoInfo video = new YoutubeVideoInfo();

                        video.Id = channelVideoPlayerRenderer["videoId"] + string.Empty;

                        video.Title = channelVideoPlayerRenderer["title"]["runs"].Select(c => c["text"])
                            .JoinWith("\n");

                        if (channelVideoPlayerRenderer["description"] != null)
                        {
                            if (channelVideoPlayerRenderer["description"]["simpleText"] != null)
                            {
                                video.Description =
                                    channelVideoPlayerRenderer["description"]["simpleText"] + string.Empty;
                            }
                            else
                            {
                                video.Description = channelVideoPlayerRenderer["description"]["runs"]
                                    .Select(c => c["text"])
                                    .JoinWith("\n");
                            }
                        }

                        video.ViewsCount =
                            channelVideoPlayerRenderer["viewCountText"]?["simpleText"] + string.Empty;

                        video.PublishedTime =
                            channelVideoPlayerRenderer["publishedTimeText"]?["simpleText"] + string.Empty;
                        channel.Uploads.Add(video);
                    }
                }
            }
            return channel;
        }
        public YoutubePlayListInfo GetPlayList(string playlistId,int maxItemsCount=0)
        {
            var url =
                $"https://www.youtube.com/list_ajax?style=json&action_get_list=1&list={playlistId}&index={maxItemsCount}&hl=en";
            var jsonString = url.DownloadHTML();
            var obj = JObject.Parse(jsonString);
            YoutubePlayListInfo playListInfo = new YoutubePlayListInfo();
            playListInfo.Title = obj["title"] + string.Empty;
            playListInfo.Description = obj["description"] + string.Empty;
            playListInfo.AuthorName = obj["author"] + string.Empty;
            playListInfo.Views = obj["views"] + string.Empty;
            JArray videos = (JArray)obj["video"];

            playListInfo.Videos = new List<YoutubeVideoInfo>();
            foreach (var v in videos)
            {
                YoutubeVideoInfo video = new YoutubeVideoInfo();
                video.CommentsCount = v["comments"] + string.Empty;
                video.ViewsCount = v["views"] + string.Empty;
                video.CCLicense = v["cc_license"] + string.Empty;
                video.Rating = v["rating"] + string.Empty;
                video.Keywords = v["keywords"] + string.Empty;
                video.LikesCount = v["likes"] + string.Empty;
                video.CategoryId = v["category_id"] + string.Empty;
                video.DislikesCount = v["dislikes"] + string.Empty;
                video.Title = v["title"] + string.Empty;
                video.AddedOn = v["added"] + string.Empty;
                video.Id = v["encrypted_id"] + string.Empty;
                video.TimeCreated = v["time_created"] + string.Empty;
                video.Description = v["description"] + string.Empty;
                video.Length = v["length_seconds"] + string.Empty;
                video.UserId = v["user_id"] + string.Empty;
                video.Thumbnail = v["thumbnail"] + string.Empty;
                video.Privacy = v["privacy"] + string.Empty;
                video.IsCC = v["is_cc"] + string.Empty;
                video.Duration = v["duration"] + string.Empty;
                video.Author = v["author"] + string.Empty;
                video.IsHD = v["is_hd"] + string.Empty;
                playListInfo.Videos.Add(video);
            }

            return playListInfo;

        }



        public  IEnumerable<YoutubeVideoCommentInfo[]> GetRootComments(string videoId)
        {
            string session_token, itct, visitorInfo1Live, xsrf_token, ysc;
            YoutubeHelpers.ParseYoutubeVideoTokens(videoId, out visitorInfo1Live, out ysc, out xsrf_token, out session_token, out itct);
            //equal in beginning
            string continuationDefault = xsrf_token;
            //here contains lots of other data and some tokens for getting other comments
            var response = YoutubeHelpers.GetYoutubeComments(videoId, continuationDefault, itct, session_token, visitorInfo1Live, ysc);
            //yield return DebugCommentsResponse(response);

            JObject itemSectionContinuation = (JObject)response["response"]["continuationContents"]["itemSectionContinuation"];
            //var serviceTrackingParams = response["response"]["responseContext"]["serviceTrackingParams"];
            //var trackingParams = itemSectionContinuation["trackingParams"] + string.Empty;

            while (true)
            {
                var continuations = (JArray)itemSectionContinuation["continuations"];
                //if (continuations == null) break;
                if (continuations == null)
                {
                    JArray contents = (JArray)itemSectionContinuation["contents"];
                    var latestRemainingComments = YoutubeHelpers.ParseCommentsResponse(contents);
                    yield return latestRemainingComments;
                    break;
                }
                var nextContinuationData = continuations[0]["nextContinuationData"];
                var continuationFirst = nextContinuationData["continuation"] + string.Empty;
                var clickTrackingParams = nextContinuationData["clickTrackingParams"] + string.Empty;
                response = YoutubeHelpers.GetYoutubeComments(videoId,
                    continuationFirst,
                    clickTrackingParams, session_token, visitorInfo1Live, ysc);
                itemSectionContinuation =
                    (JObject)response["response"]["continuationContents"]["itemSectionContinuation"];
                var current = YoutubeHelpers.ParseCommentsResponse((JArray)response["response"]["continuationContents"]["itemSectionContinuation"]["contents"]);
                yield return current;
                if (current.Length == 0)
                {
                    break;
                }
            }
        }


        public IEnumerable<YoutubeVideoInfo> SearchForVideo(string query,int page)
        {
            var url = $"https://www.youtube.com/search_ajax?style=json&search_query={query}&page={page}&hl=en";
            var jsonString = url.DownloadHTML();
            var obj = JObject.Parse(jsonString);
            var videos = obj["video"];
            foreach (var v in videos)
            {
                YoutubeVideoInfo video = new YoutubeVideoInfo();
                video.CommentsCount = v["comments"] + string.Empty;
                video.ViewsCount = v["views"] + string.Empty;
                video.CCLicense = v["cc_license"] + string.Empty;
                video.Rating = v["rating"] + string.Empty;
                video.Keywords = v["keywords"] + string.Empty;
                video.LikesCount = v["likes"] + string.Empty;
                video.CategoryId = v["category_id"] + string.Empty;
                video.DislikesCount = v["dislikes"] + string.Empty;
                video.Title = v["title"] + string.Empty;
                video.AddedOn = v["added"] + string.Empty;
                video.Id = v["encrypted_id"] + string.Empty;
                video.TimeCreated = v["time_created"] + string.Empty;
                video.Description = v["description"] + string.Empty;
                video.Length = v["length_seconds"] + string.Empty;
                video.UserId = v["user_id"] + string.Empty;
                video.Thumbnail = v["thumbnail"] + string.Empty;
                video.Privacy = v["privacy"] + string.Empty;
                video.IsCC = v["is_cc"] + string.Empty;
                video.Duration = v["duration"] + string.Empty;
                video.Author = v["author"] + string.Empty;
                video.IsHD = v["is_hd"] + string.Empty;
                yield return video;
            }
        }

        public IEnumerable<YoutubeVideoInfo> GetTrending(string countryCode)
        {
            var url = "https://www.youtube.com/feed/trending";
            if (!string.IsNullOrEmpty(countryCode))
            {
                url += "?gl=" + countryCode;
            }
            var html = url.DownloadHTML();
            var obj = JObject.Parse(html.GetYtInitialData());
            var itemsAll = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"][0]["tabRenderer"]["content"][
                    "sectionListRenderer"]
                ["contents"].SelectMany(c =>
                    c["itemSectionRenderer"]["contents"][0]["shelfRenderer"]["content"]["expandedShelfContentsRenderer"]
                        ["items"]
                ).Select(c => c["videoRenderer"]);
            foreach (var v in itemsAll)
            {
                var video = new YoutubeVideoInfo();
                video.Id = v["videoId"] + string.Empty;
                video.Thumbnails = v["thumbnail"]["thumbnails"]
                    .Select(c => c["url"] + string.Empty).ToArray();
                video.Title = v["title"]["simpleText"] + string.Empty;
                video.RichThumbnail =
                    v["richThumbnail"]?["movingThumbnailRenderer"]?["movingThumbnailDetails"]?
                        ["thumbnails"]?.Select(c => c["url"] + string.Empty).FirstOrDefault();
                video.ViewsCount = v["viewCountText"]["simpleText"] + string.Empty;
                video.Length = v["lengthText"]["simpleText"] + string.Empty;
                video.ChannelThumbnail = 
                    v["channelThumbnailSupportedRenderers"]
                    ["channelThumbnailWithLinkRenderer"]["thumbnail"]
                    ["thumbnails"]
                    .Select(c => c["url"] + string.Empty).ToArray();
                yield return video;
            }
        }

        public IEnumerable<KeyValuePair<string, YoutubeChannelInfo[]>> BrowseChannels()
        {
            var url = "https://www.youtube.com/feed/guide_builder";
            var jsonString = url.DownloadHTML().GetYtInitialData();
            var obj = JObject.Parse(jsonString);

            var itemsAll = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"][0]["tabRenderer"]["content"][
                    "sectionListRenderer"]
                ["contents"].Select(c =>
                    c["itemSectionRenderer"]["contents"][0]["shelfRenderer"]);

            foreach (var item in itemsAll)
            {
                var title = item["title"]["simpleText"] + String.Empty;
                List<YoutubeChannelInfo> results = new List<YoutubeChannelInfo>();
                var channels = item["content"]["horizontalListRenderer"]["items"].Select(c => c["gridChannelRenderer"]);
                foreach (var c in channels)
                {
                    YoutubeChannelInfo channel=new YoutubeChannelInfo();
                    channel.Id = c["channelId"] + string.Empty;
                    channel.Thumbnails = c["thumbnail"]["thumbnails"].Select(t=>t["url"]+string.Empty).ToArray();
                    channel.SubscriptionCount = c["subscriberCountText"]?["simpleText"] + string.Empty;
                    channel.Name = c["title"]["simpleText"] + String.Empty;
                    results.Add(channel);
                }

                yield return new KeyValuePair<string, YoutubeChannelInfo[]>(title, results.ToArray());
            }
        }
    }
}
