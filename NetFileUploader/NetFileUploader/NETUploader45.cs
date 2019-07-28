using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace NetFileUploader
{
    /// <summary>
    /// First checks if any of the files on the ftp server would conflict with the file(s)
    /// current name, and handles it. Then uploads the single file to the specified ftp.
    /// </summary>
    public class NETUploader35
    {
        private readonly string ftpAddress;
        private readonly string ftpDestinationPath;
        private readonly string ftpUserName;
        private readonly string ftpPassword;

        public NETUploader35(string ftpAddress, string ftpDestinationPath, string ftpUserName, string ftpPassword)
        {
            this.ftpAddress = ftpAddress;
            this.ftpDestinationPath = ftpDestinationPath;
            this.ftpUserName = ftpUserName;
            this.ftpPassword = ftpPassword;
        }

        public void UploadFile(string localFilePath)
        {
            var dirFiles = GetFileList();
            string ftpFilePath = SetFtpFilePath(localFilePath, dirFiles);
            var file = new FileInfo(localFilePath);
            var ftp = (FtpWebRequest)WebRequest.Create(ftpAddress + ftpDestinationPath + ftpFilePath);
            ftp.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
            ftp.EnableSsl = false;
            ftp.KeepAlive = false;
            ftp.UseBinary = true;
            ftp.UsePassive = true;
            ftp.Timeout = 10000000;
            ftp.ReadWriteTimeout = 10000000;
            ftp.Method = WebRequestMethods.Ftp.UploadFile;
            using (var fileStream = file.OpenRead())
            {
                using (var stream = ftp.GetRequestStream())
                {
                    int bufferSize = (int)Math.Min(fileStream.Length, 2048);
                    var buffer = new byte[bufferSize];
                    int bytesRead = fileStream.Read(buffer, 0, bufferSize), bytesWritten = 0;
                    Console.WriteLine("Uploading file: " + ftpFilePath);

                    do
                    {
                        stream.Write(buffer, 0, bufferSize);
                        bytesWritten += bytesRead;
                        Console.WriteLine(bytesWritten + " bytes written out of " + fileStream.Length);
                        bytesRead = fileStream.Read(buffer, 0, bufferSize);
                    }
                    while (bytesRead != 0);
                    Console.WriteLine("Finished uploading file " + ftpFilePath);
                }
            }
        }

        private string SetFtpFilePath(string localFilePath, IEnumerable<string> dirFiles)
        {
            var ext = Path.GetExtension(localFilePath);
            var fName = Path.GetFileNameWithoutExtension(localFilePath);
            var similarFileNames = dirFiles.Where(s => s.Contains(fName) && s.Contains('_') && s.Contains('.')).ToList();
            int max = 0;
            if (similarFileNames.Count > 0)
            {
                Console.WriteLine("Similar file name using _ found on ftp for: " + fName);
                foreach (var name in similarFileNames)
                {
                    int temp = 0;
                    string lastNumber = name.Substring(name.LastIndexOf('_') + 1).Trim();
                    string s = lastNumber.Remove(lastNumber.LastIndexOf('.')).Trim();
                    int.TryParse(s, out temp);
                    if (max < temp)
                        max = temp;
                }
                string result = fName + "_" + (max + 1) + ext;
                Console.WriteLine("Updating file name to " + result);
                return fName + "_" + (max + 1) + ext;
            }
            else if (dirFiles.Any(s => localFilePath == s))
            {
                string result = fName + "_" + max + ext;
                Console.WriteLine("Same file name found, updating name to " + result);
                return result;
            }
            return localFilePath;
        }

        private IEnumerable<string> GetFileList()
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
