using Lemuria.Core.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lemuria.Core.Server
{
    public class SBoot
    {
        private static string pathToJsonFiles;
        public static List<MessageReplyModels> MessageReplies { get; set; }
        public static List<MeVitaeNLPMappingModels> MeVitaeNLPMappingModels { get; set; }

        public static void Configure(IHostingEnvironment env)
        {
            pathToJsonFiles = env.ContentRootPath
                + Path.DirectorySeparatorChar.ToString()
                + "Data"
                + Path.DirectorySeparatorChar.ToString();

            LoadServerSideJsonFiles();
        }

        public static void LoadServerSideJsonFiles()
        {
            var jsonString = "";

            var messageRepliesPath = pathToJsonFiles + "MessageReplies.json";
            var nlpMappingPath = pathToJsonFiles + "NLPMapping.json";

            // Now get all countries
            using (StreamReader SourceReader = File.OpenText(messageRepliesPath))
            {
                jsonString = SourceReader.ReadToEnd();
            }

            MessageReplies = JsonConvert.DeserializeObject<List<MessageReplyModels>>(jsonString);

            // Now get all cities
            using (StreamReader SourceReader = System.IO.File.OpenText(nlpMappingPath))
            {
                jsonString = SourceReader.ReadToEnd();
            }

            MeVitaeNLPMappingModels = JsonConvert.DeserializeObject<List<MeVitaeNLPMappingModels>>(jsonString);
        }
    }
}
