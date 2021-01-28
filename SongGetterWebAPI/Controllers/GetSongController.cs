using Microsoft.VisualBasic.FileIO;
using SongGetterWebAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace SongGetterWebAPI.Controllers
{
    public class GetSongController : ApiController
    {
        YoutubeClient youtube = new YoutubeClient();

        private async Task<PathInfo> QueryLib(string Url)
        {
            YoutubeClient youtube = new YoutubeClient();

            var video = youtube.Videos;

            var metadata = await video.GetAsync(Url);
            var title = metadata.Title;

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Url);
            var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

            PathInfo pathInfo = new PathInfo();
            pathInfo.FilePath = "C:/Users/jabri_000/source/repos/SongGetterWebAPI/SongGetterWebAPI/tmp";
            pathInfo.FileName = title + ".mp3";

            if (streamInfo != null)
            {
                System.Diagnostics.Debug.WriteLine("downloading...");
                pathInfo.IsError = false;
                string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);
                Progress<double> prog = new Progress<double>(p => System.Diagnostics.Debug.WriteLine($"Progress updated: {p}"));
                await youtube.Videos.Streams.DownloadAsync(streamInfo, fullPath, prog);
            }
            else
            {
                pathInfo.IsError = true;
            }

            return pathInfo;
        }

        public async Task<HttpResponseMessage> GetSongFromLib(string Url)
        {
            var pathInfo = await Task.Run(() => QueryLib(Url));

            if (pathInfo.IsError)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);

            //var dataBytes = File.ReadAllBytes(fullPath);
            //var dataStream = new MemoryStream(dataBytes);

            using (HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK))
            {
                //var filePath = HttpContext.Current.Server
                //    .MapPath($"~/App_Data{pathInfo.FileName}");
                var fileBytes = File.ReadAllBytes(fullPath);
                var fileStream = new MemoryStream(fileBytes);

                response.Content = new StreamContent(fileStream);

                var headers = response.Content.Headers;

                headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                headers.ContentDisposition.FileName = pathInfo.FileName;

                headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

                headers.ContentLength = fileStream.Length;


                /*
                response.Content = new StreamContent(dataStream);
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = pathInfo.FileName;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                */

                System.Diagnostics.Debug.WriteLine("response: ", response);
                return response;
            }
        }
    }
}