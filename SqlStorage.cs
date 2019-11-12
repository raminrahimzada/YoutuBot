using System;
using System.Threading.Tasks;
using YoutuBot.Models;

namespace YoutuBot
{
    class SqlStorage
    {
        public static void Save(YoutubeChannelInfo channel)
        {
            Console.Write("saving... " + channel.Name+"\t");
            var query = QueryGenerator.Generate(channel);
            Sql.Execute(query);
            Console.WriteLine("saved ... ");
        }

        public static void Save(YoutubeVideoInfo video)
        {
            if (video == null) return;
            Console.Write("saving... " + video.Title+ "\t");
            var query = QueryGenerator.Generate(video);
            Sql.Execute(query);
            Console.WriteLine("saved ... ");
        }
    }
}
