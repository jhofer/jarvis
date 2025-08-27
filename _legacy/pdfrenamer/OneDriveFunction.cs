using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PDFRenamerIsolated;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using PDFRenamer.Services;
using PDFRenamerIsolated.Services;
using Microsoft.Graph.Models;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig;
using System.Collections.Concurrent;

namespace PDFRenamer
{

    public class Scan
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string NewName { get; set; }
        public string NewPath { get; set; }
        public object Summary { get; internal set; }
    }
    public class OneDriveFunction

    {
        private readonly ILogger<OneDriveFunction> log;
        private readonly IOneDrive oneDrive;
        private readonly IAI ai;
        private readonly IAccessRepository accessRepo;
        private readonly IHttpClientFactory httpClientFactory;

        public OneDriveFunction(ILogger<OneDriveFunction> logger, IOneDrive oneDrive, IAI ai)

        {
            this.log = logger;
            this.oneDrive = oneDrive;
            this.ai = ai;
        }

        public static string codeVerifier;


        [Function("GetFiles")]
        public async Task<List<Scan>> GetFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            var folder = await oneDrive.GetFolders("jonas");
            var files = await oneDrive.GetFiles("jonas");
            //var results = new List<Scan>();


            var results = new ConcurrentBag<Scan>();
            await Task.WhenAll(files.Select(async file =>
            {
                log.LogInformation($"File: {file.Name}");
                // log.LogInformation("Content: " + file.Content);
                var aiResult = await ai.ExtractTitleAsync(new AIRequest(file.Content, folder.GetPath()));
                var scanResult = new Scan()
                {
                    Name = file.Name,
                    Path = file.Path,
                    Summary = aiResult.Summary,
                    NewName = aiResult.Title,
                    NewPath = aiResult.Path
                };
                results.Add(scanResult);
                log.LogInformation($"ScanResult: {scanResult.NewName}");

            }));


            await Task.WhenAll(results.Select(async result =>
            {
                await oneDrive.MoveFile(result.Path, result.NewPath);
            }));

            return results.ToList();


        }


    }
}
