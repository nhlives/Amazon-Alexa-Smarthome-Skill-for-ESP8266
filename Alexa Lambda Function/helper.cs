#define TEST

using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon;
using System.Threading.Tasks;

namespace AlexSmarthomeLambdaFunction
{
    
    public static class Helper
    {                           
        public static dynamic inputObj;
#if  TEST 
        public static string host;
#else
        public static Task<string> hostName;
        public static string host = "https://" + hostName.Result + "/api/client/";
#endif
       
        public static string amznProfileHost = "https://api.amazon.com/user/profile";


        public static string secret;
        public static WebClient client = new WebClient();

        public static JObject DoDiscover()
        {
            Logger(Constants.DEBUG, "Entering DoDiscovery");
            Logger(Constants.INFO, JsonConvert.SerializeObject(inputObj));
            string token = inputObj.directive.payload.scope.token;
            Logger(Constants.DEBUG, token);

            JObject profileJson = GetAmznProfile(token);
            dynamic amaznProfile = profileJson;
                
            if (amaznProfile.email != "error")
            {
                         
                string command = "fetch?email=" + amaznProfile.email;
                string url = host + command;

                try
                {
                    client.Headers.Remove("Authorization");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to modify http header 'Authorization' in 'DoDiscover'"); }
                finally { }

                try
                {
                    client.Headers.Add("Authorization", "bearer " + secret);
                }
                catch { Logger(Constants.INFO, "Exception caught trying to modify http header 'user-agent' in 'DoDiscover'"); }
                finally { }

                try
                {
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to modify http header 'user-agent' in 'DoDiscover'"); }
                finally { }



                string body = GetUrl(url);
                if (body != null)
                {

                    dynamic clientProfile = JsonConvert.DeserializeObject(body);
                    dynamic endpoint = JsonConvert.DeserializeObject(Json.endpointModel);
                                   
                    foreach (string appliance in clientProfile.Appliance)
                    {
                        dynamic endpointId = JsonConvert.DeserializeObject(Json.endpointIdModel);
                        endpointId.endpointId = appliance;
                        endpointId.friendlyName = appliance;
                        endpoint.payload.endpoints.Add(endpointId);
                 
                    }

                    dynamic @event = JObject.Parse(Json.EventDiscoveryheader);

                    @event.@event.payload = endpoint.payload;
                    @event.@event.header.messageId = inputObj.directive.header.messageId;

              
                    JObject DiscoveryEventResponse = @event;

                    Logger(Constants.DEBUG, JsonConvert.SerializeObject(DiscoveryEventResponse));

                    return DiscoveryEventResponse;

                }//(body != null)

                return BuildErrorResponse("INTERNAL_ERROR", "failed to connect with backend");

            }//(profileJson != null)

            return BuildErrorResponse("INTERNAL_ERROR", "failed to connect with LWA");
        }

        public static JObject DoAcceptGrant()
        {
            Logger(Constants.DEBUG, "Entering DoAcceptGrant");
            Logger(Constants.INFO, JsonConvert.SerializeObject(inputObj));

            string token = inputObj.directive.payload.grantee.token;
           
            Logger(Constants.DEBUG, token);
            JObject profileJson = GetAmznProfile(token);

            dynamic amaznProfile = profileJson;

            if (amaznProfile.email != "error")
            {
              
                            
                //  amaznProfile.user_id;
                //  amaznProfile.name;
                //  amaznProfile.email;

                string command = "discover?amazn_email=" + amaznProfile.email;

             
                string url = host + command;

                Logger(Constants.DEBUG, url);

                try
                {
                    client.Headers.Remove("Authorization");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to remmove http header 'Authorization' in 'DoDiscover'"); }
                finally { }

                try
                {
                    client.Headers.Add("Authorization", "bearer " + secret);
                }
                catch { Logger(Constants.INFO, "Exception caught trying to add http header 'Authorization' in 'DoDiscover'"); }
                finally { }

                try
                {
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to modify http header 'user-agent' in 'DoDiscover'"); }
                finally { }


                string body = GetUrl(url);

                Logger(Constants.INFO, body);
                if (body != null)  //got a response
                {
                    dynamic result = JsonConvert.DeserializeObject(body);

                    Logger(Constants.DEBUG, body);
               
                    if (result.Result == "success")
                    {
                        dynamic acceptgrant = JObject.Parse(Json.acceptGrant);
                        acceptgrant.@event.header.messageId = inputObj.directive.header.messageId;
                        JObject jsonResult = acceptgrant;
                        Logger(Constants.DEBUG, JsonConvert.SerializeObject(acceptgrant));
                        return jsonResult;
                    }
                    else
                    {                        
                        return BuildErrorResponse("ACCEPT_GRANT_FAILED", "Failed to handle the AcceptGrant directive because request to backend databse failed"); ;
                    }                   
                } //  if (body != null)


            }  //  (profileJson != null)

            return BuildErrorResponse("ACCEPT_GRANT_FAILED", "Failed to handle the AcceptGrant directive because LWA failed");
           
             
        }

        public static JObject DoTurnOn()
        {
            Logger(Constants.DEBUG, "Entering DoTurnOn");
            Logger(Constants.INFO, JsonConvert.SerializeObject(inputObj));
           
            string token = inputObj.directive.endpoint.scope.token;
            JObject jsonResponse = null;

            JObject profileJson = GetAmznProfile(token);
            dynamic amaznProfile = profileJson;

            if (amaznProfile.email != "error")
            {
                //  amaznProfile.user_id;
                //  amaznProfile.name;
                //  amaznProfile.email;

                Logger(Constants.DEBUG, JsonConvert.SerializeObject(amaznProfile));
                string correlationToken = inputObj.directive.header.correlationToken;         
                Logger(Constants.DEBUG, correlationToken);
                string correlationTokenEncoded = WebUtility.UrlEncode(correlationToken);

                string command = "newrequest?"+ "directive=TurnOn" + "&messageid=" + inputObj.directive.header.messageId + "&correlationToken=" + correlationTokenEncoded + "&key=" + amaznProfile.email + "&endpointid="+ inputObj.directive.endpoint.endpointId;
                string url = host + command;
              
                Logger(Constants.DEBUG, url);

                try
                {
                    client.Headers.Remove("Authorization");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to remove http header 'Authorization' in 'DoTurnOn'"); }
                finally { }

                try
                {
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                }
                catch { Logger(Constants.INFO, "Exception caught trying to modify http header 'user-agent/Authorization' in 'DoTurnOn'"); }
                finally { }

                try
                {
                    client.Headers.Add("Authorization", "bearer " + secret);
                }
                catch { Logger(Constants.INFO, "Exception caught trying to add http header 'Authorization' in 'DoTurnOn'"); }
                finally { }

                Logger(Constants.DEBUG, client.Headers["Authorization"]);
                Logger(Constants.DEBUG, url);

                string body = GetUrl(url);
                Logger(Constants.INFO, body);
             

                if(body!=null)
                {
                    dynamic result = JsonConvert.DeserializeObject(body);

                    if (result.Result == "success")
                    {
                        dynamic turnOnResponse = JObject.Parse(Json.TurnOnResponse);

                        turnOnResponse.@event.header.messageId = inputObj.directive.header.messageId;
                        turnOnResponse.@event.header.@namespace = "Alexa";
                        turnOnResponse.@event.header.name = "Response";
                        turnOnResponse.@event.header.correlationToken = inputObj.directive.header.correlationToken;
                        turnOnResponse.@event.endpoint.scope.token = token;
                        turnOnResponse.@event.endpoint.endpointId = inputObj.directive.endpoint.endpointId;

                        jsonResponse = turnOnResponse;

                        Logger(Constants.DEBUG, JsonConvert.SerializeObject(jsonResponse));

                        return jsonResponse;
                    }
                    else
                    {
                        jsonResponse = BuildErrorResponse("INTERNAL_ERROR", "reached backend but had an error");
                        Logger(Constants.DEBUG, JsonConvert.SerializeObject(jsonResponse));

                        return jsonResponse;
                    }
                } //(body!=null)

                jsonResponse = BuildErrorResponse("INTERNAL_ERROR", "error contacting backend");
                Logger(Constants.DEBUG, JsonConvert.SerializeObject(jsonResponse));

                return jsonResponse;

            } //(profileJson != null)


            jsonResponse = BuildErrorResponse("INTERNAL_ERROR", "error contacting LWA");
            Logger(Constants.DEBUG, JsonConvert.SerializeObject(jsonResponse));

            return jsonResponse;
        }

        public static void Logger(int severity, string text)
        {
            if(severity > Constants.INFO)
            Console.WriteLine(severity.ToString() + ": " + text);
            return;
        }

        public static JObject BuildErrorResponse(string type, string message)
        {
            dynamic errorResponse = JObject.Parse(Json.errorResponse);
            errorResponse.@event.header.@namespace = inputObj.directive.header.@namespace;
            errorResponse.@event.header.messageId = inputObj.directive.header.messageId;
            errorResponse.@event.header.correlationToken = inputObj.directive.header.correlationToken;
            errorResponse.@event.payload.type = type;
            errorResponse.@event.payload.message = message;
            JObject jsonReturn = errorResponse;
            return jsonReturn;

        }

        public static JObject GetAmznProfile(string token)
        {
            try
            {
                client.Headers.Remove("user-agent");
                client.Headers.Remove("Authorization");
                client.Headers.Add("Authorization: bearer " + token);
            }
            catch { Logger(Constants.SEVERE, "Exception caught trying to modify http headers in \"GetAmznProfile\""); }
            finally { }

            string body = GetUrl(amznProfileHost);
            if (body != null)
            {
                JObject json = JObject.Parse(body);
                return json;
            }
            else
            {
                string error = @"{'user_id':'error','name':'error','email':'error'}";
                JObject json = JObject.Parse(error);
                return json;
            }
        }

        public static string GetUrl(string url)
        {
            Logger(Constants.DEBUG, "In 'GetUrl' the URL is " + url);
            try
            {
                Stream data = client.OpenRead(url);
                StreamReader reader = new StreamReader(data);
                string body = reader.ReadToEnd();
                data.Close();
                reader.Close();

                return body;
            }
            catch (WebException e) { Logger(Constants.INFO, e.ToString() + " exception caught"); return null; }
        }

        public async static System.Threading.Tasks.Task<string> GetSecret(string secretName)
        {

            String region = "us-east-1";
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        
            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;

            GetSecretValueResponse response = null;

            try
            {
                response = await client.GetSecretValueAsync(request);
            }
            catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
            {
                Logger(Constants.DEBUG,"The requested secret was not found:" + secretName);
            }
            catch (InvalidRequestException e)
            {
                Logger(Constants.DEBUG,"The request was invalid due to: " + e.Message);
            }
            catch (InvalidParameterException e)
            {
                Logger(Constants.DEBUG,"Request had invalid params:" + e.Message);
            }
            catch (InternalServiceErrorException e)
            {
                Logger(Constants.DEBUG, "An error occurred on the server side: " + e.Message);
            }
            catch (DecryptionFailureException e)
            {
                Logger(Constants.DEBUG,"Secrets Manager can't decrypt the protected secret text using the provided KMS key." + e.Message);
            }
            dynamic secretJson = JObject.Parse(response.SecretString);
            
             return secretJson.Secret; }
        
    }

    public static class Constants
    {
        public const int SEVERE = 3;
        public const int DEBUG = 2;
        public const int INFO = 1;
    }

   

}

