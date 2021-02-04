using SongGetterWebAPI.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
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
            pathInfo.FilePath = @"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\App_Data";
            pathInfo.FileName = title + ".mp3";

            if (streamInfo != null)
            {
                System.Diagnostics.Debug.WriteLine("downloading...");
                pathInfo.IsError = false;
                string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);
                //Progress<double> prog = new Progress<double>(p => System.Diagnostics.Debug.WriteLine($"Progress updated: {p}"));
                await youtube.Videos.Streams.DownloadAsync(streamInfo, fullPath/*, prog*/);
            }
            else
            {
                pathInfo.IsError = true;
            }

            return pathInfo;
        }

        [HttpGet]
        [Route("api/GetSong")]
        public async Task<HttpResponseMessage> GetSongFromLib(string Url)
        {
            var pathInfo = await Task.Run(() => QueryLib(Url));

            if (pathInfo.IsError)
            {
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                return errorResponse;
            }

            string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);

            var result = new HttpResponseMessage(HttpStatusCode.OK);

            var filePath = HttpContext.Current.Server.MapPath($"~/App_Data/{pathInfo.FileName}");
            var fileBytes = File.ReadAllBytes(filePath);
            var memoryStream = new MemoryStream(fileBytes);
            result.Content = new StreamContent(memoryStream);

            var headers = result.Content.Headers;
            headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            headers.ContentDisposition.FileName = pathInfo.FileName;
            headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            headers.ContentLength = memoryStream.Length;

            System.Diagnostics.Debug.WriteLine("sending");
            return result;
        }

        [HttpPost]
        [Route("api/DeleteAfterDownload")]
        public IHttpActionResult DeleteAfterDownload([FromBody] PathInfo body)
        {
            System.Diagnostics.Debug.WriteLine(body);
            try
            {
                var path = @"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\App_Data";
                var fullPath = Path.Combine(path, body.FileName);
                File.Delete(fullPath);
                return Ok();
            }
            catch(Exception e)
            {
                return Content(HttpStatusCode.BadRequest, e);
            }
            
        }
    }
}
