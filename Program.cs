using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace YoutuBot
{
    class Program
    {
        static async Task<string> MakeNarRequest(string otp,string formKey,string sessionId)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = false;

            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://plus.nar.az/user/account/validateotp"))
                {
                    request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                    request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
                    request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json, text/javascript, */*; q=0.01");
                    request.Headers.TryAddWithoutValidation("Origin", "https://plus.nar.az");
                    request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.87 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Referer", "https://plus.nar.az/customer/account/forgotpassword");
                    request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
                    request.Headers.TryAddWithoutValidation("Cookie", $"store=az; PHPSESSID={sessionId}; searchReport-log=0; _gcl_au=1.1.1079352900.1573642285; _fbp=fb.1.1573642286641.1474381690; mage-cache-storage=%7B%7D; mage-cache-storage-section-invalidation=%7B%7D; _hjid=2c86d49f-c275-4ab6-a763-ca22ca4e3ce9; recently_viewed_product=%7B%7D; recently_viewed_product_previous=%7B%7D; recently_compared_product=%7B%7D; recently_compared_product_previous=%7B%7D; product_data_storage=%7B%7D; mage-messages=; private_content_version=bbed535fce0eba9722854b1193715064; section_data_ids=%7B%22messages%22%3A4000%7D");

                    request.Content = new StringContent($"otp={otp}&formKey={formKey}&forgotPassword=true");
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded; charset=UTF-8");

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
        static async Task Main(string[] args)
        {
            C.Service = new YoutubeService();
            //new Thread(new Bot().AnalyzeChannelsOnDb).Start();
            new Thread(new Bot().ExtendAllVideos).Start();
            //new Thread(new Bot().AnalyzeChannelsOnDb).Start();
            //new Thread(new Bot().BrowseAllVideosComments).Start();
            //new Thread(new Bot().BrowseAllVideosComments).Start();

            while (true)
            {
            }

            var service = C.Service;
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
