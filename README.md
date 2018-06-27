# Amazon Alexa Smarthome Skill End-to-End Solution

This project uses Alexa Smarthome to drive a ESP8266 Arduino variant.  The project is currently used as a production SmartHome Skill at [Limited Mobility Solutions](http://limitedmobility.solutions).  Limited Mobility Solutions provides voice activated solutions for individuals with limited mobilty and few options for controlling devices other than a caregiver.  These are custom solutions that can activate beds, for example, when the individual's situation makes it difficult or impossible to use manual bed controllers.  These solutions are provided at no cost.

It is [Bill's hope](mailto:info@limitedmobility.solutions) that bed manufacturers will begin to provide native voice activated soltuions for their products.

From a practical standpoint, the solution can be used to activate anything that a Arduino can control.  An Arduino code sample is included in project.

The SmartHome Skill uses just the *TurnOn* directive but supports account linking and discovery.  Authenication is done with LWA(Logon With Amazon).  Additional SmartHome directives should be easy to add.  Contributors are encouraged to suggest changes.

## Getting Started

This is a Microsoft Visual Studio Community 2017 Solution and the solution can be downloaded and opened as an existing project. The solutioin consists of three C# .net 2.0 Core programs:
* A client database update program
* The AWS Lambda program to handle the incoming Smarthome directives
* A web service that acts as the go-between for the Lambda program and the ESP8266

### Prerequisites

It will be necessart to add several NuGet packages.  For the client database update:

```csproj
  <ItemGroup>
    <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.3.10" />
    <PackageReference Include="Newtonsoft.Json" Version="11.0.2" />
  </ItemGroup>
```
For the AWS Lambda program:
```csproj
  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="1.0.0" />
    <PackageReference Include="Amazon.Lambda.Logging.AspNetCore" Version="2.0.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.Json" Version="1.2.0" />
    <PackageReference Include="AWSSDK.CloudWatch" Version="3.3.6.3" />
    <PackageReference Include="AWSSDK.SecretsManager" Version="3.3.0.11" />
    <PackageReference Include="Newtonsoft.Json" Version="11.0.2" />
  </ItemGroup>
```
For the web service:
```csproj
<ItemGroup>
    <PackageReference Include="AWS.Logger.AspNetCore" Version="1.2.7" />
    <PackageReference Include="AWS.Logger.Core" Version="1.1.8" />
    <PackageReference Include="AWSSDK.Core" Version="3.3.24.1" />
    <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.3.10.1" />
    <PackageReference Include="AWSSDK.SecretsManager" Version="3.3.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.All" Version="2.0.8" />
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="2.1.1" />
  </ItemGroup>
```
**Recommended:  AWS Toolkit for Visual Studio 2017**

### Installing

This solution uses several AWS services:
* Elastic Beanstalk
* Load Balancers
* DynamoDB
* CloudWatch
* IAM
* Secrets Manager
* Certificate Manager

In addition to an AWS account, a Amazon Developers Account for the Alexa Skill console.  Additionally, the *Logon With Amazon* service is used to provide OAuth 2.0 authenication.

#### Creating the databases
The solution requires two DynamoDB tables:
* Client database
  - Primary partition key ClientGuid (String)
  - Primary sort key time (String)
* Request queue
  - Primary partition key ClientID (String)
  - Global index: ClientGuid-index ClientGuid (String)
  
Once the tables are created using the AWS console for DynamoDB, the client database can be populated using the Client Update console program and the request queue for Alexa requests will be popultated automatically by the web service.

Build and publish the *Client Database Update* project. I published this project as *win-64*.  Copy the client.txt file to the published folder.  This is a json file with the information needed to run the update program including a AWS user with appropriate DynamoDB permissions.

For example:
```json
'email': 'dummy@gmail.com',
	'ActiveStatus': true,
        'Appliance': [ 'foot', 'custom', 'attitude' ],
        'AcessKeyID': "AWS IAM accesskey",
	      'SecretAccessKey': "AWS IAM secret"
```

The json file attributes:
* email:  This is ClientID in the table and is the primary key for the client table
* ActiveStatus: Should be *true*
* Appliance:  This is a list of Alexa utterances.  For example, "Alexa, turn on foot".  These are the devices that will be found during Alexa discovery.

From the Client Database Update published folder, run the EXE with options:
* /add to add the item in the json file
* /update to update an existing item
* /list to list all items

#### The Lambda program
Build and upload the Lambda program LimitedMobilitySolution using the Visual Studio AWS Toolkit or the AWS Console.  The program requires an IAM role with:
* SecretsManagerReadWrite
* CloudWatchLogsFullAccess
* AWSLambdaRole

The lambda function requires the Smarthome trigger and the necessary linking to the Smarthome skill.  See the approriate Amazon Developer documentation.

#### The Web Service
The web service runs as an AWS Elastic Beanstalk instance.  By default the solutions uses HTTPS and therefore requires a certificate.  The certificate is installed on an AWS Load Balancer which then forwards the incoming GET to port 80 on the Beanstalk instance. The web service requires an IAM role that, in addition to the default permissions, needs the following:
* SecretsManagerReadWrite
* CloudWatchFullAccess
* AmazonDynamoDBFullAccess 

## Contributing

Feel free to comment and propose changes.  The author is not a C# expert and the code is generally more C++-like than anything.

## Authors

* **John Hollcraft** - *Initial work* -

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Acknowledgments
* Inspiration[InternetShortcut]
URL=https://github.com/nhlives/Limited-Mobility-Solutions-Alexa-Skill/blob/master/README.md
* etc

