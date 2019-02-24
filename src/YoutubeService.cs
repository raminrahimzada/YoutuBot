using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace YoutuBot
{
    public class YoutubeService : IYoutubeService
    {
        public IEnumerable<YoutubePlayList> GetUserPlayLists(string userId)
        {
            var url = $"https://www.youtube.com/user/{userId}/playlists";

            var html = url.DownloadHTML();
            
            StringReader reader = new StringReader(html);
            string line;
            while (null != (line = reader.ReadLine()))
            {
                var pattern = "window[\"ytInitialData\"] =";
                if (!line.Contains(pattern)) continue;
                line = line.RemoveThis(pattern);
                if (line.EndsWith(";"))
                {
                    line = line.ReplaceLast(";", "");
                }

                var obj = JObject.Parse(line);
                var content = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"][2]["tabRenderer"]["content"];
                var itemSectionRenderer =
                    content["sectionListRenderer"];//["itemSectionRenderer"];
                var playLists = itemSectionRenderer["contents"][0]["itemSectionRenderer"]
                    ["contents"][0]
                    ["gridRenderer"]?["items"].Select(c => c["gridPlaylistRenderer"]);
                if(playLists!=null)
                    foreach (var p in playLists)
                    {
                        YoutubePlayList playList = new YoutubePlayList();

                        playList.Id = p["playlistId"] + String.Empty;
                        playList.Thumbnail = p["thumbnail"]["thumbnails"][0]["url"] + string.Empty;
                        var title = p["title"]["runs"].Select(c => c["text"] + string.Empty).JoinWith("\n");
                        playList.VideoCount = p["videoCountText"]["simpleText"] + String.Empty;
                        playList.PublishedTime = p["publishedTimeText"]["simpleText"] + String.Empty;
                        if(string.IsNullOrEmpty(playList.VideoCount)) playList.VideoCount = p["videoCountShortText"]["simpleText"] + String.Empty;
                        playList.SidebarThumbnails = p["sidebarThumbnails"]?.Select(c => c["thumbnails"][0]["url"] + string.Empty)
                            .ToArray();
                        yield return playList;
                    }
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
            var player_response = JObject.Parse(args["player_response"] + string.Empty);

            video.Title = args["title"]+string.Empty;
            video.Thumbnail = args["thumbnail_url"]+string.Empty;
            //var fflags = args["fflags"];
            //var relative_loudness = args["relative_loudness"];
            //var account_playback_token = args["account_playback_token"];
            //var url_encoded_fmt_stream_map = args["url_encoded_fmt_stream_map"];
            //var video_id = args["video_id"];
            //var adaptive_fmts = args["adaptive_fmts"].ToString().ParseQueryString().ToArray();
            video.Watermark = args["watermark"]+string.Empty;
            var videoDetails = player_response["videoDetails"];

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

                video.PublishedTime= videoSecondaryInfoRenderer["dateText"]["simpleText"] + String.Empty;
                video.CategoryId =
                    videoSecondaryInfoRenderer["metadataRowContainer"]["metadataRowContainerRenderer"]["rows"][0]
                        ["metadataRowRenderer"]["contents"].Select(c => c["runs"][0]["text"] + String.Empty).FirstOrDefault();


                var twoColumnWatchNextResults =
                    init["contents"]["twoColumnWatchNextResults"]["secondaryResults"]["secondaryResults"]["results"];

                video.NextVideos=new List<YoutubeVideoInfo>();
                foreach (var twoColumnWatchNextResult in twoColumnWatchNextResults)
                {
                    var compactVideoRenderer = twoColumnWatchNextResult["compactVideoRenderer"];
                    if (compactVideoRenderer != null)
                    {
                        var nextVideo=new YoutubeVideoInfo();
                        nextVideo.Id = videoId;
                        nextVideo.Thumbnails = compactVideoRenderer["thumbnail"]["thumbnails"]
                            .Select(c => c["url"] + string.Empty).ToArray();
                        nextVideo.Title = compactVideoRenderer["title"]["simpleText"] + string.Empty;
                        nextVideo.RichThumbnail =
                            compactVideoRenderer["richThumbnail"]["movingThumbnailRenderer"]["movingThumbnailDetails"]
                                ["thumbnails"].Select(c => c["url"] + string.Empty).FirstOrDefault();
                        nextVideo.ViewsCount = compactVideoRenderer["viewCountText"]["simpleText"] + string.Empty;
                        nextVideo.Length = compactVideoRenderer["lengthText"]["simpleText"] + string.Empty;
                        nextVideo.ChannelThumbnail = compactVideoRenderer["channelThumbnail"]["thumbnails"]
                            .Select(c => c["url"] + string.Empty).ToArray();
                        video.NextVideos.Add(nextVideo);
                    }
                }
            }

            var streamingData = player_response["streamingData"]["formats"];
            video.VideoStreams = new List<YoutubeVideoStream>();
            foreach (var vs in streamingData)
            {
                var stream=new YoutubeVideoStream();
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

            StringReader reader = new StringReader(html);
            string line;
            while (null != (line = reader.ReadLine()))
            {
                var pattern = "window[\"ytInitialData\"] =";
                if (!line.Contains(pattern)) continue;
                line = line.RemoveThis(pattern);
                if (line.EndsWith(";"))
                {
                    line = line.ReplaceLast(";", "");
                }
                var obj = JObject.Parse(line);
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
                    innerChannel.Name = friendChannel["title"]["runs"][0]["text"] + String.Empty;
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
                        YoutubeChannelInfo innerChannel=new YoutubeChannelInfo();
                        innerChannel.Id = relatedChannel["channelId"]+string.Empty;
                        innerChannel.Name = relatedChannel["title"]["runs"][0]["text"] + String.Empty;
                        innerChannel.SubscriptionCount = relatedChannel["subscriberCountText"]["simpleText"] + string.Empty;
                        innerChannel.Thumbnails = relatedChannel["thumbnail"]["thumbnails"]
                            .Select(c => c["url"] + string.Empty).ToArray();
                        channel.RelatedChannels.Add(innerChannel);
                    }
                }

                var tabs = obj["contents"]["twoColumnBrowseResultsRenderer"]["tabs"].Select(c=>c["tabRenderer"])
                    .Where(t=>t!=null);
                foreach (var tab in tabs)
                {
                    var _title = tab["title"]+string.Empty;
                    if (_title == "Home")
                    {
                        channel.Uploads=new List<YoutubeVideoInfo>();
                        var contents = tab["content"]["sectionListRenderer"]["contents"];
                        var items = contents.Select(c =>
                                    c["itemSectionRenderer"]["contents"][0])
                                .Select(c=>c["channelVideoPlayerRenderer"] ?? c["shelfRenderer"])
                            ;

                        foreach (var channelVideoPlayerRenderer in items)
                        {
                            YoutubeVideoInfo video=new YoutubeVideoInfo();
                            
                            video.Id= channelVideoPlayerRenderer["videoId"] + string.Empty;

                            video.Title = channelVideoPlayerRenderer["title"]["runs"].Select(c => c["text"])
                                .JoinWith("\n");

                            if (channelVideoPlayerRenderer["description"]!=null)
                            {
                                if (channelVideoPlayerRenderer["description"]["simpleText"] != null)
                                {
                                    video.Description =
                                        channelVideoPlayerRenderer["description"]["simpleText"] + String.Empty;
                                }
                                else
                                {
                                    video.Description = channelVideoPlayerRenderer["description"]["runs"].Select(c => c["text"])
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
                    else
                    {
                        int errr = 1;
                    }
                }
                break;

            }

            return channel;
        }
        public YoutubePlayList GetPlayList(string playlistId,int maxItemsCount=0)
        {
            var url =
                $"https://www.youtube.com/list_ajax?style=json&action_get_list=1&list={playlistId}&index={maxItemsCount}&hl=en";
            var jsonString = url.DownloadHTML();
            var obj = JObject.Parse(jsonString);
            YoutubePlayList playList = new YoutubePlayList();
            playList.Title = obj["title"] + String.Empty;
            playList.Description = obj["description"] + String.Empty;
            playList.AuthorName = obj["author"] + String.Empty;
            playList.Views = obj["views"] + String.Empty;
            JArray videos = (JArray)obj["video"];

            playList.Videos = new List<YoutubeVideoInfo>();
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
                playList.Videos.Add(video);
            }

            return playList;

        }
    }
}
