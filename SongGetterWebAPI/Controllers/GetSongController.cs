using SongGetterWebAPI.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;


namespace SongGetterWebAPI.Controllers
{
    public class GetSongController : ApiController
    {
        readonly YoutubeClient youtube = new YoutubeClient();

        private async Task<PathInfo> DownloadVideoAsMp3(string Url, PathInfo pathInfo)
        {
            //fetch stream manifest using YoutubeExplode library 
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Url);

            //get audio as highest bitrate

            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            

            if (streamInfo != null)
            {
                System.Diagnostics.Debug.WriteLine("downloading...");
                pathInfo.IsError = false;

                //escape illegal chars in filename
                if (Regex.Match(pathInfo.FileName, @"[:|]").Success) 
                { 
                    pathInfo.FileName = Regex.Replace(pathInfo.FileName, @"[:|]", "-"); 
                }
               
                if (Regex.Match(pathInfo.FileName, @"[/?*\\]").Success) 
                { 
                    pathInfo.FileName = Regex.Replace(pathInfo.FileName, @"[/?*\\]", " "); 
                }

                if (pathInfo.FileName.Contains('"')) 
                { 
                    pathInfo.FileName = pathInfo.FileName.Replace("\"", "'"); 
                }

                System.Diagnostics.Debug.WriteLine(pathInfo.FileName, "filename");

                //download video as mp3 using path data, log progress
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

        private async Task<PathInfo> QueryLib(SongRequest SongRequest)
        {
            System.Diagnostics.Debug.WriteLine("executing query lib");

            if (!SongRequest.IsPlaylist)
            {
                var video = youtube.Videos;

                //get video title from metadata
                var metadata = await video.GetAsync(SongRequest.Url);
                var title = metadata.Title;

                //save filename as title and assign path for video download in project App_Data
                PathInfo pathInfo = new PathInfo();
                pathInfo.FilePath = @"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\App_Data";
                pathInfo.FileName = title + ".mp3";

                //download video
                return await Task.Run(() => DownloadVideoAsMp3(SongRequest.Url, pathInfo));
            }
            else
            {
                //get playlist
                var playlist = await youtube.Playlists.GetAsync(SongRequest.Url);

                //get playlist title from metadata
                var playlistTitle = playlist.Title;

                //create directory for folder to store audio files in playlist, call folder the title of playlist 
                PathInfo playlistPathInfo = new PathInfo();
                playlistPathInfo.FilePath = Path.Combine(@"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\App_Data", playlistTitle);
                Directory.CreateDirectory(playlistPathInfo.FilePath);

                await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
                {
                    //get title of each video, create path using title as filename
                    var videoTitle = video.Title;
                    PathInfo videoPathInfo = new PathInfo();
                    videoPathInfo.FilePath = playlistPathInfo.FilePath;
                    videoPathInfo.FileName = videoTitle + ".mp3";

                    //download videos
                    await Task.Run(() => DownloadVideoAsMp3(video.Url, videoPathInfo));
                }

                //create zip file in App_Data folder using playlist title
                PathInfo zipPathInfo = new PathInfo();
                zipPathInfo.FilePath = @"C:\Users\jabri_000\source\repos\SongGetterWebAPI\SongGetterWebAPI\App_Data\Zips";
                zipPathInfo.FileName = playlistTitle + ".zip";

                //zip file for transmission
                string sourceDirectory = playlistPathInfo.FilePath;
                string targetFile = Path.Combine(zipPathInfo.FilePath, zipPathInfo.FileName);

                if (!File.Exists(targetFile))
                {
                    await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, targetFile));
                }
                return zipPathInfo;
            }
        }

        private HttpResponseMessage ConstructResponse(PathInfo pathInfo, string filePath, string ContentType)
        {

            //if video could not be downloaded (incorrect url?), return error response message
            if (pathInfo.IsError)
            {
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                return errorResponse;
            }

            string fullPath = Path.Combine(pathInfo.FilePath, pathInfo.FileName);

            //build response to send to client
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            
            var fileBytes = File.ReadAllBytes(filePath);
            var memoryStream = new MemoryStream(fileBytes);
            result.Content = new StreamContent(memoryStream);

            var headers = result.Content.Headers;
            headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            headers.ContentDisposition.FileName = pathInfo.FileName;
            headers.ContentType = new MediaTypeHeaderValue(ContentType);
            headers.ContentLength = memoryStream.Length;
            return result;
        }

        [HttpGet]
        [Route("api/GetSong")]
        public async Task<HttpResponseMessage> GetSongFromLib(string Url)
        {
            System.Diagnostics.Debug.WriteLine("executing GetSongFromLib");

            //instantiate SongRequest class assigning json data sent in GET request from client
            SongRequest songRequest = new SongRequest();
            songRequest.Url = Url;
            songRequest.IsPlaylist = false;

            //create paths, download video(s) from YoutubeExplode library
            var pathInfo = await Task.Run(() => QueryLib(songRequest));

            var filePath = HttpContext.Current.Server.MapPath($"~/App_Data/{pathInfo.FileName}");
            return await Task.Run(() => ConstructResponse(pathInfo, filePath, "audio/mpeg"));
        }

        [HttpGet]
        [Route("api/GetPlaylist")]
        public async Task<HttpResponseMessage> GetPlaylistFromLib(string Url)
        {
            System.Diagnostics.Debug.WriteLine("executing GetPlaylistFromLib");

            //instantiate SongRequest class assigning json data sent in GET request from client
            SongRequest songRequest = new SongRequest();
            songRequest.Url = Url;
            songRequest.IsPlaylist = true;

            //create paths, download video(s) from YoutubeExplode library
            var pathInfo = await Task.Run(() => QueryLib(songRequest));

            var filePath = HttpContext.Current.Server.MapPath($"~/App_Data/Zips/{ pathInfo.FileName}");
            return await Task.Run(() => ConstructResponse(pathInfo, filePath, "application/zip"));
        }
    }
}
