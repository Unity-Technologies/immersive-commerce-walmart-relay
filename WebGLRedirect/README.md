# WebGL Redirect

In order to pass control back to a WebGL build after login, Walmart needs to redirect to a middle-man page that passes the auth code back to the SDK.

This basic script is an example of a GCP Cloud Function that takes the code and nonce parameters from the Walmart callback and calls the SDK via javascript.

```mermaid
sequenceDiagram
  participant W as Webgl
  participant B as Browser
  participant ARS as ARS
  participant ICS as ICS
  participant LOGIN as Walmart website
  participant C as Cloud Function
  
  W->>ARS: Get login url
  ARS->>ICS: Get login url
  ICS-->>ARS: login url
  ARS-->>W: login url
  W->>B: Open url
  B->>LOGIN: Fetch login page
  LOGIN-->>B: Login html
  B->>LOGIN: Submit credentials
  LOGIN-->>B: redirect to callback
  B->>C: Fetch callback url
  C-->>B: callback html & js
  B-->>W: Message auth code
  B->>B: close window
  W->>ARS: Link Account
  ARS->>ICS: Link Account
  ICS-->>ARS: return lcid
  ARS->>ARS: save lcid
  ARS-->>W: Confirm login
```

Good sample documentation, [Login flow for Unity WebGL applications](https://developer.okta.com/blog/2021/02/26/unity-webgl-playfab-authorization).
