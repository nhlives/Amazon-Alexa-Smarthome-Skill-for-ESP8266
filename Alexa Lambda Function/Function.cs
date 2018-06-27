using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;




// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AlexSmarthomeLambdaFunction
{
    public class Function
    {
        public JObject FunctionHandler(Stream input, ILambdaContext context)
        {

            // get secrets
            Task<string> hostName = Helper.GetSecret("WebServiceHostNameProduction");
            Helper.host = "https://beta." + hostName.Result + "/api/client/";
            Task<string> appSecret = Helper.GetSecret("BearerToken");
            Helper.secret = appSecret.Result;

        //  read the json 
        string json = null;
            Helper.inputObj = null;
       
                JObject response = JObject.Parse(@"{}");
     
            try
            {
                StreamReader reader = new StreamReader(input);
                json = reader.ReadToEnd();
            }
            catch { Helper.Logger(Constants.SEVERE, "Error streaming input"); };

            try{ Helper.inputObj = JObject.Parse(json); }
            catch { Helper.Logger(Constants.SEVERE, "Error sparsing input json"); }

            if (Helper.inputObj.directive.header.name == "Discover" && Helper.inputObj.directive.header.@namespace == "Alexa.Discovery")
                        { response = Helper.DoDiscover(); }
            else if (Helper.inputObj.directive.header.name == "AcceptGrant" && Helper.inputObj.directive.header.@namespace == "Alexa.Authorization")
                        { response = Helper.DoAcceptGrant(); }
            else if (Helper.inputObj.directive.header.name == "TurnOn" && Helper.inputObj.directive.header.@namespace == "Alexa.PowerController")
                        { response = Helper.DoTurnOn(); }          
            else 
                {  // we got a unexpected directive
                Helper.Logger(Constants.INFO, "Got an unexpected directive: " + Helper.inputObj.directive.header.@namespace + "/" + Helper.inputObj.directive.header.name);
                }

            return response;
        }


    }
   
}
