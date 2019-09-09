using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace AsyncDownloaderTest
{
    [TestClass]
    public class AsyncDownloaderTest
    {
        [TestMethod]
        public void Test_SuccessfulDownload()
        {
            //Arrange
            AsyncDownloader.AsyncDownloader dl = new AsyncDownloader.AsyncDownloader();
            dl.Url = "http://www.ovh.net/files/1Mio.dat";
            //Act
            dl.Start();
            while (!dl.Finish)
            {
                System.Threading.Thread.Sleep(500);
            };
            //Assert
            Assert.IsTrue(File.Exists(dl.Location + Path.DirectorySeparatorChar + dl.FileName));
            File.Delete(dl.Location + Path.DirectorySeparatorChar + dl.FileName);
        }

        [TestMethod]
        public void Test_AbortedDownload()
        {
            //Arrange
            AsyncDownloader.AsyncDownloader dl = new AsyncDownloader.AsyncDownloader();
            dl.Url = "http://www.ovh.net/files/101Mb.dat";
            //Act
            dl.Start();
            while (!dl.Aborted)
            {
                System.Threading.Thread.Sleep(500);
            };
            //Assert
            Assert.ThrowsException<Exception>(dl.AbortException);

        }

        [TestMethod]
        public void Test_SuccessfulDownloadWithUserAgent()
        {
            //Arrange
            AsyncDownloader.AsyncDownloader dl = new AsyncDownloader.AsyncDownloader();
            dl.Url = "http://www.ovh.net/files/1Mio.dat";
            dl.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36";
            //Act
            dl.Start();
            while (!dl.Finish)
            {
                System.Threading.Thread.Sleep(500);
            };
            //Assert
            Assert.IsTrue(dl.UserAgent != null);
            File.Delete(dl.Location + Path.DirectorySeparatorChar + dl.FileName);
        }

        [TestMethod]
        public void Test_SuccessfulMultipleDownloads()
        {
            //Arrange
            AsyncDownloader.AsyncDownloader dl = new AsyncDownloader.AsyncDownloader();
            dl.Url = "http://www.ovh.net/files/10Mio.dat";

            AsyncDownloader.AsyncDownloader dl2 = new AsyncDownloader.AsyncDownloader();
            dl2.Url = "http://speedtest.tele2.net/10MB.zip";
            //Act
            //Downloads the files asynchronously
            dl.Start();
            Console.WriteLine("Downloading started at {0}", DateTime.UtcNow);
            dl2.Start();
            Console.WriteLine("Downloading started at {0}", DateTime.UtcNow);
            while (!dl.Finish && !dl2.Finish)
            {
                System.Threading.Thread.Sleep(500);
            };
            //Assert
            Assert.IsTrue(File.Exists(dl.Location + Path.DirectorySeparatorChar + dl.FileName));
            Assert.IsTrue(File.Exists(dl2.Location + Path.DirectorySeparatorChar + dl2.FileName));
            File.Delete(dl.Location + Path.DirectorySeparatorChar + dl.FileName);
            File.Delete(dl2.Location + Path.DirectorySeparatorChar + dl2.FileName);
        }

    }
}
