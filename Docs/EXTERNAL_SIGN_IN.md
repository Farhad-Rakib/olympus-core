# External sign-in (Google, LinkedIn, Microsoft, GitHub)

Users can sign in with an external provider instead of a password. A provider's button appears on
the login page only when **both** are true:

1. **It is switched on**: Site Settings → *Sign-in Methods* (the `Auth.<Provider>.Enabled` site setting).
2. **The server has credentials**: `ExternalAuth:Providers:<Provider>:ClientId` and `ClientSecret`.
   Providers without credentials show *Not configured* in Site Settings.

## Setting up a provider

Create an OAuth app at the provider and register this **redirect URI** (lower-case provider id):

```
{API origin}/api/v1/auth/external/{google|linkedin|microsoft|github}/callback
```

| Provider | Where to create the app | Notes |
|---|---|---|
| Google | Google Cloud Console → APIs & Services → Credentials → OAuth client ID (Web) | Scopes `openid email profile` |
| LinkedIn | LinkedIn Developers → app → Auth; add the *Sign In with LinkedIn using OpenID Connect* product | Scopes `openid profile email` |
| Microsoft | Entra admin center → App registrations (accounts in any org + personal) | Uses the `common` endpoint |
| GitHub | GitHub → Settings → Developer settings → OAuth Apps | Uses the primary **verified** email |

Then supply the secrets through environment variables or a secret store (never commit them):

```bash
ExternalAuth__Providers__Google__ClientId=...
ExternalAuth__Providers__Google__ClientSecret=...
ExternalAuth__FrontendBaseUrl=https://app.example.com   # where users land after sign-in
ExternalAuth__ApiBaseUrl=https://api.example.com        # set when the API runs behind a proxy
```

Endpoints and scopes can be overridden per provider (`AuthorizationEndpoint`, `TokenEndpoint`,
`UserInfoEndpoint`, `Scopes`, and `EmailsEndpoint` for GitHub), which is how the E2E tests point Google,
Microsoft and GitHub at a mock provider.

## How it works

1. The login button opens `GET /api/v1/auth/external/{provider}/start?returnUrl=/path`. The API stores
   a one-time `state` and a PKCE verifier (10 minutes) and redirects to the provider.
2. The provider redirects back to `.../callback`. The API checks `state`, exchanges the code (with PKCE),
   reads the profile, finds or creates the user, and redirects the browser to
   `{FrontendBaseUrl}/auth/callback?code=...&returnUrl=...` with a **one-time login code** (1 minute).
3. The app posts that code to `POST /api/v1/auth/external/exchange` and receives the usual tokens.
   Tokens never appear in a URL.

On failure the browser lands on `/login?error=<code>` (for example `external_cancelled`,
`external_email_in_use`), which the login page turns into a message.

## Accounts

- **Returning users** are matched by the provider's user id (table `external_logins`).
- **New emails** create an account with **no roles**, like self-registration. The account gets a random
  password; the user can set a real one with *Forgot password*.
- **An existing account with the same email** is linked only when the provider vouches for the email
  (Google and LinkedIn with `email_verified`, GitHub's verified primary email). Microsoft does not reliably
  verify email ownership, so it never links to an existing account by email. Such sign-ins are refused
  with `external_email_in_use` rather than allowing an account takeover.
- Disabled accounts cannot sign in.

`state`, login codes and PKCE verifiers live in the distributed cache. Enable Redis
(`Caching:UseRedis`) when running more than one API instance.
