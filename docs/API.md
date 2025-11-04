# Authentication Relay Service API Documentation

## Table of Contents

- [SDK Communications](#sdk-communications)
- [Authentication Relay Service API](#authentication-relay-service-api)
- [Walmart ICS Account Linking API](#walmart-ics-account-linking-api)
- [Sample Call Flow](#sample-call-flow)
- [Walmart Authentication](#walmart-authentication)
- [API Calls](#api-calls)
- [Account Linking](#account-linking)
- [Order Processing](#order-processing)

## SDK Communications

The reference Authentication Relay Service is built on [Unity Cloud Code](https://docs.unity.com/ugs/manual/cloud-code/manual) and uses [Unity Player Authentication](https://docs.unity.com/ugs/manual/authentication/manual/overview). When integrating the SDK with your game you'll be able to continue using this model or configure the SDK to use your own backend and authentication.

When configuring the SDK the base URL for communicating with an Authentication Relay Service can be configured, as well any extra headers such as a custom bearer token for authentication can be injected into requests. However the request verbs and paths for all the needed ARS endpoints for account linking and checkout are fixed. This includes the request and response message formats, all of which will be json.

![SDK Configuration](sdk-config.png)

In this example if the base URL was specified in the SDK as https://example.com/auth-relay-service/ (the trailing slash is required), the SDK would make requests to the GetLoginUrl endpoint at the URL:

`POST https://example.com/auth-relay-service/GetLoginUrl`

### Authentication Relay Service API

These are the APIs that must be implemented by a developer when creating their own Authentication Relay Service, the list can be found in [this OpenAPI specification](ars_openapi-v2.yaml). The Immersive Commerce SDK expects all these calls to be implemented to be compatible, the functionality of each of these calls will be explained in further details [later in this document](#account-linking).

### Walmart ICS Account Linking API

In essence, the ARS is a proxy between the SDK and Walmart's Immersive Commerce Service. The specification of the calls the ARS must make to Walmart's ICS can be found in [this OpenAPI specification](walmart-ics-openapi-v3.yaml). Like the ARS API, further details on how to use these calls will be explained [later in this document](#account-linking).

### Sample call flow

```mermaid
sequenceDiagram
  actor U as User
  participant Game as Game
  participant ARS as Authentication Relay Service
  participant IAM as Walmart IAM
  participant ICS as Walmart ICS

  U->>+Game: Initiate action
  Game->>+ARS: Request
  ARS->>+IAM: Client credential request
  IAM-->>-ARS: Bearer token
  ARS->>+ICS: Action call
  ICS-->>-ARS: Results
  ARS-->>-Game: Results
  Game-->-U: Response
```

## Walmart Authentication

In order to call Walmart APIs a standard bearer token in an Authentication header must be provided. Your title's client id and secret which will be exchanged for the bearer token. With these credentials you'll be able to call the Account Linking and Checkout APIs on behalf of a user.

#### Example Request

```http request
GET /api-proxy/service/ics-account-linking/api/v1/titleid/b6fd14df-1c1d-418f-805f-1340bb4584db/login-url
Host: developer.api.us.walmart.com
WM_CONSUMER.ID: b6fd14df-1c1d-418f-805f-1340bb4584db
Authorization: Bearer eyJraWQiOiJmMGM5MzZmNC1lZDNhLTRjNTQtYTE3ZS1iZWQ2MTdhODk0MjYiLCJlbmMiOiJBMjU2R0NNIiwiYWxnIjoiZGlyIn0.."
```
### Bearer Token

During onboarding game developers will receive a client id/secret pair per title. These credentials can be used to exchange for a bearer token from the Walmart IAM service using the [standard Client Credentials Grant flow](https://datatracker.ietf.org/doc/html/rfc6749#section-4.4).

Walmart IAM url: `https://developer.api.stg.walmart.com/api-proxy/service/identity/oauth/v1/token`

```mermaid
sequenceDiagram
    participant G as Auth Relay Service
    participant IAM as Walmart IAM
    participant ICS as Walmart ICS
    
    G ->>IAM: Client Credentials
    IAM-->>G: Bearer token
    G->>ICS: Authentication: Bearer token
    ICS-->>G: Response
```

#### Example request

```http request
POST /api-proxy/service/identity/oauth/v1/token HTTP/1.1
Host: developer.api.walmart.com:443
Content-Type: application/x-www-form-urlencoded

client_id=00001111-aaaa-2222-bbbb-3333cccc4444
&client_secret=YOUR_CLIENT_SECRET
&grant_type=client_credentials
```

#### Example response

```json
{
    "access_token": "ACCESS_TOKEN",
    "token_type": "Bearer",
    "expires_in": 900
}
```

## API Calls

### Property naming conventions

When mapping between incoming call from the SDK to the Authentication Relay Service and Walmart's ICS, and vice versa, care should be taken with respect to JSON field naming conventions.

Both the SDK and ICS require camel case for all property names, please ensure your JSON serializer is appropriately configured. Developers should double check the OpenAPI spec, there are a few exceptions where a slightly different capitalization makes more logical sense, such as `needVerifyCVV`.

### Reauthentication

After one year the refresh token held on behalf of a player by Walmart ICS expires. Once this happens the player will need to go through the login process again to relink their account to the game.

Walmart ICS will signal this situation in its response with a `401 UNAUTHORIZED` and error code 80004.

```json
{
  "errors": [
    {
      "code": 80004,
      "message": "Consumer id is invalid or mismatched"
    }
  ],
  "timestamp": "2019-08-24T14:15:22Z",
  "operationId": "3051932a-fdd2-48fa-b330-7e7d41535969"
}
```

In this situation the ARS should respond to the SDK signaling the situation using a `200 OK` status code.

```json
{
  "errors": [
    {
      "code": 80004,
      "message": "Relogin Required"
    }
  ],
  "payload": {}
}
```

## Account Linking

When a player wishes to link their Walmart account to the game for faster purchases or to use existing saved credit cards and addresses, the SDK will first call the `Get Login Url` endpoint, this will return a game specific login link at walmart.com. Depending on if the `platform` parameter is given in the call, after login on walmart.com the player will either be redirected to the game (mobile) or to a developer provided web service which returns control back to the WebGL frame. A sample of what the WebGL callback handler might look like can be found in [this package](../WebGLRedirect/README.md).

Once flow control has been returned to the game, the SDK will continue with the standard oauth flow and use the one time auth code to call the `Link Account` endpoint. The ARS will then call the same endpoint on ICS and receive back an LCID for the player, this identifier must be kept secure and should be associated with the player logging in. The LCID is what will be used by the ARS to prepare and place orders on the player's behalf.

Optionally, if a player wishes to disassociate their Walmart account from the game the SDK will call the `Unlink Account` endpoint, the ARS will call the same on ICS. Upon success, the ARS should remove it's association between this LCID and the user account.

The account linking flow will also be triggered by the SDK if Walmart ICS signals the user's bearer token they store for the user has expired and a new association is required. In this case the SDK will drive the same `Fetch Login Url` and `Link Account` flow, the LCID received back from ICS should be the same, but ARS developers should validate this and update their association to the user account if needed.

```mermaid
sequenceDiagram
    participant G as Game
    participant W as Browser
    participant A as Auth Relay Service
    participant F as Web Server
    participant ICS as Walmart ICS
    participant IAM as Walmart IAM
    
    G->>+A: Get login url
    A-->>-G: login url
    G->>+W: Open Walmart login page
    W->>W: Enter credentials
    W->>+IAM: Submit credentials
    IAM-->>-W: Redirect with auth code
    W->>+ICS: Navigate to redirect controller
    ICS-->>-W: Redirect<br>with auth code
    alt WebGL Platform
        W->>+F: Redirect to developer webserver<br>with auth code
        F-->>-G: Redirect to game via deeplink<br>with auth code
    else Mobile Platform
        W-->>-G: Redirect to game via deeplink<br>with auth code
    end
    G->>+A: Call link account with auth code
    A->>+ICS: Link user with auth code
    ICS-->>-A: Return LCID
    A->>A: Associate LCID with user account
    A-->>-G: Return success 
    opt Unlink Account
        G->>+A: Unlink account
        A->>+ICS: Unlink account
        ICS-->>-A: success
        A->>A: Delete LCID association with user account
        A-->>-G: success
    end
```

### Get Login URL

To allow users to login to Walmart in your game, a url for walmart.com's login page has to be generated. It will contain your game's unique identifier and callback, plus a nonce to prevent replay attacks.

The SDK will call the ARS GetLoginUrl, in return the ARS will call the ICS login-url and return the resulting url.

**ARS Endpoint:** `POST /GetLoginUrl`

**ICS Endpoint:** `GET /titleid/{titleId}/login-url`

### Link Account

After the user enters their credentials to walmart.com, Walmart will redirect the user to the deeplink associated with the game. This will bring the user back to the SDK in the game where the next phase of the oauth flow will continue. The SDK will take the auth code and call Link Account on the ARS, which in turn will call linkCustomer on ICS.

Walmart ICS will return the LCID to the ARS, the ARS is expected to securely save this identifier and use it when calling ICS on behalf of the user in all future request. 

**ARS Endpoint:** `POST /LinkAccount`

**ICS Endpoint:** `POST /titleid/{titleId}/lcid`

### Unlink Account

If the user requests to unlink their Walmart account from the game, the SDK will call UnlinkAccount. The ARS should then call the ICS delinkCustomer endpoint using the LCID associated with the player.

**ARS Endpoint:** `POST /UnlinkAccount`

**ICS Endpoint:** `DELETE /titleid/{titleId}/lcid/lcidheader`

## Order Processing

```mermaid
sequenceDiagram
    actor  P as Player
    participant G as Game
    participant A as Auth Relay Service
    participant ICS as Walmart ICS
    
    P->>+G: Initiate checkout
    G->>+A: Get Account Details
    A->>A: Retrieve LCID
    A->>+ICS: Get Account Details
    ICS-->>-A: account_details
    A-->>-G: account_details
    G-->>-P: Account details
    P->>P: Select shipping and payment
    P->>+G: Place order
    G->>+A: Prepare Order
    A->>A: Retrieve LCID
    A->>+ICS: Prepare Order
    ICS-->>-A: order_details
    A-->>-G: order_details
    G-->>-P: Order details
    P->>P: Review order
    opt Change shipping address
        P->>+G: Update order
        G->>+A: Set shipping address
        A->>A: Retrieve LCID
        A->>+ICS: Set shipping address
        ICS-->>-A: order_details
        A-->>-G: order_details
        G-->>-P: Updated order
    end
    P->>+G: Finalize order
    G->>+A: Place Order
    A->>A: Retrieve LCID
    A->>+ICS: Place Order
    ICS-->>-A: order_id
    A-->>-G: order_id
    G-->>-P: Receive confirmation
```

### Get Account Details

When a player initiates a checkout flow, the SDK will call GetAccountDetails, this will allow the user to select which shipping address or which credit card on file to use in the purchase. The SDK will also then know if the user must exit to walmart.com to validate their CVV before the purchase can be made.

When GetAccountDetails is called the ARS should call the ICS getAccountDetails endpoint. The returned data structure should be mapped back to the SDK, the two data structures are identical in properties.

**ARS Endpoint:** `POST /GetAccountDetails`

**ICS Endpoint:** `GET /titleid/{titleId}/lcid/lcidheader/account-details`

### Prepare Order

When a player places an order, the first action that occurs is the SDK sends a Prepare Order request, this is forwarded to ICS where any shipping charges or taxes are calculated. These details are returned to the SDK and player to review before finalizing the order.

The fields for the place order request and response map one-to-one, the the ARS should forward these parameters in both directions between the SDK and ICS.

**ARS Endpoint:** `POST /PrepareOrder`

**ICS Endpoint:** `POST /titleid/{titleId}/lcid/lcidheader/prepare-order`

### Set Shipping Address

If the player wishes to change the shipping address for their order the SDK will send a Set Shipping Address request, ARS will forward this to ICS. ICS will re-calculate any taxes and fees based on the new address and return the updated prepared order.

The fields for the set shipping request and response map one-to-one, the the ARS should forward these parameters in both directions between the SDK and ICS.

**ARS Endpoint:** `POST /SetShippingAddress`

**ICS Endpoint:** `POST /titleid/{titleId}/lcid/lcidheader/shipping-address`

### Place Order

When a player is ready to place an order the SDK will send a Place Order request. ARS will forward this to ICS and ICS will attempt to place the order. If successful, ICS will return a success message which is passed back to the SDK.

The fields for the place order request and response map one-to-one, the the ARS should forward these parameters in both directions between the SDK and ICS.

**ARS Endpoint:** `POST /PlaceOrder`

**ICS Endpoint:** `POST /titleid/{titleId}/lcid/lcidheader/place-order`
