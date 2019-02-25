using System.IO;

namespace YoutuBot
{
    public static class YoutubeHelpers
    {
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
    }
}
