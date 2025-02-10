using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using PDFRenamerIsolated.Services.GraphClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig;
using System.IO;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;


namespace PDFRenamerIsolated.Services
{
    public class OneDriveFolder
    {
        public OneDriveFolder(DriveItem item)
        {
            Name = item.Name;
            Path = item.Root != null ? "/" : item.ParentReference.Path.Split(":").Last() + "/" + item.Name;

        }

        public string Name { get; set; }
        public List<OneDriveFolder> Childs { get; set; } = new List<OneDriveFolder>();
        public string Path { get; set; }

        public string[] GetPath()
        {
            List<string> paths = new();
            paths.Add(Path);
            var childPaths = Childs.SelectMany(c => c.GetPath());
            paths.AddRange(childPaths);
            return paths.ToArray();
        }
    }

    public class OneDrivePdf
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
    }

    public interface IOneDrive
    {

        Task<List<OneDrivePdf>> GetFiles(string user);
        Task<OneDriveFolder> GetFolders(string v);
        Task MoveFile(string path, string newPath);

    }


    public class OneDrive : IOneDrive
    {
        private ICache cache;
        private ILogger<OneDrive> logger;
        private ITokenProvider tokenProvider;

        public OneDrive(ILogger<OneDrive> logger, ITokenProvider tokenProvider, ICache cache)
        {
            this.cache = cache;
            this.logger = logger;
            this.tokenProvider = tokenProvider;
        }


        public async Task<List<OneDrivePdf>> GetFiles(string user)
        {
            var accessToken = await tokenProvider.GetAccessToken(user);
            var authProvider = new CustomAuthenticationProvider(accessToken);
            var graphClient = new GraphServiceClient(authProvider);

            // Get the user's driveId
            var drive = await graphClient.Me.Drive.GetAsync();
            var userDriveId = drive.Id;

            // Use the driveId to get the root drive
            string folderPath = "/From_BrotherDevice";
            var folderItem = await graphClient.Drives[userDriveId].Root.ItemWithPath(folderPath).GetAsync();
            var items = await graphClient.Drives[userDriveId].Items[folderItem.Id].Children.GetAsync();
            var result = items?.Value?.Where(e => e != null) ?? new List<DriveItem>();

            var tasks = result.Select(async item =>
            {
                var stream = await graphClient.Drives[userDriveId].Items[item.Id].Content.GetAsync();
                var content = await GetPdfContent(stream);
                return new OneDrivePdf
                {
                    Name = item.Name,
                    Path = item.ParentReference.Path,
                    Content = content
                };
            });

            var scans = await Task.WhenAll(tasks);
            return scans.ToList();
        }

        public async Task<OneDriveFolder> GetFolders(string user)
        {
            if (TryGetFolder(user, out var folder))
            {
                return folder!;
            }
            var accessToken = await tokenProvider.GetAccessToken(user);
            var authProvider = new CustomAuthenticationProvider(accessToken);
            var graphClient = new GraphServiceClient(authProvider);
            // Verzeichnisname (z. B. "Documents" oder ein beliebiges Unterverzeichnis)

            // Get the user's driveId
            var drive = await graphClient.Me.Drive.GetAsync();
            var userDriveId = drive.Id;





            var driveItem = await graphClient.Drives[userDriveId].Root.GetAsync();
            var root = await GetDriveFolders(graphClient, userDriveId, driveItem);

            CacheFolder(user, root);
            return root;
        }

        private void CacheFolder(string user, OneDriveFolder root)
        {
            this.cache.Add(user + "_Folder", root);
        }

        private bool TryGetFolder(string user, out OneDriveFolder? folder)
        {
            return this.cache.TryGet(user + "_Folder", out folder);
        }

        private async Task<OneDriveFolder> GetDriveFolders(GraphServiceClient graphClient, string? userDriveId, DriveItem item)
        {
            if (item.Folder == null)
            {
                throw new ArgumentException("Item is not a folder");
            }
            var folder = new OneDriveFolder(item);

            var driveItems = await graphClient.Drives[userDriveId].Items[item.Id].Children.GetAsync();

            var tasks = driveItems?.Value
                .Where(childItem => childItem.Folder != null)
                .Select(childItem => GetDriveFolders(graphClient, userDriveId, childItem))
                .ToList();

            if (tasks != null)
            {
                var childFolders = await Task.WhenAll(tasks);
                folder.Childs.AddRange(childFolders);
            }

            return folder;
        }


        private async Task<string> GetPdfContent(Stream? pdfStream)
        {
            if (pdfStream == null) return String.Empty;

            if (pdfStream.CanSeek)
            {
                pdfStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {

                var memoryStream = new MemoryStream();
                await pdfStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                pdfStream = memoryStream;

            }

            using (var pdfDocument = PdfDocument.Open(pdfStream))
            {
                var text = new System.Text.StringBuilder();

                foreach (var page in pdfDocument.GetPages())
                {
                    text.AppendLine(page.Text);
                }

                return text.ToString();
            }
        }


        public async Task MoveFile(string path, string newPath)
        {
            // Get the user's driveId



            var accessToken = await tokenProvider.GetAccessToken("jonas");
            var authProvider = new CustomAuthenticationProvider(accessToken);
            var graphClient = new GraphServiceClient(authProvider);

            var drive = await graphClient.Me.Drive.GetAsync();
            var userDriveId = drive.Id;


        }
    }
}

