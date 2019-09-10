# AsyncDownloader
AsyncDownloader is a .NET Core library whose main purpose is to download files irrespective of the filesize in an efficient manner.Files are downloaded asynchrnously and contents are streamed to disk.

# Features

# Properties

`SpeedBytesSecs` - Returns the current download speed in bytes per seconds.

`PrettyPrintSpeed` - Returns the current download speed in human readable fashion like in KB/s , MB/s , GB/s  

`MaxConcurrentDownloads` - Get or Set the maximum number of concurrent downloads 

`UserAgent` -  Get or Set the useragent to use when making the request to download the file.By default no useragent is used.

`Finish` - Returns true if download is complete , false otherwise

`Aborted` - Returns true if download has been aborted / cancel , false otherwise

`Filename` - Return the filename of the download file from the content-disposition header.If no such header is present, then get the filename from the path.If the filename already exist in the directory , then append a uniquely generated string to the file

`TotalBytes` -  Return the downloaded bytes downloaded so far in bytes/sec

`PrettyPrintTotalBytes` - Return the downloaded bytes downloaded so far in human readable fashion like in KB/s , MB/s , GB/s

`BufferSize` -  Get or Set the buffer size for the filestream . The size by default is 16kB

`Url` - Get or set the url of the file to download

`Location` - Get or set the download path where to store the download file.By default , download to current directory

# Methods

`Start()` - Handler method to start downloading

`CancelDownloads()` - Cancel all pending downloads

`AbortException()` - Throw an Exception if a download has been aborted

# Usage

Build the solution and add a reference to the `AsyncDownloader.dll` to your solution. 

```using System;

namespace AsyncDownloaderSample
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncDownloader.AsyncDownloader dl = new AsyncDownloader.AsyncDownloader();
            dl.Url = "http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso";
            dl.Start();
            Console.WriteLine("Downloading started");
            while (!dl.Finish && !dl.Aborted)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r"); //clears the current line
                Console.Write("Total bytes so far " + dl.PrettyPrintTotalBytes + " . DL Speed is " + dl.PrettyPrintSpeed);
                System.Threading.Thread.Sleep(1000);
            }
            if (dl.Finish)
            {
                Console.WriteLine("Downloaded  " + dl.FileName + " successfully in " + dl.Location);
            }
            else if (dl.Aborted)
            {
                dl.AbortException();
            }

        }
    }
}

```

More examples can be found in the `AsyncDownloaderTest/AsyncDownloaderTest.cs` file

# License

```MIT License

Copyright (c) 2019 Khavish Anshudass Bhundoo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
 
 
