# Using OAuth Sign-in in the Template

This guide walks you through adding "Continue with Google / LinkedIn / Microsoft / GitHub" to an app
built from this template, trying it locally, and taking it to production. For the protocol details and
the account-linking rules, see [EXTERNAL_SIGN_IN.md](EXTERNAL_SIGN_IN.md).

## Contents

1. [What you get](#what-you-get)
2. [Quick start: enable Google locally](#quick-start-enable-google-locally)
3. [Setting up each provider](#setting-up-each-provider)
4. [Configuration reference](#configuration-reference)
5. [Turning providers on and off](#turning-providers-on-and-off)
6. [Going to production](#going-to-production)
7. [Testing without real providers](#testing-without-real-providers)
8. [Adding another provider](#adding-another-provider)
9. [Troubleshooting](#troubleshooting)

## What you get

- **Four providers out of the box:** Google, LinkedIn, Microsoft and GitHub.
- **Two switches per provider.** A provider's button appears on the login page only when:
  - an admin has switched it on in **Site Settings → Sign-in Methods**, and
  - the server has its client ID and secret.
- **Secure by default.** The flow uses PKCE and single-use state, tokens never appear in URLs, and the
  post-login return path can't point to another site.
- **Accounts are handled for you.** New users are created with no roles (assign roles afterwards, as with
  registration). Returning users are recognised, and existing accounts are linked only when the provider
  has verified the email.

## Quick start: enable Google locally

Local defaults: the API runs at `https://localhost:5001` and the React app at `http://localhost:5173`.

### 1. Create a Google OAuth client

1. Open [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services** → **OAuth consent screen**.
   Configure it (the *External* user type is fine for testing) and add yourself as a test user.
2. Go to **Credentials** → **Create credentials** → **OAuth client ID** → application type **Web application**.
3. Under **Authorized redirect URIs**, add:

   ```
   https://localhost:5001/api/v1/auth/external/google/callback
   ```

4. Copy the **Client ID** and **Client secret**.

### 2. Give the API the credentials

Use .NET user secrets so the values stay out of source control:

```bash
cd ProjectNamePlaceholder.Api
dotnet user-secrets init   # once per project
dotnet user-secrets set "ExternalAuth:Providers:Google:ClientId" "<client id>"
dotnet user-secrets set "ExternalAuth:Providers:Google:ClientSecret" "<client secret>"
```

Environment variables work too (`ExternalAuth__Providers__Google__ClientId=...`).

### 3. Run and switch it on

1. Start the API (`dotnet run`) and the frontend (`npm run dev` in `olympus-react`).
2. Sign in as the super admin and open **Site Settings**.
3. In **Sign-in Methods**, turn on **Google**. The *Not configured* badge must be gone; if it isn't, the API
   didn't pick up the credentials.
4. Sign out. The login page now shows **Continue with Google**.

A first-time Google user gets a new account with no roles, so they see only the dashboard welcome. Give
them roles in **Users** or **User Roles**.

## Setting up each provider

Every provider needs the same redirect URI pattern, with the provider id in lower case:

```
{API origin}/api/v1/auth/external/{provider}/callback
```

| Provider | Id | Create the app at | What to set there |
|---|---|---|---|
| Google | `google` | Google Cloud Console → APIs & Services → Credentials | OAuth client ID of type *Web application*; add the redirect URI |
| LinkedIn | `linkedin` | [LinkedIn Developers](https://www.linkedin.com/developers/apps) → your app | **Products**: add *Sign In with LinkedIn using OpenID Connect*. **Auth**: add the redirect URI |
| Microsoft | `microsoft` | Entra admin center → **App registrations** → New registration | Supported accounts: *any organizational directory and personal Microsoft accounts*. Platform **Web**, add the redirect URI. Create a client secret under **Certificates & secrets** |
| GitHub | `github` | GitHub → **Settings** → **Developer settings** → **OAuth Apps** | Set **Authorization callback URL** to the redirect URI |

Then set `ClientId` and `ClientSecret` under `ExternalAuth:Providers:<Provider>` (see below).

**Provider-specific behaviour**

- **GitHub:** the template signs the user in with their primary *verified* email, not the profile email,
  which may be hidden.
- **Microsoft:** it never links to an existing password account by email, because Microsoft doesn't
  reliably verify email ownership. It can sign in returning users and create new accounts. A Microsoft
  user whose email already has an account sees "An account with this email already exists".

## Configuration reference

All settings live in the `ExternalAuth` section (`appsettings.json` has empty placeholders):

```json
"ExternalAuth": {
  "FrontendBaseUrl": "http://localhost:5173",
  "ApiBaseUrl": "",
  "Providers": {
    "Google":    { "ClientId": "", "ClientSecret": "" },
    "LinkedIn":  { "ClientId": "", "ClientSecret": "" },
    "Microsoft": { "ClientId": "", "ClientSecret": "" },
    "GitHub":    { "ClientId": "", "ClientSecret": "" }
  }
}
```

| Key | Purpose |
|---|---|
| `FrontendBaseUrl` | Where the browser goes after the provider callback (`/auth/callback` and `/login?error=...` are appended). |
| `ApiBaseUrl` | Public API origin used to build redirect URIs. Leave empty to use the request's own origin; set it when the API runs behind a proxy or load balancer. |
| `Providers:<P>:ClientId` / `ClientSecret` | Credentials from the provider. Both are required for the provider to count as *configured*. |
| `Providers:<P>:AuthorizationEndpoint`, `TokenEndpoint`, `UserInfoEndpoint` | Optional overrides. The defaults are the providers' public endpoints. |
| `Providers:<P>:Scopes` | Optional scope override (space-separated). |
| `Providers:GitHub:EmailsEndpoint` | Optional override for GitHub's email-list endpoint (default `https://api.github.com/user/emails`). |

The frontend needs nothing extra. It finds the API through `VITE_API_BASE_URL` and asks
`GET /api/v1/auth/providers` which buttons to show.

## Turning providers on and off

- **In the UI:** Site Settings → **Sign-in Methods**. Changing it needs the `site-settings.create`
  permission; viewing needs `site-settings.read`.
- **As data:** the switches are ordinary site settings, `Auth.Google.Enabled`, `Auth.LinkedIn.Enabled`,
  `Auth.Microsoft.Enabled` and `Auth.GitHub.Enabled`, with the value `true` or `false`. They're seeded as
  `false` on first start.

Turning a provider off hides its button and makes its start and callback URLs refuse new sign-ins
straight away. Accounts created through it keep working; those users can set a password with
**Forgot password**.

## Going to production

1. **Register the production redirect URI** at each provider, e.g.
   `https://api.example.com/api/v1/auth/external/google/callback`. Most providers allow several URIs, so
   you can keep the local one in a separate dev app.
2. **Set `FrontendBaseUrl`** to the deployed app, e.g. `https://app.example.com`.
3. **Set `ApiBaseUrl`** if the API sits behind a proxy, so redirect URIs use the public `https://` origin.
4. **Store secrets in a secret store** (environment variables, Key Vault, etc.); never in `appsettings.*.json`.
5. **Enable Redis** (`Caching:UseRedis=true`) if you run more than one API instance. Pending sign-ins and
   one-time codes live in the distributed cache, and the in-memory default isn't shared between instances.
6. **Publish the consent screen** (Google) or app (LinkedIn/Microsoft) so users outside your test list can sign in.

## Testing without real providers

Both test suites include a mock OAuth provider that checks the client secret and PKCE like a real one.
Point Google, Microsoft and GitHub at it when starting the API:

```bash
M=http://127.0.0.1:5099
export ExternalAuth__Providers__Google__ClientId=e2e-client ExternalAuth__Providers__Google__ClientSecret=e2e-secret \
  ExternalAuth__Providers__Google__AuthorizationEndpoint=$M/authorize \
  ExternalAuth__Providers__Google__TokenEndpoint=$M/token \
  ExternalAuth__Providers__Google__UserInfoEndpoint=$M/userinfo
# repeat the same five settings for Microsoft; for GitHub use
# UserInfoEndpoint=$M/github/user and EmailsEndpoint=$M/github/emails
```

Then run:

```bash
python3 tests/e2e/api_e2e.py --api http://localhost:5091   # API checks (EXT-001 to EXT-025)
cd ../olympus-react && npm run test:e2e                     # includes the full browser journey
```

See [tests/e2e/README.md](../tests/e2e/README.md) for starting the API on a test database.

## Adding another provider

Any provider that supports the OAuth 2.0 authorization-code flow and an OpenID Connect `userinfo`
endpoint (returning `sub`, `email`, `email_verified`, `name`, `picture`) needs only small changes:

1. **Register it:** add an entry to `ExternalProviders.All` in
   `ProjectNamePlaceholder.Application/Auth/External/ExternalProviders.cs`:

   ```csharp
   new(Gitlab, "GitLab", true),   // id, display name, provider verifies email?
   ```

   Only set the last value to `true` if the provider guarantees `email_verified` is accurate. It decides
   whether existing accounts may be linked by email.

2. **Give it default endpoints:** add it to `KnownProviders` in
   `ProjectNamePlaceholder.Infrastructure/Authentication/External/OAuthIdentityProvider.cs`
   (authorization, token, userinfo and default scopes). If its profile isn't standard OIDC, add a mapper
   alongside `MapGitHubAsync`.

3. **Seed its switch:** add `Auth.<DisplayName>.Enabled` with the value `false` in
   `ProjectNamePlaceholder.Persistence/Seeding/DefaultSiteSettingsSeeder.cs`. Existing databases get it on
   the next start.

4. **Add the config placeholder** to `ExternalAuth:Providers` in `appsettings.json`.

5. **Optional: add a button icon** in `olympus-react/src/features/auth/components/ExternalSignInButtons.tsx`
   (`ICONS`, keyed by provider id). Without one, a generic key icon is shown.

No new endpoints, routes or database changes are needed. The provider appears in Site Settings
automatically.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Provider shows **Not configured** | `ClientId` or `ClientSecret` is empty or misspelled. Section names are case-insensitive, but the path must be `ExternalAuth:Providers:<Provider>`. |
| Button missing although the switch is on | Same as above: buttons appear only for providers that are both on *and* configured. |
| Provider error `redirect_uri_mismatch` | The registered redirect URI differs from what the API sends (scheme, host, port or provider id case). Set `ApiBaseUrl` behind a proxy. |
| Lands on login with "That sign-in attempt expired" | More than 10 minutes passed, the page was refreshed mid-flow, or multiple API instances run without Redis. |
| "An account with this email already exists" | A password account has that email and the provider didn't verify it (always the case for Microsoft). Sign in with the password instead. |
| "The provider did not share an email address" | The provider returned no email. Check the requested scopes; on GitHub, make sure the account has a verified primary email. |
| New user sees an empty menu | Expected: external sign-up gives no roles. Assign roles in **Users**. |
| Lands on the wrong app URL after sign-in | `FrontendBaseUrl` doesn't match where the React app is served. |

API logs record provider errors (for example a failed token exchange) at warning level under
`ExternalAuthService`; the user only sees a generic message.
