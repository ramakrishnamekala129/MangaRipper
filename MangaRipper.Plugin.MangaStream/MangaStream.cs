﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaRipper.Core.Logging;
using MangaRipper.Core.Models;
using MangaRipper.Core.Plugins;

namespace MangaRipper.Plugin.MangaStream
{
    /// <summary>
    /// Support find chapters, images from MangaStream
    /// </summary>
    public class MangaStream : IPlugin
    {
        private static ILogger logger;
        private readonly IHttpDownloader downloader;
        private readonly IXPathSelector selector;

        public MangaStream(ILogger myLogger, IHttpDownloader downloader, IXPathSelector selector)
        {
            logger = myLogger;
            this.downloader = downloader;
            this.selector = selector;
        }
        public async Task<IEnumerable<Chapter>> GetChapters(string manga, IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            string input = await downloader.GetStringAsync(manga, cancellationToken);
            var title = selector.Select(input, "//h1").InnerText;
            var chaps = selector
                .SelectMany(input, "//td/a")
                .Select(n =>
                {
                    string url = $"https://readms.net{n.Attributes["href"]}";
                    return new Chapter($"{title} {n.InnerText}", url);
                });
            return chaps;
        }

        public async Task<IEnumerable<string>> GetImages(string chapterUrl, IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            // find all pages in a chapter
            string input = await downloader.GetStringAsync(chapterUrl, cancellationToken);
            var pages = selector.SelectMany(input, "//div[contains(@class,'btn-reader-page')]/ul/li/a")
                .Select(n => n.Attributes["href"])
                .Select(p => $"https://readms.net{p}");

            // find all images in pages
            var images = new List<string>();
            foreach (var page in pages)
            {
                var pageHtml = await downloader.GetStringAsync(page, cancellationToken);
                var image = selector
                .Select(pageHtml, "//img[@id='manga-page']")
                .Attributes["src"];

                images.Add(image);
                progress.Report("Detecting: " + images.Count);
            }
            return images.Select(i => $"https:{i}");
        }

        public SiteInformation GetInformation()
        {
            return new SiteInformation(nameof(MangaStream), "https://readms.net/manga", "English");
        }

        public bool Of(string link)
        {
            var uri = new Uri(link);
            return uri.Host.Equals("readms.net");
        }
    }
}
