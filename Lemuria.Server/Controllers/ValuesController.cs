using Lemuria.Server.Models;
using Lemuria.Server.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;

namespace Lemuria.Server.Controllers
{
    public class ValuesController : ApiController
    {
        static SpeechServices speechService;
        static Timer MovementTimer { get; set; }

        public ValuesController()
        {
            if(MovementTimer == null)
                MovementTimer = new Timer();
        }

        private void InitializeShastaSpeech()
        {
            if (speechService == null)
            {
            //    speechService = new SpeechServices();
            //    if (!speechService.HasInitialized)
            //        speechService.GetAllJsonData();
            }
        }

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            List<MessageReplyModels> models = new List<MessageReplyModels>();
            if (id == 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    models.Add(new MessageReplyModels
                    {
                        ReceivedMessage = "Hello" + i,
                        ReplyMessage = "Hi" + i,
                        Methods = new Dictionary<string, List<string>>()
                    {
                        { "Speak", new List<string> { "Hi" + i } }
                    }
                    });
                }

                return JsonConvert.SerializeObject(models);
            }
            else if(id==2)
            {
                string json = "[{\"ReceivedMessage\":\"Hello0\",\"ReplyMessage\":\"Hi0\",\"Methods\":{\"Speak\":[\"Hi0\"]}},{\"ReceivedMessage\":\"Hello1\",\"ReplyMessage\":\"Hi1\",\"Methods\":{\"Speak\":[\"Hi1\"]}}]";
                models = JsonConvert.DeserializeObject<List<MessageReplyModels>>(json);
                return "Converted";
            }
            else if(id==3)
            {              
                using (StreamReader sr = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/Data/MessageReplies.json")))
                {
                    models = JsonConvert.DeserializeObject<List<MessageReplyModels>>(sr.ReadToEnd());
                }
                return "Converted";
            }
            else if(id == 4)
            {
                InitializeShastaSpeech();
                return "Initialize set";
            }
            else if (id == 5)
            {
                if (speechService.HasInitialized)
                    return JsonConvert.SerializeObject(speechService.MessageReplies);
                return "Not Initialized";
            }
            else if(id == 6)
            {
                string message = "please change the volume to 30";
                return speechService.ProcessMessage(message);
            }
            else
            {
                MovementTimer.Stop();
                MovementTimer.Elapsed += MovementTimer_Elapsed;
                MovementTimer.Interval = 10000;
                MovementTimer.Start();
                MovementTimer.AutoReset = false;
                return "Timer dispose initialized";
            }
        }


        private void MovementTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string call = "I am called";
        }

        public string Get(string message)
        {
            return speechService.ProcessMessage(message);
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
