using Microsoft.VisualBasic.FileIO;
using SongGetterWebAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public IHttpActionResult SendSongFileToUser(IStreamInfo stream)
        {

            //System.Diagnostics.Debug.WriteLine(": ", );
            return Ok(stream);
        }

        public async Task<IStreamInfo> QueryLib(string Url)
        { 
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Url);
            var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

            if (streamInfo != null)
            {
                string path = Path.Combine("C:/Users/jabri_000/source/repos/SongGetterWebAPI/SongGetterWebAPI/tmp", "MAudio.mp3");
                Progress<double> prog = new Progress<double>(p => System.Diagnostics.Debug.WriteLine($"Progress updated: {p}"));
                await youtube.Videos.Streams.DownloadAsync(streamInfo, path, prog);
            }

            return streamInfo;
        }

        public async Task<IHttpActionResult> RequestSongFromLib([FromBody] SongRequest Body)
        {
            await QueryLib(Body.Url);
            //SendSongFileToUser(stream);

            return Ok();
        }
    }
}