
#define NOLOCALDB
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon;
using Newtonsoft.Json.Linq;

//using System.Threading;


namespace SmarthomeSkillsWebAPI.Models
{


    public class Globals
    {
        public static Dictionary<string, Monitor> monitoring = new Dictionary<string, Monitor>();
        public static string appSecret;
        public static ILogger logger = null;
        public static string AccessKeyID;
        public static string Secret;

    }
    public class ClientGuidRequest
    {
        public string ClientGuid { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public string secret { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public bool ActiveStatus { get; set; }
    }

    public class Client
    {
        public string ClientID { get; set; }
        public string Amazn_link_time { get; set; }
        public string ClientGuid { get; set; }
        public List<string> Appliance { get; set; }
        public bool ActiveStatus { get; set; }
    }

    public class AmaznRequest
    {
        public string Amazn_email { get; set; }
        public string Amazn_link_time { get; set; }
    }
    public class Monitor
    {
        public string CLIENTGuid { get; set; }
        public string lastTimeForRequest { get; set; }
        public string lastTimeForPoll { get; set; }
        public long numberOfRequests { get; set; }
    }

    public class RequestQ
    {
        public string ClientGuid { get; set; }
        public string MessageId { get; set; }
        public string corelationToken { get; set; }
        public string directive { get; set; }
        public string endpointId { get; set; }
        public string time { get; set; }
        public string secret { get; set; }

    }


    public static class AWSDynamoDB
    {
        public static string clientTableName = "Client";
        public static string requestQTableName = "RequestQ";
        // constructor initalizations
        public static AmazonDynamoDBClient dbClient = BuildClient();

        public static List<Monitor> GetMonitor()
        {

            List<Monitor> list = Globals.monitoring.Values.ToList();
            return list;

        }
        public static void PutMonitor(string CLIENTGuid, bool isPoll)
        {
            DateTime localTime = DateTime.Now;
            string guid = CLIENTGuid;

            Monitor tryMonitor = new Monitor();

            if (!Globals.monitoring.TryGetValue(guid, out tryMonitor))
            {
                Monitor monitor = new Monitor();
                {
                    monitor.CLIENTGuid = CLIENTGuid;
                    monitor.numberOfRequests = 1;
                    monitor.lastTimeForRequest = localTime.ToString();
                    monitor.lastTimeForPoll = localTime.ToString();
                }
                Globals.monitoring.Add(guid, monitor);
            }
            else
            {
                Globals.monitoring[guid].lastTimeForPoll = localTime.ToString();
                if (!isPoll)
                {
                    Globals.monitoring[guid].lastTimeForRequest = localTime.ToString();
                    Globals.monitoring[guid].numberOfRequests++;
                }
            }

        }
        public async static Task<bool> DeleteQbyClientGuid(string KeyItem)
        {

            //  The requestQ for the ESP8266 may have more than one unhandled request.  When we get a requestQ object to present to the 
            //  8266 we only retrieve the latest.  This procedure is called when the 8266 responds after a requestQ item is processed successfully.
            //  We then delete any unhandled request in the queue.

            string tableName = requestQTableName;

            try
            {

                Table QTable = Table.LoadTable(dbClient, tableName);
                //get the RequestQ items for the ClientGuid
                ScanFilter scanFilter = new ScanFilter();
                scanFilter.AddCondition("ClientGuid", ScanOperator.Equal, KeyItem);
                ScanOperationConfig config = new ScanOperationConfig()
                {
                    Filter = scanFilter,
                    IndexName = "ClientGuid-index"
                };

                Search search = QTable.Scan(scanFilter);
                List<Document> allItems = await search.GetRemainingAsync();

                foreach (Document doc in allItems)
                {
                    string KeytoDelete = "";
                    string TimetoDelete = "";
                    foreach (string itemkey in doc.Keys)
                    {

                        DynamoDBEntry dbEntry = doc[itemkey];
                        string val = dbEntry.ToString();
                        if (itemkey == "ClientGuid") { KeytoDelete = val; }
                        if (itemkey == "time") { TimetoDelete = val; }
                    } // end foreach key   

                    Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
                      {
                            { "ClientGuid", new AttributeValue { S = KeytoDelete } },
                            { "time", new AttributeValue { S = TimetoDelete } }
                      };

                    // Create DeleteItem request
                    DeleteItemRequest request = new DeleteItemRequest
                    {
                        TableName = tableName,
                        Key = key
                    };

                    // Issue request
                    var response = await dbClient.DeleteItemAsync(request);
                } //end foreach document

                return true;
            }

            catch { return false; }

        }

        public async static Task<string> QueryByClientGuid(string key)
        {
            // Function  returns a client "secret" using the ClientGuid as a search
            Table QTable = Table.LoadTable(dbClient, clientTableName);
            ScanFilter scanFilter = new ScanFilter();
            scanFilter.AddCondition("ClientGuid", ScanOperator.Equal, key);
            ScanOperationConfig config = new ScanOperationConfig()
            {
                Filter = scanFilter,
                IndexName = "ClientGuid-index"
            };

            Search search = QTable.Scan(scanFilter);

            List<Document> allItems = await search.GetRemainingAsync();
            string secret = null;
            foreach (Document doc in allItems)
            {
                foreach (string itemkey in doc.Keys)
                {
                    DynamoDBEntry dbEntry = doc[itemkey];
                    string val = dbEntry.ToString();
                    if (itemkey == "secret") { secret = val; }
                }
            }
            return secret;
        }
        public async static Task<List<RequestQ>> FetchQItem(string Key)
        {
            // Functions returns all requestQ items
            string tableName = requestQTableName;
            Table ThreadTable = Table.LoadTable(dbClient, tableName);

            List<RequestQ> requests = new List<RequestQ>();

            //get the RequestQ items for the ClientGuid
            ScanFilter scanFilter = new ScanFilter();
            scanFilter.AddCondition("ClientGuid", ScanOperator.Equal, Key);
            Search search = ThreadTable.Scan(scanFilter);
            try
            {
                //get the request. MobilitySkillsRequestQ has a sort key so we may get more than one
                List<Document> allItems = await search.GetRemainingAsync();
                Document doc = allItems.Last();
                //create a RequestQ object for this request
                RequestQ request = new RequestQ();
                foreach (string key in doc.Keys)
                {
                    DynamoDBEntry dbEntry = doc[key];
                    string val = dbEntry.ToString();
                    if (key == "ClientGuid") { request.ClientGuid = val; }
                    if (key == "MessageId") { request.MessageId = val; }
                    if (key == "corelationToken") { request.corelationToken = val; }
                    if (key == "directive") { request.directive = val; }
                    if (key == "endpointId") { request.endpointId = val; }
                    if (key == "time") { request.time = val; }
                    if (key == "secret") { request.secret = val; }

                } //end for loop
                requests.Add(request);

                return requests;
            } //end try
            catch
            {
                Globals.logger.LogCritical("Exception reading: " + requestQTableName);
                return requests;
            } //end catch

        }

        public static AmazonDynamoDBClient BuildClient()
        {
            AmazonDynamoDBClient dbClient;
#if LOCALDB
              var credentials = new BasicAWSCredentials(
              accessKey: "LOCALDB",
              secretKey: "LOCALDB");
              var config = new AmazonDynamoDBConfig
              {
            
                  // for testing only
                  ServiceURL = "http://localhost:8000"

              };
             dbClient = new AmazonDynamoDBClient(credentials, config);
#else
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1
            };
            // get the development credentials if available
            Globals.logger.LogInformation("AccessKeyID: " + Globals.AccessKeyID + " Secret: " + Globals.Secret);
            if (Globals.AccessKeyID == null)
            {
                dbClient = new AmazonDynamoDBClient(config);
            }
            else
            {
                var credentials = new BasicAWSCredentials(
                                    accessKey: Globals.AccessKeyID,
                                    secretKey: Globals.Secret);

                dbClient = new AmazonDynamoDBClient(credentials, config);
            }
#endif

            return dbClient;

        }

        public async static Task<bool> ItemExists(string key)
        {

            var response = await dbClient.GetItemAsync(
               tableName: clientTableName,
               key: new Dictionary<string, AttributeValue>
               {
                    {"ClientID", new AttributeValue {S = key }}
               });

            Client client = new Client();
            try
            {
                client.ClientID = response.Item["ClientID"].S.ToString();
            }
            catch { return false; }

            return true;
        }

        public async static Task<bool> AddtoRequestQ(string directive, string MessageId, string corelationToken, string ClientGuid, string endpointId, string secret)
        {
            DateTime localTime = DateTime.Now;

            try
            {
                Table clientDocument = Table.LoadTable(dbClient, requestQTableName);

                var queueDoc = new Document();
                {
                    queueDoc["ClientGuid"] = ClientGuid;
                    queueDoc["directive"] = directive;
                    queueDoc["MessageId"] = MessageId;
                    queueDoc["corelationToken"] = corelationToken;
                    queueDoc["endpointId"] = endpointId;
                    queueDoc["time"] = localTime.ToFileTime().ToString();
                    queueDoc["secret"] = secret;
                }
                await clientDocument.UpdateItemAsync(queueDoc);

            }
            catch
            {
                Globals.logger.LogError("Exception in function 'AddtoRequestQ");
                return false;
            }


            return true;
        }

        public async static Task<bool> AmaznLink(AmaznRequest client)
        {
            DateTime localTime = DateTime.Now;
            try
            {
                Table clientDocument = Table.LoadTable(dbClient, clientTableName);

                var clientDoc = new Document();

                clientDoc["ClientID"] = client.Amazn_email;
                clientDoc["Amazn_link_time"] = localTime.ToString();

                await clientDocument.UpdateItemAsync(clientDoc);
            }
            catch
            {
                Globals.logger.LogError("Exception reading client table in function 'AmaznLink'");
                return false;
            }


            return true;
        }

        public async static Task<ClientGuidRequest> GetClientGuid(string Key)
        {

            try
            {
                ClientGuidRequest guid = new ClientGuidRequest();


                var response = await dbClient.GetItemAsync(
                   tableName: clientTableName,
                   key: new Dictionary<string, AttributeValue>
                   {
                    {"ClientID", new AttributeValue {S = Key }}
                   });

                guid.ClientGuid = response.Item["ClientGuid"].S.ToString();
                guid.secret = response.Item["secret"].S.ToString();
                guid.ActiveStatus = response.Item["ActiveStatus"].BOOL;
                return guid;
            }
            catch { return null; }


        }

        public static async Task<Client> FetchItem(string Key)
        {
            Client client = new Client();
            try
            {
                var response = await dbClient.GetItemAsync(
               tableName: clientTableName,
               key: new Dictionary<string, AttributeValue>
               {
                    {"ClientID", new AttributeValue {S = Key}}
               }
               );
                client.ClientID = response.Item["ClientID"].S.ToString();
                client.Appliance = response.Item["Appliance"].SS;
                client.ClientGuid = response.Item["ClientGuid"].S.ToString();
                client.Amazn_link_time = response.Item["Amazn_link_time"].S.ToString();
                client.ActiveStatus = response.Item["ActiveStatus"].BOOL;

            }
            catch { return client; }

            return client;
        }

        public async static Task<string> GetSecret(string secretName)
        {

            String region = "us-east-1";
         
            IAmazonSecretsManager client;
            if (Globals.AccessKeyID != null)
            {
                client = new AmazonSecretsManagerClient(Globals.AccessKeyID, Globals.Secret, RegionEndpoint.GetBySystemName(region));
            }
            else
            { 
                client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            }

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;

            GetSecretValueResponse response = null;

            try
            {
                response = await client.GetSecretValueAsync(request);
            }
            catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
            {
                Globals.logger.LogCritical("The requested secret {0} was not found", secretName);
            }
            catch (InvalidRequestException e)
            {
                Globals.logger.LogCritical("The request was invalid due to: {0}", e.Message);
            }
            catch (InvalidParameterException e)
            {
                Globals.logger.LogCritical("Request had invalid params: {0}", e.Message);
            }
            catch (InternalServiceErrorException e)
            {
                Globals.logger.LogCritical("An error occurred on the server side.", e.Message);
            }
            catch (DecryptionFailureException e)
            {
                Globals.logger.LogCritical("Secrets Manager can't decrypt the protected secret text using the provided KMS key.", e.Message);
            }
            dynamic secretJson  = JObject.Parse(response.SecretString);
        
            return secretJson.Secret; 
           
        }
    }
}


        


        