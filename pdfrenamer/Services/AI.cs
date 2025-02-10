using Azure.AI.OpenAI;
using Azure.Identity;
using Newtonsoft.Json;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFRenamerIsolated.Services
{


    public class AIRequest
    {
        public AIRequest(string content, string[] folderStructure)
        {
            Content = content;
            FolderStructure = folderStructure;
        }
        public string Content { get; set; }
        public string[] FolderStructure { get; set; }
    }

    public class AIResult
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string Summary { get; set; }
    }
    public class AI : IAI
    {
        private ChatClient chatClient;
        private SystemChatMessage systemMessage = new SystemChatMessage(
            @"Sie sind KI-Assistent und helfen Personen PDF aufgrund des Inhalts sinnvoll abzulegen.
            Sie definieren den neuen Titel und einen passenden Ablagepfad und eine kurze Zusammenfassung.
            Sie befolgen immer folgende Regeln:
            - Der Titel startet immer mit einem Timestamp in folgendem Format: YYYY-MM-DD
            - Personen namen komme nicht im Titel vor.
            - Als Pfad wird wenn möglich ein Wert aus dem Property 'FolderStructure' gewählt. 
              Andernfalls wird ein neuer Pfad passend zur besthenden Ordnerstrukur gewählt. 
            - Die Anwort wird immer in folgendem JSON Format geliefert:
            {
                ""title"": ""new title of the pdf"",
                ""path"": ""/folder/path/"",
                ""summary"": ""short summary of the content""
            }"
        )
        {

        };

        public AI()
        {
            var options = new DefaultAzureCredentialOptions { ExcludeVisualStudioCredential = true };

            AzureOpenAIClient azureClient = new(
    new Uri("https://openai-pers-assist-poc.openai.azure.com/"),
    new DefaultAzureCredential(options));
            this.chatClient = azureClient.GetChatClient("gpt-4o-mini-2");

        }
        public async Task<AIResult> ExtractTitleAsync(AIRequest request)
        {
            var text = JsonConvert.SerializeObject(request);


            ChatCompletion result = await chatClient.CompleteChatAsync([
                        systemMessage,
                        new UserChatMessage(text)
                    ]
            );


            var markdown = result.Content[0].Text;
            var jsonStart = markdown.IndexOf("{");
            var jsonEnd = markdown.LastIndexOf("}");
            var json = markdown.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var aiResult = JsonConvert.DeserializeObject<AIResult>(json);
            if (aiResult == null)
            {
                throw new Exception("Could not extract title");
            }
            return aiResult;

        }
    }
}
