using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using YoutuBot.Models;

namespace YoutuBot
{
    class Bot
    {
        public  IYoutubeService Service => C.Service;
         readonly ConcurrentBag<string> _cache=new ConcurrentBag<string>();
        public  object sync=new object();
        public  BlockingCollection<KeyValuePair<string, string>> _queue = new BlockingCollection<KeyValuePair<string, string>>();

        public  void WorkerThread()
        {
            foreach (var kv in _queue.GetConsumingEnumerable())
            {
                var channelId=kv.Key;
                var fromChannelId = kv.Value;
                Browse(channelId);
            }
        }
        public  void Browse(string channelId)
        {
            //lock (sync)
            {
                if (_cache.Contains(channelId)) return;
                
                C.WriteLine(" queue.size::" + _queue.Count);

                C.WriteLine("browsing channel with Id: " + channelId);
                var channel = Service.GetChannelInfo(channelId);
                SqlStorage.Save(channel);
                if (_cache.Contains(channelId)) return;
                _cache.Add(channelId);

                if (channel.FriendChannels != null)
                    foreach (var info in channel.FriendChannels)
                    {
                        if (!_cache.Contains(info.Id))
                        {
                            C.WriteLine("queueing related channel ... " + info.Id);
                            _queue.Add(new KeyValuePair<string, string>(info.Id, channelId));
                        }
                    }
            }
        }

        private readonly object _syncException = new object();
        private static int _newlyFoundVideosCount = 0;

        private void ExtendVideo(string videoId,bool useCache,int level)
        {
            Interlocked.Decrement(ref _newlyFoundVideosCount);
            if (level >= 5) return;
            try
            {
                if (useCache)
                {
                    if (_videoCache.Contains(videoId)) return;
                }
                C.WriteLine("working.videoId[{0}]:{1}", level, videoId);
                var video = Service.GetVideo(videoId);
                if (video == null)
                {
                    _videoCache.Add(videoId);
                    return;
                }
                if (useCache)
                {
                    if (_videoCache.Contains(videoId)) return;
                }
                SqlStorage.Save(video);
                _videoCache.Add(videoId);
                
                if (video.NextVideos != null)
                {
                    Interlocked.Add(ref _newlyFoundVideosCount, video.NextVideos?.Count ?? 0);
                    C.WriteLine("--- working.Found :" + _newlyFoundVideosCount);

                    foreach (var nv in video.NextVideos)
                    {
                        if (_videoCache.Contains(nv.Id)) continue;
                        var nvId = nv.Id;
                        var levelCopy = level+1;
                        ThreadPool.QueueUserWorkItem(x => ExtendVideo(nvId, true, levelCopy));
                    }
                }
            }
            catch (Exception e)
            {
                lock (_syncException)
                {
                    C.WriteLine("ERROR:" + videoId + " " + e.Message);
                    File.AppendAllLines("errors.txt", new[] {videoId});
                }
            }
        }

        private readonly ConcurrentBag<string> _videoCache = new ConcurrentBag<string>();

        public  void ExtendAllVideos()
        {
            C.WriteLine("loading videos from db..");
            var videoIds = Sql
                .Execute<string>(
                    "select  Id from Videos  order by NEWID() ")
                .ToList();
            foreach (var videoId in videoIds)
            {
                _videoCache.Add(videoId);
            }

            videoIds = videoIds.Take(100).ToList();

            C.WriteLine("loaded. count:"+videoIds.Count);

            var count = 0;
            foreach (var videoId in videoIds)
            {
                if(string.IsNullOrEmpty(videoId)) continue;
                C.WriteLine("count:" + ++count +"/"+ videoIds.Count);
                ExtendVideo(videoId, false, 0);
            }
            C.WriteLine("completed currently all videos ..");
        }
        public  void AnalyzeChannelsOnDb()
        {
            //var channelIdList = Sql.Execute<string>("select Id from Channels");
            var channelIdList =
                Sql.Execute<string>(
                        "select ChannelId from Videos with (NOLOCK) where ChannelId not in (select DISTINCT(Id) from Channels)  ORDER BY NEWID()")
                    .Distinct().ToArray();

            C.WriteLine("found channels " + channelIdList.Length);
            var videoIds = Sql.Execute<string>("select Id from Videos");
            foreach (var channelId in channelIdList)
            {
                try
                {
                    var channel = Service.GetChannelInfo(channelId);
                    if (channel == null) continue;

                    SqlStorage.Save(channel);
                    C.WriteLine("started channel : " + channel.Name);
                    if (channel.Uploads == null) continue;
                    
                    foreach (var v in channel.Uploads)
                    {
                        if (string.IsNullOrEmpty(v.Id)) continue;
                        if (videoIds.Contains(v.Id)) continue;

                        var video = Service.GetVideo(v.Id);

                        if (video != null)
                        {
                            SqlStorage.Save(video);
                        }
                        else
                        {
                            C.WriteLine("video not found with id `{0}`", v.Id);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            C.WriteLine("finished all channels");
        }
        public  void BrowseOnlyChannels(IYoutubeService service)
        {
            //var oldChannelsInDb = Sql.Execute<string>("select Id from Channels");
           
            var channelId = "UCjsdYhMIQ0-fJPqEjiHYlzA";//efe aydal 
            //channelId = "UCv6jcPwFujuTIwFQ11jt1Yw";
            _queue.Add(new KeyValuePair<string, string>(channelId, channelId));
            new Thread(WorkerThread).Start();
            YoutubeChannelInfo channel;
            //browse channels for popular categories
            //returns pairs as following: category name and list of channels
            var channels = service.BrowseChannels().ToArray();
            foreach (var keyValuePair in channels)
            {
                foreach (var channelInfo in keyValuePair.Value)
                {
                    channel = service.GetChannelInfo(channelInfo.Id);
                    SqlStorage.Save(channel);
                }
            }
        }

        public  void BrowseAllVideosComments()
        {
            C.WriteLine("loading videos");
            var videoIds = Sql
                .Execute<string>(
                    "select top(1000) Id from Videos where Id not in (select DISTINCT(VideoId) from Comments )  order by NEWID() ")
                .ToList();
            C.WriteLine("loaded videos");

            var count = 0;
            foreach (var videoId in videoIds)
            {
                try
                {
                    if (string.IsNullOrEmpty(videoId)) return;
                    C.WriteLine("started video with Id: " + videoId);
                    var commentsBatch = Service.GetRootComments(videoId);
                    foreach (var comments in commentsBatch)
                    {
                        try
                        {
                            count += comments.Length;
                            C.Write(comments.Length + "|");
                            foreach (var comment in comments)
                            {
                                comment.VideoId = videoId;
                            }

                            if (comments.Any(c => string.IsNullOrEmpty(c.VideoId)))
                            {
                                ;
                            }

                            SqlStorage.Save(comments);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                File.AppendAllLines("error.comments", new[] {videoId});
                            }
                            catch (Exception exception)
                            {
                                C.WriteLine("\t\t" + exception.Message);
                            }
                        }
                    }
                }
                catch (System.Net.WebException e)
                {
                    if (e.Message != "The remote server returned an error: (413) Request Entity Too Large.") continue;
                }
                catch (Exception e)
                {
                    //Thread.Sleep(TimeSpan.FromSeconds(10));
                    C.WriteLine(e.Message);
                    //if(retryCount<3) goto start;
                    //throw;
                }
            }
            C.WriteLine("\nfound.comments count -> " + count);
        }
    }
}