using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using Octopus.Client;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace process_message
{
    public class Function
    {
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {

        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            foreach(var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            // Log
            LambdaLogger.Log("Begin message processing...");
            
            // Get environment variables
            string octopusServerUrl = Environment.GetEnvironmentVariable("OCTOPUS_SERVER_URL");
            string octopusApiKey = Environment.GetEnvironmentVariable("OCTOPUS_API_KEY");
            
            // Log
            LambdaLogger.Log(string.Format("Retrieved environment variables, Octopus Server Url: {0}...", octopusServerUrl));

            // Deserialize message JSON
            LambdaLogger.Log(string.Format("Parsing message..."));
            //dynamic subscriptionEvent = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(message.Body, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
            dynamic subscriptionEvent = JsonConvert.DeserializeObject(message.Body);
            LambdaLogger.Log("Successfully parsed message JSON...");

            // Create Octopus client object
            LambdaLogger.Log("Creating server endpoint object ...");
            var endpoint = new OctopusServerEndpoint(octopusServerUrl, octopusApiKey);
            LambdaLogger.Log("Creating repository object...");
            var repository = new OctopusRepository(endpoint);
            LambdaLogger.Log("Creating client object ...");
            var client = new OctopusClient(endpoint);

            // Create repository for space
            string spaceId = subscriptionEvent.Payload.Event.SpaceId;
            LambdaLogger.Log(string.Format("Creating repository object for space: {0}", spaceId));
            var space = repository.Spaces.Get(spaceId);
            Octopus.Client.IOctopusSpaceRepository repositoryForSpace = client.ForSpace(space);

            // Retrieve interruption; first related document is the DeploymentId
            string documentId = subscriptionEvent.Payload.Event.RelatedDocumentIds[0];

            LambdaLogger.Log(string.Format("Processing event for document: {0}...", documentId));
            var interruptionCollection = repositoryForSpace.Interruptions.List(regardingDocumentId: documentId).Items;

            if (interruptionCollection.Count > 0)
            {
                foreach (var interruption in interruptionCollection)
                {
                    // Take responsibility
                    LambdaLogger.Log(string.Format("Taking responsibility for interruption: {0}...", interruption.Id));
                    repositoryForSpace.Interruptions.TakeResponsibility(interruption);

                    // Set the result
                    interruption.Form.Values[message.Attributes["Type"]] = message.Attributes["Action"];

                    // Update Octopus
                    LambdaLogger.Log(string.Format("Submitting guidance for: {0}...", interruption.Id));
                    repositoryForSpace.Interruptions.Submit(interruption);
                }
            }


            await Task.CompletedTask;
        }
    }
}
