const { QueueClient, QueueServiceClient } = require("@azure/storage-queue");
const connectionString = process.env.AZURE_STORAGE_CONNECTION_STRING;
const queueName = process.env.QUEUE_NAME;
const queueServiceClient = QueueServiceClient.fromConnectionString(connectionString);
const queueClient = queueServiceClient.getQueueClient(queueName);

module.exports = async function (context, req) {
    if (req.body) {
        // Base64 encode message
        let message = req.rawBody;
        let messageBuffer = new Buffer.from(message, 'utf-8');
        let encodedMessage = messageBuffer.toString('base64');
        
        //await queueClient.sendMessage(req.body.body);
        await queueClient.sendMessage(encodedMessage);
        context.res = {
            body: "Message added!",
            status: 200
        }
    } else {
        context.res = {
            status: 400,
            body: "Request contains no body!"
        };
    }
};