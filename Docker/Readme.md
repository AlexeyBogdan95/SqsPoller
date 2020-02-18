# Getting Started
1. AWS dependencies:
    * SQS

# Run locally
1. Setup SQS locally using `docker-compose.yml` file.
2. Install [Commandeer](https://github.com/commandeer/open) - AWS Exporer GUI, helps you to view SQS queues.
3. Setup **any** fake credentials, i.e. `AWS_ACCESS_KEY_ID=foo` `AWS_SECRET_ACCESS_KEY=bar`.
4. Create test bucket `aws --endpoint-url=http://localhost:4576 sqs create-queue --queue-name TestClient`. In general `aws --endpoint-url` allows you to call any localstack service

# Useful commands
* `aws --endpoint-url=http://localhost:4576 sqs list-queues` - get all queues
