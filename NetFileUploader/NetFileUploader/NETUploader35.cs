using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetFileUploader
{
    /// <summary>
    /// First checks if any of the files on the ftp server would conflict with the file(s)
    /// current name, and handles it. Then uploads all files asynchronously.
    /// </summary>
    public class NETUploader45
    {
        private readonly string ftpAddress;
        private readonly string ftpDestinationPath;
        private readonly string ftpUserName;
        private readonly string ftpPassword;

        public NETUploader45(string ftpAddress, string ftpDestinationPath, string ftpUserName, string ftpPassword)
        {
            this.ftpAddress = ftpAddress;
            this.ftpDestinationPath = ftpDestinationPath;
            this.ftpUserName = ftpUserName;
            this.ftpPassword = ftpPassword;
        }

        public async void UploadFile(IEnumerable<string> localFilePaths)
        {
            var dirFiles = GetFtpFileList();
            var tasks = new List<Task>();
            foreach (string localFilePath in localFilePaths)
            {
                string ftpFileName = SetFtpFileName(localFilePath, dirFiles);
                tasks.Add(GetTask(localFilePath, ftpFileName));
            }
            Console.WriteLine("Uploading all files...");
            await Task.WhenAll(tasks.ToArray()).ContinueWith(a => Console.WriteLine(a.Status == TaskStatus.RanToCompletion ? "All files uploaded!" : "There were issues uploading the files, following is the error:" + a.Exception));
        }

        private Task GetTask(string localFilePath, string ftpFileName)
        {
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                client.UploadFileCompleted += (sender, e) => Console.WriteLine("Upload completed for " + ftpFileName + ": " + Encoding.UTF8.GetString(e.Result));
                return client.UploadFileTaskAsync(ftpAddress + ftpDestinationPath + ftpFileName, localFilePath);
            }
        }

        private string SetFtpFileName(string localFilePath, IEnumerable<string> dirFiles)
        {
            var ext = Path.GetExtension(localFilePath);
            var fName = Path.GetFileNameWithoutExtension(localFilePath);
            var similarFileNames = dirFiles.Where(s => s.Contains(fName) && s.Contains('_')).ToList();
            int max = 0;
            if (similarFileNames.Count > 0)
            {
                Console.WriteLine("Similar file name using _ found on ftp for: " + fName);
                foreach (var name in similarFileNames)
                {
                    int curMax = 0;
                    string lastNumber = name.Substring(name.LastIndexOf('_') + 1).Trim();
                    int endIndex = lastNumber.LastIndexOf('.');

                    string s = lastNumber.Remove(endIndex != -1 ? endIndex : lastNumber.Length - 1).Trim();
                    int.TryParse(s, out curMax);
                    if (max < curMax)
                        max = curMax;
                }
                string result = fName + "_" + (max + 1) + ext;
                Console.WriteLine("Updating file name to " + result);
                return result;
            }
            else if (dirFiles.Any(s => localFilePath == s))
            {
                string result = fName + "_" + max + ext;
                Console.WriteLine("Same file name found, updating name to " + result);
                return result;
            }
            return localFilePath;
        }

        private IEnumerable<string> GetFtpFileList()
        {
            Console.WriteLine("Retrieving ftp file list...");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAddress + ftpDestinationPath);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(ftpUserName, ftpPassword);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        IList<string> directories = new List<string>();
                        string line = reader.ReadLine();
                        while (!reader.EndOfStream)
                        {
                            Console.WriteLine("File found: " + line);
                            directories.Add(line);
                            line = reader.ReadLine();
                        }
                        return directories;
                    }
                }
            }
        }
    }
}
