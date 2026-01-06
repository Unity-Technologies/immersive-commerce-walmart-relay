# Setting up an Authentication Relay Server

## Table of Contents

1. [Register a project with the Walmart Immersive Commerce Service](#register-a-project-with-immersive-commerce-service)
2. [Generate an RSA public/private key pair](#generate-an-rsa-publicprivate-key-pair)
3. [Set up Remote Config](#set-up-remote-config)
4. [Deploy Cloud Code C# module](#deploy-cloud-code-c-module)

## Prerequisites

Before you begin, ensure you have:
- A Unity Dashboard account
- OpenSSL installed on your system
- Unity Gaming Services CLI installed and configured

## Setup Steps

### 1. Register a project with Immersive Commerce Service

Go online to https://dashboard.unity.com and register for the Walmart Immersive Commerce Service. You will need to:

1. Accept and sign the Terms of Usage and API Agreement
2. Upload a public key (see next section for generation instructions)
3. Retrieve your client ID and client secret from the site

### 2. Generate an RSA public/private key pair

#### Using OpenSSL

Use openssl in a terminal to generate a 2048 bit RSA key pair:

```bash
openssl genrsa -des3 -out WM_IO_my_rsa_key_pair 2048
```

Export the private key:

```bash
openssl pkcs8 -topk8 -inform PEM -in WM_IO_my_rsa_key_pair -outform PEM -out WM_IO_private_key.pem -nocrypt
```

Export the public key:

```bash
openssl rsa -in WM_IO_private_key.pem -pubout > WM_IO_public_key.pem
```

**Important:** Copy and paste the public key to the Unity Immersive Commerce dashboard, Setup page.

After retrieving the Walmart encrypted credential, use the private key to decrypt it:

```bash
base64 -d -i /path/to/credentials_encrypted.key | openssl pkeyutl -decrypt -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256 -inkey /path/to/WM_IO_private_key.pem
```

### 3. Set up Remote Config

There are some key/value pairs you need to set in Remote Config:

* `WALMART_IAM_CLIENT_ID` - the client ID given to you by Walmart to access the Walmart IAM service
* `WALMART_IAM_HOSTNAME` - the hostname for the Walmart IAM service you will be using
* `WALMART_ICS_HOSTNAME` - the hostname for the Walmart ICS service you will be using
* `WALMART_ICS_TITLE_ID` - the title ID given to you by Walmart to access the Walmart ICS service, likely the same as the client ID
* `WALMART_ICS_SANDBOX` - set to `true` if you want ARS to use the Walmart ICS sandbox environment

The Remote Config can be deployed to your project using the `ugs` CLI. Create a `configuration.rc` file with the values and from that directory execute:

```bash
ugs deploy .
```

#### Configuration File Example

Create a `configuration.rc` file with the following structure:

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json",
  "entries": {
    "WALMART_IAM_CLIENT_ID": "<client ID as described above>",
    "WALMART_IAM_HOSTNAME": "developer.api.walmart.com",
    "WALMART_ICS_HOSTNAME": "developer.api.us.walmart.com",
    "WALMART_ICS_TITLE_ID": "<title ID as described above>"
  },
  "types": {
  }
}
```

### 4. Set up Walmart client secret in Secret Manager
The following secret must be added at the project level: `WALMART_IAM_CLIENT_SECRET`. 
The secret is obtained from Unity dashboard, in the Setup section. More details about adding a secret can be found in the 
[documentation](https://docs.unity.com/ugs/en-us/manual/secret-manager/manual/tutorials/store-secrets#add-a-secret).

### 5. Set up LCID encryption key in Secret Manager
The PlayerDataService encrypts LCID values using AES-256-GCM.
Generate a secure 256-bit (32-byte) key and encode it as base64:
```bash
# Generate random 32-byte key and encode as base64
openssl rand -base64 32
```
Set the `LCID_ENCRYPTION_KEY` project secret using the value from above.

#### Important Notes
- The key must be exactly 256 bits (32 bytes) when decoded from base64
- Keep the key secure - losing it will make existing encrypted data unrecoverable

### 6. Deploy Cloud Code C# module

See the main [README.md](../README.md) file for deployment instructions.
