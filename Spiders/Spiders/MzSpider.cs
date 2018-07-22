using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Spiders
{
    class MzSpider
    {
        private readonly string _homePage = "http://www.mzitu.com/all";
        private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
        private CookieContainer _cookieContainer;

        public MzSpider()
        {
            _cookieContainer = new CookieContainer();
        }

        public void Start()
        {
            var request = WebRequest.CreateHttp(_homePage);
            request.UserAgent = _userAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.CookieContainer = _cookieContainer;

            var homeHtml = string.Empty;
            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var sr = new StreamReader(stream);
                    homeHtml = sr.ReadToEnd();
                }
            }
            catch(WebException wex)
            {
                Console.WriteLine("获取列表失败, {0}", wex);
            }

            if (!string.IsNullOrEmpty(homeHtml))
            {
                var hd = new HtmlDocument();
                hd.LoadHtml(homeHtml);
                var root = hd.DocumentNode;

                var allNode = root.SelectSingleNode(".//div[@class='all']");
                var uls = allNode.SelectNodes(".//ul[@class='archives']");
                foreach(var ul in uls)
                {
                    var linkAs = ul.SelectNodes(".//a");
                    foreach (var linkA in linkAs)
                    {
                        var detailLink = linkA.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(detailLink))
                        {
                            GetAllDetails(detailLink);
                        }
                    }

                }
            }
        }

        private void GetAllDetails(string detailRootUrl)
        {
            var request = WebRequest.CreateHttp(detailRootUrl);
            request.UserAgent = _userAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.CookieContainer = _cookieContainer;
            request.Referer = _homePage;

            var detailHtml = string.Empty;
            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var sr = new StreamReader(stream);
                    detailHtml = sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                Console.WriteLine("获取详情页 {0} 失败, {1}", detailRootUrl, wex);
            }

            if (!string.IsNullOrEmpty(detailHtml))
            {
                var hd = new HtmlDocument();
                hd.LoadHtml(detailHtml);
                //获取用于遍历图片固定部分
                var mainNode = hd.DocumentNode.SelectSingleNode(".//div[@class='main-image']");
                var imageNode = mainNode.SelectSingleNode(".//img");
                var imageUrl = imageNode.GetAttributeValue("src", "");
                var uri = new Uri(imageUrl);
                var imageName = uri.Segments[uri.Segments.Length - 1].Substring(3);
                var imageFolder = imageUrl.Replace(imageName, "");

                //获取图片总数
                var navigateNode = hd.DocumentNode.SelectSingleNode(".//div[@class='pagenavi']");
                var nodes = navigateNode.SelectNodes(".//a");
                var totalNode = nodes[nodes.Count - 2];
                var totalPage = int.Parse(totalNode.InnerText.Trim());

                for(var i = 1; i <= totalPage; i++)
                {
                    var url = i > 9 ? string.Format("{0}{1}.jpg", imageFolder, "0" + i) : string.Format("{0}{1}.jpg", imageFolder, i);
                    var referer = i == 1 ? detailHtml : string.Format("{0}/{1}", detailHtml, i);

                    var imagRequest = WebRequest.CreateHttp(url);
                    imagRequest.UserAgent = _userAgent;
                    imagRequest.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
                    imagRequest.CookieContainer = _cookieContainer;
                    imagRequest.Referer = referer;
                }
            }
        }
    }
}
