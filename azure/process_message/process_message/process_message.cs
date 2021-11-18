using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slack;

namespace process_message
{
    public static class process_message
    {
        [FunctionName("process_message")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run([QueueTrigger("octopusdeploy", Connection = "")]string myQueueItem, ILogger log)
        {
            // Deserialize message into JSON object
            dynamic subscriptionEvent = JsonConvert.DeserializeObject(myQueueItem);
            string slackUrl = Environment.GetEnvironmentVariable("SlackUrl");
            string slackChannel = Environment.GetEnvironmentVariable("SlackChannel");

            // Create slack objects
            Slack.Webhooks.SlackClient slackClient = new Slack.Webhooks.SlackClient(slackUrl);
            Slack.Webhooks.SlackMessage slackMessage = new Slack.Webhooks.SlackMessage();

            // Get Octopus Event information
            string librarySetName = subscriptionEvent.Payload.Event.ChangeDetails.DocumentContext.Name;
            System.Text.StringBuilder messageText = new System.Text.StringBuilder();

            // Get message from event
            messageText.AppendLine(subscriptionEvent.Payload.Event.Message.ToString());

            if (subscriptionEvent.Payload.Event.ChangeDetails.Differences.Count > 0)
            {
                // Loop through variables collection
                foreach (var difference in subscriptionEvent.Payload.Event.ChangeDetails.Differences)
                {
                    // Split the path
                    string[] parsedDifference = difference.path.ToString().Split("/", StringSplitOptions.RemoveEmptyEntries);

                    if (parsedDifference[0] == "Variables")
                    {
                        // Get the variable index value
                        int variableIndex = int.Parse(parsedDifference[1]);

                        // Add to message
                        messageText.AppendLine(string.Format("Variable: {0}", subscriptionEvent.Payload.Event.ChangeDetails.DocumentContext.Variables[variableIndex].Name));
                        messageText.AppendLine(string.Format("Old value: {0} \r\n New value: {1}", subscriptionEvent.Payload.Event.ChangeDetails.DocumentContext.Variables[variableIndex].Value, difference.value));
                    }
                }
            }

            log.LogInformation($"Posting to Slack: {messageText.ToString()}");

            // Post message to channel
            slackMessage.Channel = slackChannel;
            slackMessage.Text = messageText.ToString();
            slackClient.Post(slackMessage);
        }
    }
}
