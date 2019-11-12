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
        public static IYoutubeService Service;
        static readonly ConcurrentBag<string> _cache=new ConcurrentBag<string>();
        public static object sync=new object();
        public static BlockingCollection<KeyValuePair<string, string>> _queue = new BlockingCollection<KeyValuePair<string, string>>();

        public static void WorkerThread()
        {
            foreach (var kv in _queue.GetConsumingEnumerable())
            {
                var channelId=kv.Key;
                var fromChannelId = kv.Value;
                Browse(channelId);
            }
        }
        public static void Browse(string channelId)
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

        public static object syncException = new object();

        public static void ExtendVideo(string videoId)
        {
            try
            {
                if (_videoCache.Contains(videoId)) return;
                C.WriteLine("working.videoId:" + videoId);
                var video = Service.GetVideo(videoId);
                if (video == null)
                {
                    _videoCache.Add(videoId);
                    return;
                }
                if (_videoCache.Contains(videoId)) return;
                SqlStorage.Save(video);
                _videoCache.Add(videoId);
                
                if (video.NextVideos != null)
                {
                    C.WriteLine("working.Found :" + video.NextVideos?.Count);

                    foreach (var nv in video.NextVideos)
                    {
                        if (_videoCache.Contains(nv.Id)) continue;
                        var nvId = nv.Id;
                        ThreadPool.QueueUserWorkItem(x =>
                        {
                            ExtendVideo(nvId); 
                        });
                    }
                }
            }
            catch (Exception e)
            {
                lock (syncException)
                {
                    C.WriteLine("ERROR:" + videoId + " " + e.Message);
                    File.AppendAllLines("errors.txt", new[] {videoId});
                }
            }
        }
        private static ConcurrentBag<string> _videoCache=new ConcurrentBag<string>();
        public static void ExtendAllVideos(IYoutubeService service)
        {
            Service = service;

            C.WriteLine("loading videos from db..");
            var videoIds = Sql
                .Execute<string>(
                    "select top(4000) Id from Videos  order by NEWID() ")
                .ToList();

            C.WriteLine("loaded. count:"+videoIds.Count);

            var count = 0;
            foreach (var videoId in videoIds)
            {
                if(string.IsNullOrEmpty(videoId)) continue;
                C.WriteLine("count:" + ++count +"/"+ videoIds.Count);
                ExtendVideo(videoId);
            }
        }
        public static void AnalizeChannelsOnDb()
        {
            var channelIdList = Sql.Execute<string>("select Id from Channels");
            C.WriteLine("found channels " + channelIdList.Length);
            var videoIds = Sql.Execute<string>("select Id from Videos");
            foreach (var channelId in channelIdList)
            {
                var channel = Service.GetChannelInfo(channelId);
                C.WriteLine("started channel : " + channel.Name);
                if (channel.Uploads != null)
                {
                    foreach (var v in channel.Uploads)
                    {
                        if(string.IsNullOrEmpty(v.Id)) continue;
                        if(videoIds.Contains(v.Id)) continue;

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
            }

            C.WriteLine("finished all channels");
        }
        public static void BrowseOnlyChannels(IYoutubeService service)
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
    }
}