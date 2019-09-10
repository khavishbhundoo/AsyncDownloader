using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncDownloader
{
    /// <summary>
    /// Handles downloading of files irrespective of file size asynchronous and stream the files to disk
    /// </summary>
    public class AsyncDownloader
    {
        static SocketsHttpHandler handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            /*
             * Time to wait when tring to connect to a host similar to CURLOPT_CONNECTTIMEOUT in curl
             * Ensure that we don't keep waiting forever to connect since we set infinite timeout for the whole transfer
             * Source : https://github.com/dotnet/corefx/issues/40706
             */
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };
        static readonly HttpClient client = new HttpClient(handler)
        {
            /* 
             * This timeout is for the whole transfer, that's why it has been set to infinite since we want to be able to download files of any size
             * Source : https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=netframework-4.8#remarks
             */
            Timeout = Timeout.InfiniteTimeSpan
        };

        static readonly Stopwatch sw = new Stopwatch();

        private string _url;

        private string _location;

        private string _user_agent;

        private int _buffer_size = 16384;

        private int _max_concurrent_downloads = 2;

        private string _abort_message;

        #region Properties
        /// <summary>
        /// Returns the current download speed in bytes per seconds.
        /// </summary>
        public long SpeedBytesSecs { get; private set; }

        /// <summary>
        /// Returns the current download speed in human readable fashion like in KB/s , MB/s , GB/s  
        /// </summary>
        public string PrettyPrintSpeed
        {
            get
            {
                return Utils.GetBytesReadable(SpeedBytesSecs);
            }
        }
        /// <summary>
        /// Get or Set the maximum number of concurrent downloads 
        /// </summary>
        public int MaxConcurrentDownloads
        {
            set
            {
                bool isNum = int.TryParse(value.ToString(), out this._max_concurrent_downloads);
                if (isNum)
                {
                    if (this._max_concurrent_downloads < 1) throw new Exception("The max concurrent download should be greater than 1");

                }
                else
                {
                    throw new Exception("The max concurrent download should be an integer value greater than 1");
                }
            }

            get
            {
                return this._max_concurrent_downloads;
            }
        }

        /// <summary>
        /// Get or Set the useragent to use when making the request to download the file.By default no useragent is used.
        /// </summary>
        public string UserAgent
        {
            set
            {
                this._user_agent = value;
            }
            get
            {
                return this._user_agent;
            }
        }

        /// <summary>
        /// Returns true if download is complete , false otherwise
        /// </summary>
        public bool Finish { get; private set; } = false;

        /// <summary>
        /// Returns true if download has been aborted , false otherwise
        /// </summary>
        public bool Aborted { get; private set; } = false;

        /// <summary>
        /// Throw an Exception if a download has been aborted
        /// </summary>
        public string AbortException()
        {
            if (!string.IsNullOrWhiteSpace(this._abort_message))
            {
                throw new Exception(this._abort_message);
            }
            return null;

        }
        /// <summary>
        /// Return the filename of the download file from the content-disposition header.If no such header is present, then get the filename from the path.
        /// If the filename already exist in the directory , then append a uniquely generated string to the file
        /// </summary>
        public string FileName { get; private set; } = "";
        /// <summary>
        /// Return the downloaded bytes downloaded so far in bytes/sec
        /// </summary>
        public long TotalBytes { get; private set; } = 0L;

        /// <summary>
        /// Return the downloaded bytes downloaded so far in human readable fashion like in KB/s , MB/s , GB/s
        /// </summary>
        public string PrettyPrintTotalBytes
        {
            get
            {
                return Utils.GetBytesReadable(TotalBytes);
            }
        }

        /// <summary>
        /// Get or Set the buffer size for the filestream . The size by default is 16kB
        /// </summary>
        public int BufferSize
        {
            set
            {
                bool isNum = int.TryParse(value.ToString(), out this._buffer_size);

                if (isNum)
                {
                    if (this._buffer_size < 8192) throw new Exception("The buffer size should be at least 8192 bytes");

                }
                else
                {
                    throw new Exception("The buffer size should be an integer in bytes");
                }
            }

            get
            {
                return this._buffer_size;
            }
        }

        /// <summary>
        /// Get or set url to download
        /// </summary>
        public string Url
        {
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("Url is mandatory");
                }

                if (!Utils.CheckURL(value))
                {
                    throw new Exception("Invalid url.A valid url must begin with http(s)");
                }

                this._url = value;
            }
            get
            {
                if (string.IsNullOrWhiteSpace(this._url))
                {
                    throw new Exception("Url is mandatory");
                }
                else if (!Utils.CheckURL(this._url))
                {
                    throw new Exception("Invalid url.A valid url must begin with http(s)");
                }
                return this._url;
            }
        }
        /// <summary>
        /// Get or set the download path where to store the download file.By default , download to current directory
        /// </summary>
        public string Location
        {
            set
            {
                if (!Directory.Exists(value))
                {
                    throw new Exception("Invalid path");
                }
                this._location = value;
            }
            get
            {
                if (string.IsNullOrWhiteSpace(this._location))
                {
                    return Environment.CurrentDirectory;
                }
                return this._location;
            }
        }

        #endregion Properties

        /// <summary>
        /// Main method responsible for downloading file asynchronously
        /// </summary>
        private async Task FetchLargeFiles()
        {

            // ServicePointManager setup
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = this._max_concurrent_downloads;
            ServicePointManager.EnableDnsRoundRobin = true;
            ServicePointManager.ReusePort = true;

            if (!string.IsNullOrWhiteSpace(this.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(this.UserAgent);
            }

            HttpResponseMessage response = null;

            try
            {
                response = await client.GetAsync(this._url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                this.Aborted = true;
                this._abort_message = string.Format("An exception has occured while trying to download {0} ! .{1}", this.Url, ex.Message);
                return;
            }

            if (FileName.Length == 0 && response.Content.Headers.Contains("content-disposition"))
            {
                FileName = response.Content.Headers.ContentDisposition.FileName;
            }
            else if (FileName.Length == 0)
            {
                FileName = Path.GetFileName(this._url);
            }

            string fileToWriteTo = Path.Combine((string.IsNullOrWhiteSpace(this._location)) ? Environment.CurrentDirectory : this._location, FileName);

            if(File.Exists(fileToWriteTo))
            {
                DateTime foo = DateTime.UtcNow;
                long unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds();
                fileToWriteTo = Path.Combine((string.IsNullOrWhiteSpace(this._location)) ? Environment.CurrentDirectory : this._location, unixTime + '_'+ FileName);
            } 

            using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(fileToWriteTo, FileMode.Create, FileAccess.Write, FileShare.None, this._buffer_size, true))
            {
                byte[] buffer = new byte[this._buffer_size];
                bool isMoreToRead = true;

                Stack<int> Chunk_dl_speeds = new Stack<int>();

                do
                {
                    // Start the stopwatch used to calculate the download speed
                    sw.Start();
                    var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        isMoreToRead = false;
                        Finish = true;
                    }
                    else
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        await fileStream.FlushAsync();
                        sw.Stop();
                        this.TotalBytes += read;
                        this.SpeedBytesSecs = (long)(read / sw.Elapsed.TotalSeconds);
                        Chunk_dl_speeds.Push((int)this.SpeedBytesSecs);
                        if (Chunk_dl_speeds.Count == 5)
                        {
                            if (Chunk_dl_speeds.Peek() <= 5120) //Cancel if download speed is less than 5 KB/S (5120 Bytes per sec)
                            {
                                CancelDownloads();
                                Chunk_dl_speeds.Clear();
                                Aborted = true;
                                this._abort_message = string.Format("The download speed is too low(less than 5KB/s)");
                                break;
                            }
                            Chunk_dl_speeds.Clear();
                        }
                    }
                    sw.Reset();
                }
                while (isMoreToRead);
            }


        }
        /// <summary>
        /// Cancel all pending downloads
        /// </summary>
        public void CancelDownloads()
        {
            client.CancelPendingRequests();
        }


        /// <summary>
        /// Handler to start downloading files
        /// </summary>

        public void Start()
        {
            _ = FetchLargeFiles();
        }
    }
}
