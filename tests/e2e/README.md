# API end-to-end tests

`api_e2e.py` covers the API side of `Docs/END_TO_END_TEST_CASES.md` (auth, RBAC, admin CRUD,
settings, audit, system operations, validation and security). It needs only Python 3.

## Run

1. Start the API against a disposable database, pointing SMTP at the suite's built-in mail capture:

   ```bash
   cd ProjectNamePlaceholder.Api
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5091 \
   ConnectionStrings__PostgresConnection="Host=localhost;Port=5432;Database=olympus_e2e;Username=postgres;Password=postgres" \
   Smtp__Host=127.0.0.1 Smtp__Port=1025 Smtp__UseSsl=false \
   dotnet run --no-launch-profile
   ```

   To include the external sign-in checks, also point Google, Microsoft and GitHub at the suite's mock
   OAuth provider (otherwise those checks are skipped):

   ```bash
   M=http://127.0.0.1:5099
   export ExternalAuth__Providers__Google__ClientId=e2e-client ExternalAuth__Providers__Google__ClientSecret=e2e-secret \
     ExternalAuth__Providers__Google__AuthorizationEndpoint=$M/authorize ExternalAuth__Providers__Google__TokenEndpoint=$M/token \
     ExternalAuth__Providers__Google__UserInfoEndpoint=$M/userinfo \
     ExternalAuth__Providers__Microsoft__ClientId=e2e-client ExternalAuth__Providers__Microsoft__ClientSecret=e2e-secret \
     ExternalAuth__Providers__Microsoft__AuthorizationEndpoint=$M/authorize ExternalAuth__Providers__Microsoft__TokenEndpoint=$M/token \
     ExternalAuth__Providers__Microsoft__UserInfoEndpoint=$M/userinfo \
     ExternalAuth__Providers__GitHub__ClientId=e2e-client ExternalAuth__Providers__GitHub__ClientSecret=e2e-secret \
     ExternalAuth__Providers__GitHub__AuthorizationEndpoint=$M/authorize ExternalAuth__Providers__GitHub__TokenEndpoint=$M/token \
     ExternalAuth__Providers__GitHub__UserInfoEndpoint=$M/github/user ExternalAuth__Providers__GitHub__EmailsEndpoint=$M/github/emails
   ```

2. Run the suite (exit code is non-zero if any check fails):

   ```bash
   python3 tests/e2e/api_e2e.py --api http://localhost:5091
   ```

Options: `--smtp-port`, `--mock-oauth-port` (default 5099), `--superadmin-email`, `--superadmin-password`.
Test records get a per-run suffix, so the suite can be re-run on the same database and
alongside the browser tests in `olympus-react/e2e`.
