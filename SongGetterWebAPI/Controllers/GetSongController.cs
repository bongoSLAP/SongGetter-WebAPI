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
using System.Web.Http;
using System.Web.Http.Results;
using YoutubeExplode;
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
            pathInfo.FilePath = @"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\tmp";
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

        public async Task<ResponseMessageResult> GetSongFromLib(string Url)
        {
            var pathInfo = await Task.Run(() => QueryLib(Url));

            if (pathInfo.IsError)
            {
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                ResponseMessageResult errorResult = ResponseMessage(errorResponse);
                return errorResult;
            }

            string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);
            System.Diagnostics.Debug.WriteLine("fullPath: ", fullPath);

            byte[] fileBytes = File.ReadAllBytes(fullPath);
            MemoryStream stream = new MemoryStream(fileBytes);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };

            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = pathInfo.FileName
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            ResponseMessageResult result = ResponseMessage(response);

            /*
            var headers = response.Content.Headers;

                headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                headers.ContentDisposition.FileName = pathInfo.FileName;

                headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

                headers.ContentLength = stream.Length;
            */

            return result;


        }
    }
}