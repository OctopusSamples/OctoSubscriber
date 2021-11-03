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
            dynamic subscriptionEvent = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(message.Body, new Newtonsoft.Json.Converters.ExpandoObjectConverter());

            // Create Octopus client object
            var endpoint = new OctopusServerEndpoint(octopusServerUrl, octopusApiKey);
            var repository = new OctopusRepository(endpoint);
            var client = new OctopusClient(endpoint);

            // Create repository for space
            LambdaLogger.Log(string.Format("Creating repository object for space: {0}", subscriptionEvent.SpaceId));
            var space = repository.Spaces.Get(subscriptionEvent.SpaceId);
            Octopus.Client.IOctopusSpaceRepository repositoryForSpace = client.ForSpace(space);

            // Retrieve interruption; first related document is the DeploymentId
            string deploymentId = subscriptionEvent.Event.RelatedDocumentIds[0];

            LambdaLogger.Log(string.Format("Processing event for deployment: {0}...", deploymentId));
            var guidedFailureInterruptionCollection = repositoryForSpace.Interruptions.List(regardingDocumentId: deploymentId).Items;

            if (guidedFailureInterruptionCollection.Count > 0)
            {
                foreach (var guidedFailureInterruption in guidedFailureInterruptionCollection)
                {
                    // Take responsibility
                    LambdaLogger.Log(string.Format("Taking responsibility for interruption: {0}...", guidedFailureInterruption.Id));
                    repositoryForSpace.Interruptions.TakeResponsibility(guidedFailureInterruption);

                    // Set the result
                    guidedFailureInterruption.Form.Values["Result"] = "Fail";

                    // Update Octopus
                    LambdaLogger.Log(string.Format("Submitting guidance for: {0}...", guidedFailureInterruption.Id));
                    repositoryForSpace.Interruptions.Submit(guidedFailureInterruption);
                }
            }


            await Task.CompletedTask;
        }
    }
}
