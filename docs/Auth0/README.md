# Auth0

Hive requires an Authentication serivce to run. Auth0 support is built in.

Auth0 is company which provides authentication and authorization as a service. They have different priced tiers, but their free tier will realistically be enough for the majority of use cases.

This guide covers setting up an Auth0 tenant for use in Hive. You can use these instructions for both development, staging, and production.

## Create Tenant

Log in or sign up for Auth0 and go to the dashboard. If you already have a tenant, you can use your default tenant or create a new one.

You can set the Tenant domain, Region, and Environment Tag to what you desire.

## Applications

### APIs

1. Navigate to Applications -> APIs.
2. Create a new API
    * Set the Name to what you desire
    * Set the Identifier to what you desire. Record this value somewhere for your configuration.
    * Set the Signing Algorithm to `RS256`

This should create an Auth0 Machine to Machine Application named `<Your API Name> (Test Application)`

### Applications

1. Navigate to Applications -> Applications
2. Go to the newly created Application (`<Your API Name> (Test Application)`)
3. Under The Settings Tab...
    * Record the values of `Domain`, `Client ID`, and `Client Secret` somewhere for your configuration.
    * In Allowed Callback URLs, add the endpoint to what you want to redirect to after auth.
    If you're just working on the Hive API, you can set this to a random localhost url like `http://localhost:10000`.
    * In the Advanced Settings Grant Types tab, ensure that `Authorization Code`, `Refresh Token`, and `Client Credentials` are selected.

## Hive

In your API configuration, customize the `Auth0` section.

`appsettings.json, secrets.json, etc.`
```jsonc
{
    // ...
    "Auth0": {
        "Domain": "https://your-domain.region.auth0.com/", // Make sure you include "https://" at the front and the "/" at the end.
        "Audience": "<your identifier>", // This is the Identifier you made when creating the API in Auth0
        "ClientID": "<your client id>",
        "ClientSecret": "<your client secret>"
    }
}
```

## Testing Your API

Run the Hive API.

Navigate to `/api/auth0/get_data`. This should return a JSON response containing your Auth0 tenant data.

Combine the `domain` and `clientId` as so. You will also need to specify one of the URLs from your callback settings in Auth0 to be the redirect uri.

`[THE DOMAIN]authorize?response_type=code&client_id=[THE CLIENT ID]&redirect_uri=[REDIRECT URI]&scope=openid profile`

Using the data above, it would look something like this.

`https://your-domain.region.auth0.com/authorize?response_type=code&client_id=<your client id>&redirect_uri=http://localhost:10000&scope=openid profile`

Run this url in your browser. Sign in using the options provided (you can customize these options in Auth0 under Authentication). It should redirect to the url specified with a `code` query parameter.

Navigate to `/api/auth0/token?code=[CODE]&redirectUri=[REDIRECT URI]`. You should receive a JSON response with the properties `Access_Token`, `Refresh_Token`, `Id_Token` and `Token_Type`.

Use `Bearer <your access token>` as your Authorization header when making requests to your Hive instance.

You can also paste the `Id_Token` into a website like [jwt.io](https://jwt.io) to ensure that you've logged in successfully.

## Done

You now should have authorization and authentication fully functioning for your Hive API.