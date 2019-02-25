using System;
using System.Linq;
namespace YoutuBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //our awesome service
            IYoutubeService service = new YoutubeService();
            
            //browse channels for popular categories
            //returns pairs as following: category name and list of channels
            var channels = service.BrowseChannels().ToArray();

            //awesome channelId 
            var channelId = "UCjsdYhMIQ0-fJPqEjiHYlzA";

            //awesome video
            var videoId = "N45X65Uh6Z8";


            //and awesome information about video and channel
            var vid = service.GetVideo(videoId);
            var channel = service.GetChannelInfo(channelId);
            
            //get user playlist list
            var playLists = service.GetUserPlayLists(channel.UserId).ToArray();
            
            //get playlist info and all videos
            var list = service.GetPlayList(playLists.First().Id);

            //get video comments
            var comments = service.GetRootComments(videoId);
            foreach (var commentPackage in comments)
            {
                //lets write comments down here 
                foreach (var videoComment in commentPackage)
                {
                    //change color and write comment author
                    var temp = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(videoComment.AuthorName+" :");

                    //write comment text
                    Console.ForegroundColor = temp;
                    Console.WriteLine(videoComment.Text);
                }
            }

            //get trends for country
            var trends = service.GetTrending("TR").ToArray();
            
            //search for videos
            var videos = service.SearchForVideo("efe aydal", 31);
            //and write down here 
            Console.WriteLine("found videos");
            int i = 1;
            foreach (var videoInfo in videos)
            {
                Console.WriteLine(i++ + ") " + videoInfo.Title);
            }
            
            
        }
    }
}
