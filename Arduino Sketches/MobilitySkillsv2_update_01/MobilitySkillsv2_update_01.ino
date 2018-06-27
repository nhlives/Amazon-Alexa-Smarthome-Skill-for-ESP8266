#include <ArduinoJson.h>
#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <ESP8266WiFiMulti.h>
#include <ESP8266HTTPClient.h>

#define NODEBUG  // comment out or change to NODEBUG this line for production/release DEBUG for verbose logging
//===================================================================================== Change History ===================================================================================================
//  6/6/2018 Initial Version
//====================================================================================== To Do ===========================================================================================================     
const String CLIENTGUID = " "; 
const String SECRET = " ";
String HOST = "host url";
const String fallbackHOST = "backup host url";
String fingerprint = "05 06 5d 55 88 2d 51 85 cb 51 a4 bf bd f9 9a 00 fa cc 63 7e"; 
const char* ssid = "ssid";
const char* password = "password";
const String sketchVersion = "1"; //this gets sent to the web service.  Nothing done with it yet.
int delayperiod = 10000;  // default delay between queries to web service
//=========================================================================================================================================================================================================
//===================================================================================  Add global variables or arrays here ================================================================================

//=========================================================================================================================================================================================================
ESP8266WiFiMulti WiFiMulti;
HTTPClient http;
String directive = "";
String endpointId = "";
String MessageId = "";
String corelationToken = "";
String secret = "";
String newHOST = "";
String newFingerprint = "";
int newDelayperiod= 0;
String Result = "";
//===============================================================================================================================
void setup()
{
  Serial.begin(115200);
  
  WiFiMulti.addAP( ssid,  password );   // You can add multiple access points.  The best one will be selected
  // wait for WiFi connection
  SetupWIFI();

 //========================================== Get Global Changes ================================================================
  String payload =  GetUrl( fallbackHOST + "/api/client/global?key=" + CLIENTGUID + "&version=" + sketchVersion, false ); 
  if(payload != "")
  {
    if(!MakeJsonObject(payload))
    {
      if( newHOST != "" ) { HOST = newHOST; }
      if( newFingerprint != "" ) { fingerprint = newFingerprint; }
      if( newDelayperiod != 0 ) { delayperiod = newDelayperiod; }
    }
  }
 //==============================================================================================================================
//setup authorization
  const char *c_SECRET = SECRET.c_str();
  http.setAuthorization("bearer",c_SECRET);
  http.setReuse(true);  //reuse connection if supported by remote server
//===============================================================================================================================
//  Clear any residual directives from queue
    GetUrl( HOST + "/api/client/QueueRequestStatus?key=" + CLIENTGUID, true );
}

void loop() 
{
  String payload =  GetUrl( HOST + "/api/client/request?key=" + CLIENTGUID, true); 
  if(payload != "")
  {
   bool jsonError =  MakeJsonObject( payload );
   if (!jsonError)
   {
    directive.toLowerCase();
    if (directive == "turnon")
    {   
      endpointId.toLowerCase();
      //
//========================process endpointId response here.  Add each endpointId in lowercase==============
       if (endpointId == "foot") { DoFoot(); }
       if (endpointId == "head") { DoHead(); }
       if (endpointId == "custom") { DoCustom(); }    
//=========================================================================================================         
    }  
    // Reply to server.  This does not change the Alexa response but instead clears the RESPONSEQ on the server
    GetUrl( HOST + "/api/client/QueueRequestStatus?key=" + CLIENTGUID, true );     
   }  // json error
  } // payload != ""
    
   delay(delayperiod);
}
//=============================== To Do ===================================================================
//  Add functions to handle endpoinId directives
//=========================================================================================================

void DoFoot()
{Serial.println("Do stuff for 'foot'");}

void DoHead()
{Serial.println("Do stuff for 'head'");}

void DoCustom()
{Serial.println("Do stuff for 'custom'");}

//=========================================================================================================
//=================================== Don't change below =================================================    
bool MakeJsonObject( String input )
{
  char  json[input.length()+1];
  strcpy (json, input.c_str());
  StaticJsonBuffer<2000>jsonBuffer;
  JsonObject& root = jsonBuffer.parseObject(json);
  if (!root.success()) 
    {  
      #ifdef DEBUG               
      Serial.println("json parse failed");      
      #endif
      return true;
    }
    else // json OK
    {
     // print the json
     #ifdef DEBUG
     root.prettyPrintTo(Serial);  
     Serial.println("");
     #endif
     // The JSON library for Arduino will return a "" or 0  if the variable doesn't exist in the JSON object.  We are safe then to request a value for any JSON string.
     // We take advantage of that here by ignoring what the JSON object looks like and rely on the result to be "" or 0 for type integer if the JSON doesn't contain the variable
     directive = root["directive"].as<String>();
     endpointId = root["endpointId"].as<String>();
     MessageId = root["MessageId"].as<String>();
     corelationToken = root["corelationToken"].as<String>();
     secret = root["secret"].as<String>();
     newHOST = root["host"].as<String>();
     newFingerprint = root["host"].as<String>();
     newDelayperiod = root["delayperid"].as<int>();
     Result = root["Result"].as<String>();

      return false;
    
    }
}

String GetUrl( String Url, bool SSL )
{ 
    String payload = "";
    if(SSL)
    { http.begin( "https://" + Url, fingerprint );}
    else
    { http.begin( "http://" + Url ); }
   
    // start connection and send HTTP header
    int httpCode = http.GET();

    // httpCode will be negative on error
    if (httpCode > 0) 
    {
      // file found at server
      if (httpCode == HTTP_CODE_OK) 
      {
       payload = http.getString();   
      } // HTTP_CODE_OK
    } // no error
    else // got error
    {
      #ifdef DEBUG
      Serial.printf("[HTTP] GET... failed, error: %s\n", http.errorToString(httpCode).c_str());
      #endif
    }
    http.end();
    return payload;
}

void SetupWIFI()
{
   #ifdef DEBUG
   Serial.print("Connecting ...");
   #endif
   while ( WiFiMulti.run() != WL_CONNECTED)
   {
      delay(100);
      #ifdef DEBUG
      Serial.print(".");
      #endif
   }
 
   Serial.println("");
   Serial.print("Connected to ");
   Serial.println(ssid);
   Serial.print("IP address: ");
   Serial.println(WiFi.localIP());
}


