// https://github.com/alexa/alexa-smarthome/tree/master/sample_messages/Authorization


namespace AlexSmarthomeLambdaFunction
{
    class Json
    {
       public static string endpointModel = @"{'payload':{'endpoints':[]}}";
        public static string endpointIdModel = @"{
                'endpointId': 'demo_id',
                     'manufacturerName': 'Your company',
                     'friendlyName': 'Bedroom Outlet',
                    'description': 'Smart Device Switch',
                      'displayCategories': ['SWITCH'],
                    'capabilities':
                   [
                    {
                        'type': 'AlexaInterface',
                        'interface': 'Alexa',
                        'version': '3'
                    },
                    {
                        'interface': 'Alexa.PowerController',
                        'version': '3',
                        'type': 'AlexaInterface',
                        'properties': {
                            'supported': [{
                                'name': 'powerState'
                            }],
                            'retrievable': false
                        }
                    }
                ]}";

        public static string EventDiscoveryheader = @"{
                                            'event': {
                                                        'header': {
                                                                    'namespace': 'Alexa.Discovery',
                                                                    'name': 'Discover.Response',
                                                                    'payloadVersion': '3',
                                                                    'messageId': '5f8a426e-01e4-4cc9-8b79-65f8bd0fd8a4'
                                                                  },
                                                         'payload': {}
                                                     }
                                        }";


        public static string TurnOnResponse = @"{'event': { 'header':
                                                    {'namespace':'Alexa','name':'Response','payloadVersion':'3',
                                                     'messageId': '5f8a426e-01e4-4cc9-8b79-65f8bd0fd8a4',
                                                     'correlationToken': 'dFM'
                                                    },
                                                 
                                                 'endpoint': 
                                                    {
                                                      'scope': {
                                                                    'type': 'BearerToken',
                                                                    'token': 'access-token-from-Amazon'
                                                               },
                                                      'endpointId': 'endpoint-001'
                                                     },
                                                 'payload': { }
                                                }
                                      }";


        public static string acceptGrant = @"{'event':{'header':{'namespace':'Alexa.Authorization','name':'AcceptGrant.Response','payloadVersion':'3','messageId':' '},'payload':{}}}";


        public static string errorResponse = @"{

    'event': {
        'header': {
                       'namespace': 'Alexa.Authorization',
                       'name': 'ErrorResponse',
                       'payloadVersion': '3',
                       'messageId': '5f8a426e-01e4-4cc9-8b79-65f8bd0fd8a4',
                       'correlationToken': 'dFMb0z+PgpgdDmluhJ1LddFvSqZ/jCc8ptlAKulUj90jSqg=='
                  },

        'payload': {
                       'type': 'ACCEPT_GRANT_FAILED',
                       'message': 'Failed to handle the AcceptGrant directive because request to Login with Amazon failed'      
                   }
        }}";



    }
}


 