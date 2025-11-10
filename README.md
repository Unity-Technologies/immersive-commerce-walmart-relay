# Walmart Immersive Commerce - Authentication Relay Service

## Disclaimer
The repo does not accept pull requests, GitHub review requests, or any other GitHub-hosted issue management requests.

## Table of Contents

- [Overview](#overview)
- [Target Audience](#target-audience)
- [Initial Setup](#initial-setup)
- [Configuration](#configuration)
- [Deployment](#deployment)
  - [Sandbox Deployments](#sandbox-deployments)
- [Testing](#testing)
  - [Manually using curl](#manually-using-curl)
  - [Testing Example](#testing-example)
- [Useful Links](#useful-links)

## Overview

To reduce friction for in-game sales, players are able to link their existing Walmart account to games using the Walmart Immersive Commerce Service SDK. To facilitate this linking, developers are required to have an Authentication Relay Service(ARS) to bridge requests between the SDK and Walmart APIs.

The ARS will be the custodian of the credentials needed to call Walmart APIs as well as maintaining the linkage between the player and their Walmart account. For this reason a game's ARS needs to maintain a high level of security and be deployed in an environment with tools such as a secrets manager available.

This project uses Unity Cloud Code and Unity RemoteConfig to set up a REST API that the Walmart Immersive Commerce SDK can then communicate with, to do authenticated tasks such as authenticated item checkout and purchase.

This sample project is intended to be a reference for the calls being made between a Unity application and the Walmart Immersive Commerce backend infrastructure. Applying the best security practices that are inline with your organization guidelines is your responsibility.

Potential areas of hardening:

* HTTP retry logic when doing REST calls to IAM and ICS
* Retry logic when making calls to Unity Cloud services
* Secrets handling through proper secrets service
* Performance and observability analytics through a service such as AWS X-Ray or Azure Monitor or Datadog 

## Target Audience

This project is intended to be used as an example of setting up authentication relay code and infrastructure that lives between a Unity application and the Walmart backend infrastructure. It would be deployed by a developer creating a Unity application that uses the Walmart Immersive Commerce to offer Walmart items for sale within their application. Each Unity project using this service requires its own separate Authentication Relay Service, as secret keys that it uses are specific to a single application and cannot be shared between multiple applications.

In all instructions below, "you" refers to the developer creating the Unity application.

Developers can also decide to implement their own Walmart relay service which could be hosted on premise or in any cloud provider infrastructure. Detailed technical specifications can be found in [API.md](docs/API.md)

## Initial Setup

This project uses the [Unity Gaming Services CLI](https://services.docs.unity.com/guides/ugs-cli/latest/general/overview/index.html) to deploy to Unity Cloud Code. To get started, install the CLI using the instructions at the UGS CLI documentation website.

Once you have the CLI installed, set the UGS project and environment to your specific values, both of which can be found at the UGS Dashboard. Note that the project ID is a GUID and the environment name is its actual name.

```bash
ugs config set project-id <your-project-id>
ugs config set environment-name <your-environment-name>
```

If you have not explicitly set up an environment, the default environment name is `production`.

Once you have set up your project ID and environment name, you will need to create a Service Account that the UGS CLI will use to deploy Cloud Code. To do this, follow the [Create A Service Account](https://services.docs.unity.com/docs/service-account-auth/index.html#create-a-service-account) instructions, and add the following project-level roles:

* Admin > Unity Environments Viewer
* LiveOps > Cloud Code Script Publisher
* LiveOps > Cloud Code Viewer
* LiveOps > Cloud Code Editor
* LiveOps > Remote Config Admin

Create a new key for this Service Account, and make sure you save the Key ID and Secret Key to private locations.

Using the Key ID and Secret Key, you can authorize with:

```bash
ugs login
```

This will ask for your Key ID and Secret Key. Automated login instructions and further information can be found at the [UGS CLI Login page](https://services.docs.unity.com/guides/ugs-cli/latest/general/base-commands/login/).

## Configuration

The Authentication Relay Service stores project settings in RemoteConfig, you will also need to deploy these settings. Documentation on how to deploy these can be found in the [SETUP.md](docs/SETUP.md) file.

## Deployment

To deploy this project to Cloud Code, when you are in the root directory, run:

```bash
ugs deploy .
```

To change which project or environment you are deploying to, simply change the project Id or environment name before deploying:

```bash
ugs config set project-id <your-project-id>
ugs config set environment-name <your-environment-name>
```

### Sandbox Deployments

To deploy using the ICS sandbox environment, you will first need to create a new `environment` in the project in the UGS Dashboard. Once you have created the environment, you can set the environment name to the new environment name and deploy to it.

```bash
ugs config set environment-name <your-sandbox-environment-name>
ugs deploy .
```

In addition, you will need to set the `WALMART_ICS_SANDBOX` value in the remote configuration file to `true` before deploying.

More details on sandbox testing can be found in [SANDBOX_TESTING.md](docs/SANDBOX_TESTING.md)

## Testing

### Manually using `curl`

Cloud Code makes its functions accessible through REST, so you can test your deployed code using a REST client such as `curl` or Postman. Endpoints use a bearer token authentication scheme, so it is necessary to authenticate before using the endpoints. The easiest way to do this is to login as an anonymous player,  then use the bearer token for subsequent Cloud Code API calls:

```bash
curl -XPOST -H 'ProjectId: <your-project-id>' -H 'Content-Type: application/json' \
 https://player-auth.services.api.unity.com/v1/authentication/anonymous
```

This will return an `idToken` that you can use for subsequent Cloud Code API calls. The token will expire after a certain amount of time denoted in the `expiresIn` field, and if it does you will need to [reauthenticate using the session token](https://services.docs.unity.com/player-auth/v1/#tag/Player-Authentication/operation/SignInWithSessionToken) from the `sessionToken` field, then use the new `idToken` field in subsequent calls.

To test against a Cloud Code endpoint:

```bash
curl -XPOST -H 'Authorization: Bearer <idToken>' -H 'Content-Type: application/json' \
 https://cloud-code.services.api.unity.com/v1/projects/<project-id>/modules/<module-name>/<function-name> \ 
 --data-raw '{"params": { "param1": "value1" }}'
```

#### Testing Example

The `TestModule` is included so that developers can check their Cloud Code modules have deployed properly. To test it out, get a bearer token for a user, and then run:

```bash
curl -XPOST -H 'Authorization: Bearer <idToken>' -H 'Content-Type: application/json' \
 https://cloud-code.services.api.unity.com/v1/projects/<project-id>/modules/WalmartAuthRelay/HelloWorld \
 --data-raw '{"params": { "name": "your name here" }}'
```

This should return `{"output":"Hello, your name here!"}`.


If you have deployed your Cloud Code C# Modules to an environment different from `production`, then set the `UNITY_CLOUD_ENVIRONMENT_NAME` variable before using either of the `Initial Login` or `Session Refresh` queries.

## Useful Links

* [Cloud Code](https://docs.unity.com/ugs/manual/cloud-code/manual)
* [Cloud Code CLI Overview](https://services.docs.unity.com/guides/ugs-cli/latest/cloud-code/Cloud%20Code%20Command%20Line/overview/)
* [Cloud Code Admin API](https://services.docs.unity.com/cloud-code-admin/v1/)
* [Cloud Code Client API](https://services.docs.unity.com/cloud-code/v1/)
* [Player Authentication API](https://services.docs.unity.com/player-auth/v1/)
