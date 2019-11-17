using System;
using System.Text;
using System.Threading.Tasks;
using YoutuBot.Models;

namespace YoutuBot
{
    class SqlStorage
    {
        public static void Save(YoutubeChannelInfo channel)
        {
            if (channel == null) return;
            if (string.IsNullOrEmpty(channel.Id)) return;
            C.Write("saving... " + channel.Name+"\t");
            var query = QueryGenerator.Generate(channel);
            Sql.Execute(query);
            C.WriteLine("saved ... ");
        }

        public static void Save(YoutubeVideoInfo video)
        {
            if (video == null) return;
            if (string.IsNullOrEmpty(video.Id)) return;
            C.Write("saving... " + video.Title+ "\t");
            var query = QueryGenerator.Generate(video);
            Sql.Execute(query);
            C.WriteLine("saved ... ");
        }

        public static void Save(YoutubeVideoCommentInfo[] comments)
        {
            var sb = new StringBuilder();
            foreach (var comment in comments)
            {
                sb.AppendLine(QueryGenerator.Generate(comment));
            }
            if (sb.Length > 0)
            {
                Sql.Execute(sb.ToString());
            }
        }
    }
}
