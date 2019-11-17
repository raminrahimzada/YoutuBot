using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using YoutuBot.Models;

namespace YoutuBot
{
    public static class YoutubeHelpers
    {
        public static YoutubeVideoCommentInfo[] ParseCommentsResponse(JArray contents)
        {
            //gets all comments
            //var continuationContents = response["response"]["continuationContents"]["itemSectionContinuation"]["contents"];

            //var itemSectionContinuation = continuationContents["itemSectionContinuation"];
            List<YoutubeVideoCommentInfo> allRootComments = new List<YoutubeVideoCommentInfo>();
            foreach (var content in contents)
            {
                YoutubeVideoCommentInfo commentInfo = new YoutubeVideoCommentInfo();
                commentInfo.Extra = content.ToString();
                var c = content["commentThreadRenderer"]["comment"]["commentRenderer"];
                commentInfo.Id= c["commentId"] + string.Empty;
                commentInfo.Text= c["contentText"]["simpleText"] + string.Empty;
                if (string.IsNullOrEmpty(commentInfo.Text))
                {
                    commentInfo.Text = c["contentText"]?["runs"]?[0]?["text"] + string.Empty;
                }

                if (string.IsNullOrEmpty(commentInfo.Text))
                {
                    ;
                }

                    commentInfo.AuthorName= c["authorText"]?["simpleText"] + string.Empty;
                commentInfo.AuthorThumbnails =
                    c["authorThumbnail"]?["thumbnails"].Select(cc => cc["url"] + string.Empty).ToArray();
                commentInfo.AuthorChannelId = c["authorEndpoint"]["commandMetadata"]["webCommandMetadata"]["url"].ToString()
                    .RemoveThisFirst("/channel/");
                commentInfo.LikeCount = c["likeCount"] + string.Empty;
                commentInfo.ReplyCount = c["publishedTimeText"]["replyCount"] + string.Empty;
                commentInfo.AuthorIsChannelOwner = c["publishedTimeText"]["authorIsChannelOwner"] + string.Empty;
                commentInfo.PublishedTime = c["publishedTimeText"]["runs"].Select(r => r["text"] + string.Empty)
                    .FirstOrDefault();
                var replies = c["replies"];
                if (replies != null)
                {
                    var continuations = replies["commentRepliesRenderer"]["continuations"].FirstOrDefault();
                    if (continuations != null)
                    {
                        commentInfo.TokenContinuation = continuations["nextContinuationData"]["continuation"] + string.Empty;
                        commentInfo.TokenClickTrackingParams = continuations["nextContinuationData"]["clickTrackingParams"] + string.Empty;
                    }
                }
                allRootComments.Add(commentInfo);
            }

            return allRootComments.ToArray();
        }
        public static string GetYtInitialData(this string html)
        {
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

                return line;
            }

            return string.Empty;
        }

        public static void ParseYoutubeVideoTokens(string videoId, out string visitorInfo1Live, out string ysc,
            out string ctoken, out string session_token, out string itct)
        {
            var youtubeVideoUrl = "https://www.youtube.com/watch?v=" + videoId;
            var request = (HttpWebRequest) WebRequest.Create(youtubeVideoUrl);
            request.Headers.Add("accept-language", "en-US,en;q=0.9,az;q=0.8,ru;q=0.7");
            //request.Headers.Add("accept-encoding", "gzip, deflate, br");
            request.Headers.Add("cache-control", "max-age=0");
            request.Headers.Add("upgrade-insecure-requests", "1");
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.119 Safari/537.36";
            request.Method = "GET";

            request.Headers.Add("authority", "www.youtube.com");
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            var responseObj = (HttpWebResponse) request.GetResponse();
            request.CookieContainer = new CookieContainer();

            var cookieHeader = responseObj.Headers["set-cookie"];

            //VISITOR_INFO1_LIVE=qVHupn93XTk; path=/; domain=.youtube.com; expires=Thu, 22-Aug-2019 16:20:04 GMT; httponly,YSC=5GvBabGzfnc; path=/; domain=.youtube.com; httponly,PREF=f1=50000000; path=/; domain=.youtube.com; expires=Fri, 25-Oct-2019 04:13:04 GMT,GPS=1; path=/; domain=.youtube.com; expires=Sat, 23-Feb-2019 16:50:04 GMT
            visitorInfo1Live = cookieHeader.GetStringBetween("VISITOR_INFO1_LIVE=", ";");
            ysc = cookieHeader.GetStringBetween("YSC=", ";");

            var stream = responseObj.GetResponseStream();
            if (stream == null)
            {
                ctoken = null;
                session_token = null;
                itct = null;
                return;
            }
            var html = new StreamReader(stream).ReadToEnd();
            ctoken = html.GetStringBetween("{\"nextContinuationData\":{\"continuation\":\"", "\"");
            session_token = html.GetStringBetween(",\"XSRF_TOKEN\":\"", "\"");
            itct = html.GetStringBetween("itct%3D", "%253D");
        }

        public static JObject GetYoutubeCommentsSafe(string videoId, string continuation, string itct, string session_token,
            string visitorInfo1Live, string ysc)
        {
            int maxRetry = 5;
            int i = 0;
            start:
            try
            {
                return GetYoutubeComments(videoId, continuation, itct, session_token, visitorInfo1Live, ysc);
            }
            catch (Exception e)
            {
                C.WriteLine("-------------------------------------------");
                C.WriteLine(e.Message);
                Thread.Sleep(TimeSpan.FromSeconds(5));
                i++;
                if (i < maxRetry) goto start;
                else
                {
                    throw;
                }
            }
        }
        static JObject GetYoutubeComments(string videoId, string continuation, string itct, string session_token,
            string visitorInfo1Live, string ysc)
        {
            Uri uri = new Uri("https://www.youtube.com");
            var commentUrl =
                $"https://www.youtube.com/comment_service_ajax?action_get_comments=1&pbj=1&ctoken={continuation}&continuation={continuation}&itct={itct}";

            var request = (HttpWebRequest) WebRequest.Create(commentUrl);
            var postData = "session_token=" + session_token.UrlEncode();
            var data = Encoding.ASCII.GetBytes(postData);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(uri, new Cookie("GPS", "1"));
            request.CookieContainer.Add(uri, new Cookie("PREF", "f1=50000000"));
            request.CookieContainer.Add(uri, new Cookie("VISITOR_INFO1_LIVE", visitorInfo1Live));
            request.CookieContainer.Add(uri, new Cookie("YSC", ysc));
            request.Method = "POST";
            //request.Timeout = -1;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "*/*";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = data.Length;
            request.Headers.Add("x-youtube-client-name", "1");
            request.Headers.Add("x-youtube-client-version", "2.20190221");
#if true
            request.Headers.Add("x-spf-previous", "https://www.youtube.com/watch?v=" + videoId);
            request.Headers.Add("x-spf-referer", "https://www.youtube.com/watch?v=" + videoId);
            //request.Headers.Add("referer", "https://www.youtube.com/watch?v=" + videoId);
            request.Referer = "https://www.youtube.com/watch?v=" + videoId;
            request.Headers.Add("x-youtube-page-cl", "235088990");
            request.Headers.Add("x-youtube-page-label", "youtube.ytfe.desktop_20190220_7_RC2");
            request.Headers.Add("x-youtube-utc-offset", "-480");
            request.Headers.Add("x-youtube-variants-checksum", "3b505208894dc737ec75a377127f327a");
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.Headers.Add("authority", "www.youtube.com");
            request.Proxy = new WebProxy( /* can't present value of type System.Net.WebProxy */);
            request.Headers.Add("origin", "https://www.youtube.com");
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
#endif
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responseRaw = request.GetResponse();
            if (responseRaw is HttpWebResponse responseObj)
            {
                var responseStream = responseObj.GetResponseStream();
                if (responseStream == null) return null;
                using (var reader = new StreamReader(responseStream))
                {
                    var responseString = reader.ReadToEnd();
                    responseObj.Close();
                    responseObj.Dispose();
                    var response = JObject.Parse(responseString);
                    return response;
                }
            }

            return null;
        }
    }
}