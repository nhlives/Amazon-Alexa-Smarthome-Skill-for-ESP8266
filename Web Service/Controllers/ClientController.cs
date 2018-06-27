#define NOVERBOSEDEBUG
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmarthomeSkillsWebAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
// AWS logging instructions: https://stackoverflow.com/questions/42586754/how-to-debug-or-see-output-for-a-net-core-app-deployed-in-elastic-beanstalk/46146001#46146001

namespace SmarthomeSkillsWebAPI.Controllers
{
}

[Route("api/[controller]")]
public class ClientController : Controller
{

    // constructor initalizations
   
    
    private readonly JObject invalidAuthToken = JObject.Parse(@"{ 'Result': 'invalid request'}");
    private readonly JObject successReturn = JObject.Parse(@"{ 'Result': 'success'}");
    private readonly JObject noResponseReturn = JObject.Parse(@"{ 'Result': 'empty queue'}");   
    private readonly JObject failedReturn = JObject.Parse(@"{ 'Result': 'failed'}");    
    private readonly JObject notFoundReturn = JObject.Parse(@"{ 'Result': 'not found'}");
    private readonly JObject notActiveReturn = JObject.Parse(@"{ 'Result': 'client inactive'}");
  

    private static string host = "";
    private static string fingerprint = "";
    private static int delayperiod = 0;

    //This request comes from the Uno and returns a request JSON with populated data
    [HttpGet("request")]
    public JsonResult Getrequest(string key)
    {
        Task<string> secret = AWSDynamoDB.QueryByClientGuid(key);
        // The 8266 uses 'Basic' authenication so be have to Base64 decode it and account for the user name
        // get basic authenication token
        string appToken = Request.Headers["Authorization"];
        string auth = appToken.Substring(6);
        string decoded = Base64Decode(auth);
        if(secret.Result != decoded.Substring(7))
        {
            return Json(invalidAuthToken);
        }

        Task<List<RequestQ>> requests = AWSDynamoDB.FetchQItem(key);
                   
        if (requests.Result.Count > 0 )
        {
            AWSDynamoDB.PutMonitor(key, false); //add this client to the list of active endpointId's
            string jsonResult = JsonConvert.SerializeObject(requests.Result[0]);
            return Json(JObject.Parse(jsonResult));         
        }
        else
        {
            AWSDynamoDB.PutMonitor(key, true);
            return Json(noResponseReturn);
        }

    } //Getrequest

    //encode
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    //decode
    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }


    [HttpGet("newrequest")]
        public JsonResult Getnewrequest(string directive, string MessageId, string correlationToken, string key, string endpointId)
        {

        Globals.logger.LogInformation("Entering 'Getnewrequest' using key: "+ key + " enpoindId: " + endpointId );
        // get Bearer authenication token
        string appToken = Request.Headers["Authorization"];

         if (!CheckAuth(Globals.appSecret))
         {
            return Json(invalidAuthToken);
         }
            
            Task<ClientGuidRequest> ClientGuid = AWSDynamoDB.GetClientGuid(key);

            if(ClientGuid.Result != null)
            {
                if (ClientGuid.Result.ActiveStatus)
                {
                    Task<bool> result = AWSDynamoDB.AddtoRequestQ(directive, MessageId, correlationToken, ClientGuid.Result.ClientGuid, endpointId, ClientGuid.Result.secret);
                    if (result.Result)
                    {
                        return Json(successReturn);
                    }
                    else
                    {
                        return Json(failedReturn);
                    }
                } //(ClientGuid.Result.ActiveStatus)
            else { return Json(notActiveReturn); }
            }
            else //ClientGuid.Result != null
            {
                return Json(notFoundReturn);
            }         
        }
        
        [HttpGet("fetch")]
   
        public JsonResult Getfetch(string email )
        {
        Globals.logger.LogInformation("Entering 'fetch' using email: " + email);
        Globals.logger.LogInformation(Globals.appSecret);
        if (!CheckAuth(Globals.appSecret))
        {
            return Json(invalidAuthToken);
        }

        Client client = AWSDynamoDB.FetchItem(email).Result;
            
            if (client == null){ return Json(new { }); }
            else
            {
                JObject jobject = (JObject)JToken.FromObject(client);
                return Json(jobject);
            }                     
        } // end [HttpGet("fetch")]

        [HttpGet("discover")]
        public JsonResult Getdiscover(string amazn_email)
        {
        Globals.logger.LogInformation("Entering 'Getdiscover' using amazn_email: " + amazn_email);
            if (!CheckAuth(Globals.appSecret))
            {
                return Json(invalidAuthToken);
            }       

            AmaznRequest req = new AmaznRequest();
            {
                req.Amazn_email = amazn_email;
            }

            Task<bool> ifExists = AWSDynamoDB.ItemExists(req.Amazn_email.ToString());
        
            if (ifExists.Result)
            {

                Task<bool> result = AWSDynamoDB.AmaznLink(req);

                
                if (result.Result)
                {
                    return Json( successReturn);
                }
                else
                {
                    return Json(failedReturn);
                }
            }
            else
            {
                return Json(notFoundReturn);

            }
        } // end  [HttpGet("discover")]

    // from Uno
        [HttpGet("Global")]
        public JsonResult GetGlobalRequest(string key, string version)
        {
            Globals.logger.LogInformation("Entering 'GetGlobalRequest' using version: " + version);
            dynamic globals = JObject.Parse(@"{ 'host': '','fingerprint': '','delayperiod': 0}");
            globals.host = host;
            globals.fingerprint = fingerprint;
            globals.delayperiod = delayperiod;

            return Json(globals);

    }

        [HttpGet("QueueRequestStatus")]
        public JsonResult GetQueueRequestStatus(string key)
        {

#if VERBOSEDEBUG 
        Globals.logger.LogInformation("Entering 'GetQueueRequestStatus' using key: " + key); 
#endif
        if (AWSDynamoDB.DeleteQbyClientGuid(key).Result)
                {
                    return Json(successReturn);
                }
                else
                {
                    return Json(notFoundReturn);
                }
        }


    // GET: api/<controller>
    [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Alexa Skill" };
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            switch(id)
            {
                case 1: return JsonConvert.SerializeObject(AWSDynamoDB.GetMonitor());
                default: return "Alexa Skill";
            }
           
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        bool CheckAuth(string secret)
        {
        string appToken = Request.Headers["Authorization"];
        if (appToken != null)
        {
            string authType = appToken.Substring(0, 6);
            authType = authType.ToUpper();
            string tok = appToken.Substring(7);
            if (authType != "BEARER" || (appToken.Substring(7) != secret))
            {
                return false;
            }
            else return true;
        }
        else { return false; }

    }
    }

