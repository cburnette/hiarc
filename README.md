# Hiarc

For more information see https://www.hiarcdb.com.

Much more to come in the near future!

## Settings

Use the following environment variables to configure Hiarc:

- `HIARC_CONFIG_STRATEGY=env`
- `HIARC_SETTINGS=<base64 encoded json>`

If using `docker-compose` update `HIARC_SETTINGS` before running `docker-compose up`.

`HIARC_SETTINGS` must be a base64 encoded JSON string. If you use the follow example, be sure to replace any values formatted like this: `<value>`. These are the following settings Hiarc supports:

```json
{
    "BaseUri": "http://localhost:5000",
    "JwtSigningKey": "<provide key value>",
    "AdminApiKey": "<provide key value>",
    "ForceHTTPS": false,
    "JWTTokenExpirationMinutes": 43200,
    "Database": {
        "Uri": "bolt://localhost:7687",
        "Username": "neo4j",
        "Password": "<password>"
    },
    "EventServices": [
        {
            "Provider": "Webhook",
            "Name": "webhook.site",
            "Enabled": false,
            "Config": {
                "URL": "<site url>",
                "Secret": "<secret>"
            }
        },
        {
            "Provider": "AWS-Kinesis",
            "Name": "hiarc-aws-kinesis",
            "Enabled": false,
            "Config": {
                "AccessKeyId": "<key>",
                "SecretAccessKey": "<secret>",
                "RegionSystemName": "us-east-1",
                "Stream": "hiarc-test"
            }
        },
        {
            "Provider": "Azure-ServiceBus",
            "Name": "hiarc-azure-servicebus",
            "Enabled": false,
            "Config": {
                "ConnectionString": "<connection string>",
                "Topic": "hiarc"
            }
        },
        {
            "Provider": "Google-PubSub",
            "Name": "hiarc-google-pubsub",
            "Enabled": false,
            "Config": {
                "ServiceAccountCredential": "<creds>",
                "ProjectId": "<project id>",
                "Topic": "hiarc"
            }
        }
    ],
    "StorageServices": [
        {
            "Provider": "AWS-S3",
            "Name": "hiarc-aws-s3-east",
            "IsDefault": true,
            "Config": {
                "AccessKeyId": "<key>",
                "SecretAccessKey": "<secret>",
                "RegionSystemName": "us-east-1",
                "Bucket": "hiarc-test"
            }
        },
        {
            "Provider": "Azure-Blob",
            "Name": "hiarc-azure-blob-1",
            "IsDefault": false,
            "Config": {
                "StorageConnectionString": "<connection string>",
                "Container": "hiarc-test"
            }
        },
        {
            "Provider": "Google-Storage",
            "Name": "hiarc-google-storage-east",
            "IsDefault": false,
            "Config": {
                "ServiceAccountCredential": "<creds>",
                "Bucket": "hiarc-test"
            }
        }
    ]
}
```

With these settings saved to a file and assuming the file is named settings.json, you can use the follow `bash` script to generate a base64 encoded string:

```sh
export HIARC_CONFIG_STRATEGY=env
export HIARC_SETTINGS=$(cat settings.json | base64)
```