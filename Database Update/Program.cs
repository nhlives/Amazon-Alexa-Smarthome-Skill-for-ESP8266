using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Newtonsoft.Json;

namespace MobilitySkillsClient
{
    

    public class Program
    {

        private class Client
        {
            public string ClientID { get; set; }
            public string Amazn_link_time { get; set; }
            public string ClientGuid { get; set; }
#pragma warning disable IDE1006 // Naming Styles
            public string secret { get; set; }
#pragma warning restore IDE1006 // Naming Styles
            public List<string> Appliance { get; set; }
#pragma warning disable IDE1006 // Naming Styles
            public bool ActiveStatus { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        }

        private readonly AmazonDynamoDBClient client;
        private static string email;
        private static string AccessKeyID;
        private static string SecretAccessKey;
        private static bool error = false;
        private static string inputJson = null;
        private static bool clientStatus = true;
        private static dynamic input = null;
        private static List<string> appliance = new List<string>();
        private const string ClientTableName = "MobilitySkillsClientV2";
        private static string option = null;

        public Program()
        {
            client = BuildClient();
        }

        public static void Main(string[] args)
        {

            /* Input JSON
              { 
	              'email': 'dummay@gmail.com',
	              'ActiveStatus': true,
                  'Appliance': [ 'foot', 'custom', 'attitide' ],
                  'AcessKeyID': " ",
	              'SecretAccessKey': " "
              }
}
            */

            Console.WriteLine("");
            Console.WriteLine("");

            if (args.Length > 0)
            {
                option = args[0].Replace("/", "");
                option = option.Replace("-", "");
                option = option.ToLower();
            }
            else
            {
                Console.WriteLine("No option.  Use /add to add a client or /update tp update a client");
            }
            try
            {
                inputJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\client.txt");
            }
            catch (IOException err) { error = true; Console.WriteLine("IO error:  " + err.Message); }

            if (error) { return; }

            try
            {
                input = JsonConvert.DeserializeObject<dynamic>(inputJson);
            }
            catch (JsonReaderException err) { error = true; Console.WriteLine("Invalid json: " + err.Message); }

            if (error) { return; }

            // email
            email = input.email;
            if (email == null) { error = true; Console.WriteLine("email null or invalid"); }
            if (error) { return; }
            //Status
            string temp_clientStatus = input.ActiveStatus;
            if(temp_clientStatus==null){ clientStatus = true; }
            else { clientStatus = input.ActiveStatus; }
            // Appliances
            foreach (string item in input.Appliance)
            { appliance.Add(item); }
            //Access Key ID
            AccessKeyID = input.AccessKeyID;
            if (AccessKeyID == null) { error = true; Console.WriteLine("Acess Key ID null or invalid"); }
            if (error) { return; }
            // Secret Access Key
            SecretAccessKey = input.SecretAccessKey;
            if (SecretAccessKey == null) { error = true; Console.WriteLine("Secret Access Key null or invalid"); }
            if (error) { return; }

            var program = new Program();
            if (option == "add") { program.Add().Wait(); }
            else if (option == "update") { program.Update().Wait(); }
            else if (option == "list") { program.List().Wait(); }
            else
            {
                Console.WriteLine("Use /add to add a client or /update to update a client or /list to list all clients");
            }

        }

        public async Task List()
        {
            Console.WriteLine("Entering List");
            var description = await DescribeTable();
            Console.WriteLine($"Table name: {description.TableName}" + "  " + $"Table status: {description.TableStatus}");

            Table QTable = Table.LoadTable(client, ClientTableName);
            //get the RequestQ items for the ClientGuid
            ScanFilter scanFilter = new ScanFilter();
            scanFilter.AddCondition("ClientID", ScanOperator.GreaterThan, " ");
            ScanOperationConfig config = new ScanOperationConfig()
            {
                Filter = scanFilter,
            };

            Search search = QTable.Scan(scanFilter);
            List<Document> allItems = await search.GetRemainingAsync();
            Console.WriteLine("================================================================================");
            foreach (Document doc in allItems)
            {
                foreach (string itemkey in doc.Keys)
                {
                    DynamoDBEntry dbEntry = doc[itemkey];
                    
                    if (itemkey == "ClientID")        { string val = dbEntry.AsString(); Console.WriteLine( "ClientID:         " + val); }
                    if (itemkey == "ClientGuid")      { string val = dbEntry.AsString(); Console.WriteLine( "ClientGuid:       " + val); }
                    if (itemkey == "ActiveStatus")    { bool   val = dbEntry.AsBoolean();Console.WriteLine( "ActiveStatus:     " + val); }
                    if (itemkey == "secret")          { string val = dbEntry.AsString(); Console.WriteLine( "secret:           " + val); }
                    if (itemkey == "amazn_link_time") { string val = dbEntry.AsString(); Console.WriteLine( "amazn_link_time:  " + val); }
                    if (itemkey == "Appliance")       { int i = 0; foreach (string device in dbEntry.AsListOfString()) { Console.WriteLine("Appliance(" + i +"):     " + device); i++; } }
                    
                }// end foreach key   

                Console.WriteLine("================================================================================");
            }
                return;
        }

        
        public async Task Add()
        {
            Console.WriteLine("Entering Add");
            var description = await DescribeTable();
            Console.WriteLine($"Table name: {description.TableName}"+ "  " + $"Table status: {description.TableStatus}");

            var loadedItem = await FetchItem();
            if (loadedItem.Count == 0)
            {
                await SaveNewItem();
                loadedItem = await FetchItem();
                Console.WriteLine("================================================================================");
                Console.WriteLine($"ClientID:          {loadedItem["ClientID"].S}");
                Console.WriteLine($"ActiveStatus:      {loadedItem["ActiveStatus"].BOOL}");
                Console.WriteLine($"ClientGuid:        {loadedItem["ClientGuid"].S}");
                Console.WriteLine($"secret:            {loadedItem["secret"].S}");
                Console.WriteLine($"Amazn_link_time:   {loadedItem["Amazn_link_time"].S}");
                int i = 0;
                foreach (string item in loadedItem["Appliance"].SS)
                {
                    Console.WriteLine($"Appliance(" + i + "):      " + item);
                    i++;
                }
                    Console.WriteLine("================================================================================");
            }
            else
            {
                Console.WriteLine("Error: Client email already exists");
                return;
            }
        }

        public async Task Update()
        {
            Console.WriteLine("Entering Update");
            var description = await DescribeTable();
            Console.WriteLine($"Table name: {description.TableName}" + "  " + $"Table status: {description.TableStatus}");

            var loadedItem = await FetchItem();
            if (loadedItem.Count != 0)
            {
#pragma warning disable IDE0017 // Simplify object initialization
                Client client = new Client();
#pragma warning restore IDE0017 // Simplify object initialization
                client.ClientID = loadedItem["ClientID"].S;
                client.ActiveStatus = loadedItem["ActiveStatus"].BOOL;
                client.ClientGuid = loadedItem["ClientGuid"].S;
                client.secret = loadedItem["secret"].S;
                client.Appliance = loadedItem["Appliance"].SS;
                client.Amazn_link_time = loadedItem["Amazn_link_time"].S;

                await SaveUpdateItem(client);
            }
            else
            {
                Console.WriteLine("Error: Client email does not exist");
                return;
            }
            loadedItem = await FetchItem();
            Console.WriteLine("================================================================================");
            Console.WriteLine($"ClientID:          {loadedItem["ClientID"].S}");
            Console.WriteLine($"ActiveStatus:      {loadedItem["ActiveStatus"].BOOL}");
            Console.WriteLine($"ClientGuid:        {loadedItem["ClientGuid"].S}");
            Console.WriteLine($"secret:            {loadedItem["secret"].S}");
            Console.WriteLine($"Amazn_link_time:   {loadedItem["Amazn_link_time"].S}");
            int i = 0;
            foreach (string item in loadedItem["Appliance"].SS)
            {
                Console.WriteLine($"Appliance(" + i + "):      " + item);
                i++;
            }
            Console.WriteLine("================================================================================");
        }
        private AmazonDynamoDBClient BuildClient()
        {
#if LOCALDB
              var credentials = new BasicAWSCredentials(
              accessKey: "LocalDB",
              secretKey: "LocalDB");
              var config = new AmazonDynamoDBConfig
              {
                // for testing only
                ServiceURL = "http://localhost:8000"

              };
             return new AmazonDynamoDBClient(credentials, config);
#else
            var credentials = new BasicAWSCredentials(
              accessKey: AccessKeyID,
              secretKey: SecretAccessKey);
            var config = new AmazonDynamoDBConfig
             {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1              
             };
             return new AmazonDynamoDBClient(credentials, config);
#endif
          
        }

        private async Task<Dictionary<string, AttributeValue>> FetchItem()
        {
            try
            {
                var response = await client.GetItemAsync(
                    tableName: ClientTableName,
                    key: new Dictionary<string, AttributeValue>
                    {
                    {"ClientID", new AttributeValue {S = email}}
                    }
                );
                return response.Item;
            }
            catch { return null; }
        }

        private async Task SaveNewItem()
        {
            Guid ClientGuidid = Guid.NewGuid();
            Guid SecretGuidid = Guid.NewGuid();

            await client.PutItemAsync(
                tableName: ClientTableName,
                item: new Dictionary<string, AttributeValue>
                {
                   {"ClientID", new AttributeValue {S = email}},
                   {"Appliance", new AttributeValue {SS = appliance}},
                   {"Amazn_link_time", new AttributeValue {S = " "}},
                   {"ActiveStatus", new AttributeValue {BOOL = clientStatus}},
                   {"ClientGuid", new AttributeValue {S = ClientGuidid.ToString()}},
                   {"secret", new AttributeValue {S = SecretGuidid.ToString()}}

                }

            );
        }

        private async Task SaveUpdateItem(Client UpdateClient)
        {
            await client.PutItemAsync(
                tableName: ClientTableName,
                item: new Dictionary<string, AttributeValue>
                {
                   {"ClientID", new AttributeValue {S = UpdateClient.ClientID}},
                   {"Appliance", new AttributeValue {SS = appliance}},
                   {"ActiveStatus", new AttributeValue {BOOL = clientStatus}},
                   {"Amazn_link_time", new AttributeValue {S = " "}},
                   {"ClientGuid", new AttributeValue {S = UpdateClient.ClientGuid}},
                   {"secret", new AttributeValue {S = UpdateClient.secret}}
               

                }

            );
        }

        private async Task<TableDescription> DescribeTable()
        {
          
                var description = await client.DescribeTableAsync(new DescribeTableRequest {TableName = ClientTableName });
                return description.Table;
            
        }
    }
}
