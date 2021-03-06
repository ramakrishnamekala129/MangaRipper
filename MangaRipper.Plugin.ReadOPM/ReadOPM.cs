﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaRipper.Core.Logging;
using MangaRipper.Core.Models;
using MangaRipper.Core.Plugins;

namespace MangaRipper.Plugin.ReadOPM
{
    /// <summary>
    /// Support find chapters, images from readopm
    /// </summary>
    public class ReadOPM : IPlugin
    {
        private static ILogger logger;
        private readonly IHttpDownloader downloader;
        private readonly IXPathSelector selector;

        public ReadOPM(ILogger myLogger, IHttpDownloader downloader, IXPathSelector selector)
        {
            logger = myLogger;
            this.downloader = downloader;
            this.selector = selector;
        }
        public async Task<IEnumerable<Chapter>> GetChapters(string manga, IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            string input = await downloader.GetStringAsync(manga, cancellationToken);
            var chaps = selector
                .SelectMany(input, "//ul[contains(@class, 'chapters-list')]/li/a")
                .Select(n =>
                {
                    string url = n.Attributes["href"];
                    return new Chapter(null, url);
                }).ToList();

            var chap_numbers = selector
                .SelectMany(input, "//ul[contains(@class, 'chapters-list')]/li/a/span[contains(@class, 'chapter__no')]")
                .Select(n => n.InnerText)
                .ToList();

            chaps.ForEach(c => c.Name = "One Punch Man " + chap_numbers[chaps.IndexOf(c)]);
            return chaps;
        }

        public async Task<IEnumerable<string>> GetImages(string chapterUrl, IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            progress.Report("Detecting...");
            string input = await downloader.GetStringAsync(chapterUrl, cancellationToken);
            var images = selector.SelectMany(input, "//div[contains(@class,'img_container')]/img")
                .Select(n => n.Attributes["src"])
                .Where(src =>
                {
                    return !string.IsNullOrWhiteSpace(src)
                    && Uri.TryCreate(src, UriKind.Absolute, out Uri validatedUri)
                    && !string.IsNullOrWhiteSpace(Path.GetFileName(validatedUri.LocalPath));
                });

            return images;
        }

        public SiteInformation GetInformation()
        {
            return new SiteInformation(nameof(ReadOPM), "https://ww3.readopm.com/", "English");
        }

        public bool Of(string link)
        {
            var uri = new Uri(link);
            return uri.Host.Equals("ww3.readopm.com");
        }
    }
}
