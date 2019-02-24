using System.Linq;
namespace YoutuBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //our awesome service
            IYoutubeService service = new YoutubeService();

            //awesome channelId 
            var channelId = "UCjsdYhMIQ0-fJPqEjiHYlzA";

            //awesome video
            var videoId = "N45X65Uh6Z8";
            
            //and awesome information about video and channel
            var vid = service.GetVideo(videoId);
            var channel = service.GetChannelInfo(channelId);
            var playLists = service.GetUserPlayLists(channel.UserId).ToArray();

            var list = service.GetPlayList(playLists.First().Id);
        }
    }
}
