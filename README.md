# Octo Subscriber
This repository contains sample serverless applications for processing webhook notifications from [Octopus Deploy Subscription Events](https://octopus.com/docs/administration/managing-infrastructure/subscriptions).

# Support
This repository is intended for sample purposes only.  It is provided as-is and is not supported.

# Architecture
Each sample will follow the same basic architecture.

1. API application to accept a message via a webhook from Octopus Deploy and save it to a queue.
2. Process application to process messages from the queue.

Each application is used to monitor _one_ Octopus Deploy instance.  

# Infrastructure
Included in this sample is the TerraForm script to build up all the necessary infrastructure required to support this application on a cloud provider.