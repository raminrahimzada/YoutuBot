using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace YoutuBot
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(
            this IEnumerable<TSource> source, int size)
        {
            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new TSource[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }

        public static string Sqlize(this decimal? d)
        {
            if (d == null) return "0.0";
            return d.ToString();
        }

        public static string Sqlize(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Replace("'", "''");
        }

        public static string UrlEncode(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str
                .Replace(" ", "%20")
                .Replace("!", "%21")
                .Replace("\"", "%22")
                .Replace("#", "%23")
                //...
                .Replace("=", "%3D");
        }

        public static string JoinWith<T>(this IEnumerable<T> source, string separator = "`")
        {
            if (source == null) return string.Empty;
            return string.Join(separator, source);
        }

        public static string ReplaceLast(this string source, string find, string replace)
        {
            var place = source.LastIndexOf(find, StringComparison.Ordinal);

            if (place == -1)
                return source;

            var result = source.Remove(place, find.Length).Insert(place, replace);
            return result;
        }

        public static string GetStringBetween(this string text, string start, string end)
        {
            var id1 = text.IndexOf(start, StringComparison.Ordinal); //0
            if (id1 == -1) return null;
            var txt = text.Substring(id1 + start.Length);
            var id2 = txt.IndexOf(end, StringComparison.Ordinal); //12
            txt = txt.Substring(0, id2);
            return txt;
        }

        public static IEnumerable<string> GetStringBetweenAll(this string text, string start, string end)
        {
            var str = text;
            int id1;
            while (true)
            {
                id1 = str.IndexOf(start, StringComparison.Ordinal); //0
                if (id1 == -1) yield break;
                var txt = str.Substring(id1 + start.Length);
                if (string.IsNullOrEmpty(txt))
                {
                    yield return txt;
                    yield break;
                }

                var id2 = txt.IndexOf(end, StringComparison.Ordinal); //12
                txt = txt.Substring(0, id2);
                yield return txt;
                str = str.Substring(id1 + txt.Length + end.Length);
            }
        }

        public static int CountOf(this string str, string word)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            var count = 0;
            var id = 0;
            do
            {
                id = str.IndexOf(word, id, StringComparison.InvariantCultureIgnoreCase);
                if (id > 0) count++;
                if (id > str.Length) break;
                if (id == -1) break;
                id = id + word.Length;
            } while (id > 0);

            return count;
        }

        public static string RemoveThisFirst(this string str, string none)
        {
            return str.ReplaceFirst(none, string.Empty);
        }

        public static string RemoveThisLast(this string str, string none)
        {
            return str.ReplaceLast(none, string.Empty);
        }

        public static string RemoveThis(this string str, string none)
        {
            return str.Replace(none, string.Empty);
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseQueryString(this string s)
        {
            // remove anything other than query string from url
            if (s.Contains("?")) s = s.Substring(s.IndexOf('?') + 1);

            return Regex.Split(s, "&").Select(vp => Regex.Split(vp, "="))
                .Select(strings => new KeyValuePair<string, string>(strings[0],
                    strings.Length == 2 ? strings[1].UrlDecode() : string.Empty));
        }

        public static decimal? ParseDecimal(this string str)
        {
            if (decimal.TryParse(str, out var d)) return d;

            return null;
        }

        public static KeyValuePair<string, string>[] ParseQS(this string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str)) return new KeyValuePair<string, string>[0];
                return
                    str?.Split('&')
                        .Select(c => new KeyValuePair<string, string>(c.Split('=')[0],
                            Uri.UnescapeDataString(c.Split('=')[1]))).ToArray();
            }
            catch (Exception e)
            {
                return str.ParseQueryString().ToArray();
            }
        }

        public static string UrlDecode(this string url)
        {
#if !PORTABLE
            return WebUtility.UrlDecode(url);
#else
            return System.Web.HttpUtility.UrlDecode(url);
#endif
        }

        public static string DownloadHTML(this string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
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
            var responseStream = responseObj.GetResponseStream();
            if (responseStream == null)
            {
                responseObj.Close();
                responseObj.Dispose();
                return string.Empty;
            }

            var reader = new StreamReader(responseStream);
            using (reader)
            {
                return reader.ReadToEnd();
            }
        }
    }
}