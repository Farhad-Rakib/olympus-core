# CI/CD Implementation Checklist

Complete checklist for setting up the GitLab CI/CD pipeline for ProjectNamePlaceholder.

## Pre-Implementation

- [ ] GitLab account with project access
- [ ] GitLab Runner configured (with Docker executor)
- [ ] Docker installed on deployment server
- [ ] Database servers ready (Dev, Staging, Prod)
- [ ] SMTP server configured (for email)
- [ ] SSL certificates prepared (if needed)

## Phase 1: Local Testing (30 minutes)

- [ ] Copy `Dockerfile` to project root
- [ ] Copy `.dockerignore` to project root
- [ ] Run `docker build -t projectnameplaceholder:test .`
- [ ] Test Docker image builds successfully
- [ ] Copy `docker-compose.yml` to project root
- [ ] Run `docker-compose --profile dev up`
- [ ] Verify API is accessible at `http://localhost:5000`
- [ ] Check PostgreSQL container is running
- [ ] Check MailHog is accessible at `http://localhost:8025`
- [ ] Run `docker-compose down` to cleanup

## Phase 2: Configuration Files (15 minutes)

- [ ] Copy `appsettings.Staging.json` to `ProjectNamePlaceholder.Api/`
- [ ] Copy `appsettings.Production.json` to `ProjectNamePlaceholder.Api/`
- [ ] Verify appsettings files use environment variables
- [ ] Test local build with `dotnet build`
- [ ] Test local publish with `dotnet publish`

## Phase 3: GitLab Setup (30 minutes)

- [ ] Copy `.gitlab-ci.yml` to repository root
- [ ] Copy `.env.template` to repository root
- [ ] Update `.gitignore` (or use `.gitignore-ci-cd`)
- [ ] Verify `.gitlab-ci.yml` syntax:
  ```bash
  gitlab-runner lint .gitlab-ci.yml
  ```
- [ ] Push files to repository:
  ```bash
  git add .gitlab-ci.yml .env.template appsettings.*.json Dockerfile .dockerignore
  git commit -m "Add GitLab CI/CD pipeline configuration"
  git push origin main
  ```
- [ ] Verify pipeline files exist in repository

## Phase 4: GitLab Variables Setup (20 minutes)

### Registry Access
- [ ] Go to **Settings → CI/CD → Variables**
- [ ] Add `CI_REGISTRY_USER`: GitLab username or robot account
- [ ] Add `CI_REGISTRY_PASSWORD`: GitLab token or password
- [ ] Mark both as **Masked**

### Development Environment (DEV_*)
- [ ] Add `DEV_DB_CONNECTION_STRING`
- [ ] Add `DEV_JWT_SECRET_KEY` (32+ characters)
- [ ] Add `DEV_JWT_ISSUER`
- [ ] Add `DEV_JWT_AUDIENCE`
- [ ] Add `DEV_SMTP_HOST`
- [ ] Add `DEV_SMTP_PORT`
- [ ] Add `DEV_SMTP_USERNAME`
- [ ] Add `DEV_SMTP_PASSWORD`
- [ ] Add `DEV_SMTP_FROM_ADDRESS`
- [ ] Mark all as **Masked** (especially secrets)

### Staging Environment (STAGING_*)
- [ ] Add `STAGING_DB_CONNECTION_STRING`
- [ ] Add `STAGING_JWT_SECRET_KEY` (32+ characters, different from DEV)
- [ ] Add `STAGING_JWT_ISSUER`
- [ ] Add `STAGING_JWT_AUDIENCE`
- [ ] Add `STAGING_SMTP_HOST`
- [ ] Add `STAGING_SMTP_PORT`
- [ ] Add `STAGING_SMTP_USERNAME`
- [ ] Add `STAGING_SMTP_PASSWORD`
- [ ] Add `STAGING_SMTP_FROM_ADDRESS`
- [ ] Mark all as **Masked**

### Production Environment (PROD_*)
- [ ] Add `PROD_DB_CONNECTION_STRING`
- [ ] Add `PROD_JWT_SECRET_KEY` (32+ characters, different from others)
- [ ] Add `PROD_JWT_ISSUER`
- [ ] Add `PROD_JWT_AUDIENCE`
- [ ] Add `PROD_SMTP_HOST`
- [ ] Add `PROD_SMTP_PORT`
- [ ] Add `PROD_SMTP_USERNAME`
- [ ] Add `PROD_SMTP_PASSWORD`
- [ ] Add `PROD_SMTP_FROM_ADDRESS`
- [ ] Mark all as **Masked**

## Phase 5: Repository Branches (10 minutes)

- [ ] Ensure `dev` branch exists
  ```bash
  git checkout -b dev
  git push -u origin dev
  ```
- [ ] Ensure `stage` branch exists
  ```bash
  git checkout -b stage
  git push -u origin stage
  ```
- [ ] Ensure `main` branch exists
  ```bash
  # Usually exists by default
  git push origin main
  ```
- [ ] Set branch protection rules (optional):
  - [ ] Require pull request for `main`
  - [ ] Require approvals for `main`

## Phase 6: First Pipeline Run (10 minutes)

- [ ] Push code to `dev` branch:
  ```bash
  git checkout dev
  git push origin dev
  ```
- [ ] Go to **CI/CD → Pipelines**
- [ ] Verify pipeline starts automatically
- [ ] Watch **build** stage complete
- [ ] Watch **test** stage complete
- [ ] Watch **docker:build:dev** stage complete
- [ ] Verify Docker image appears in **Packages & Registries → Container Registry**
- [ ] Check pipeline logs for any errors

## Phase 7: First Deployment (10 minutes)

- [ ] Go to **CI/CD → Pipelines**
- [ ] Find the pipeline that completed successfully
- [ ] Click **play** button next to `deploy:dev` job
- [ ] Monitor deployment progress
- [ ] Check for errors in deployment logs
- [ ] Verify container is running:
  ```bash
  docker ps | grep projectnameplaceholder
  ```
- [ ] Test API endpoint:
  ```bash
  curl https://localhost:5001/health
  ```

## Phase 8: Validation (15 minutes)

### Development
- [ ] Pipeline runs on `dev` branch push
- [ ] Docker image is tagged as `development-*`
- [ ] Deployment creates running container
- [ ] API is accessible and responds
- [ ] Logs are being generated

### Staging
- [ ] Pipeline runs on `stage` branch push
- [ ] Docker image is tagged as `staging-*`
- [ ] Manual deployment works
- [ ] Uses staging configuration correctly
- [ ] Database connectivity is successful

### Production
- [ ] Pipeline runs on `main` branch push
- [ ] Docker image is tagged as `production-*`
- [ ] Manual deployment requires manual trigger
- [ ] Uses production configuration correctly
- [ ] Sensitive data is not logged

## Phase 9: Documentation & Handoff (15 minutes)

- [ ] Review **QUICK-START-CICD.md**
- [ ] Review **CI-CD-SETUP.md**
- [ ] Share `.env.template` with team
- [ ] Document custom variables (if any)
- [ ] Create deployment runbook for team
- [ ] Set up Slack/email notifications (optional)

## Phase 10: Ongoing Maintenance

### Weekly
- [ ] Monitor pipeline success rate
- [ ] Review failed deployments
- [ ] Check container logs

### Monthly
- [ ] Rotate JWT secrets
- [ ] Update .NET dependencies
- [ ] Review Docker image sizes
- [ ] Check for security updates

### Quarterly
- [ ] Update base Docker images (.NET versions)
- [ ] Audit GitLab variables
- [ ] Review security practices
- [ ] Performance analysis

---

## Troubleshooting Checklist

### Pipeline doesn't start
- [ ] Verify `.gitlab-ci.yml` syntax is correct
- [ ] Check GitLab Runner is active and online
- [ ] Verify branch is configured to trigger pipeline
- [ ] Check GitLab user has project access

### Build fails
- [ ] Review build logs in GitLab UI
- [ ] Verify .NET projects build locally
- [ ] Check .csproj files are correct
- [ ] Verify NuGet packages can be restored

### Docker build fails
- [ ] Verify Dockerfile syntax is correct
- [ ] Check Docker is available on runner
- [ ] Verify all base images are accessible
- [ ] Review Docker build logs

### Deployment fails
- [ ] Verify all environment variables are set
- [ ] Check GitLab Registry access credentials
- [ ] Verify Docker image exists in registry
- [ ] Check deployment server has Docker installed
- [ ] Review deployment logs

### Container won't start
- [ ] Check all required environment variables are set
- [ ] Verify database is accessible
- [ ] Check network connectivity
- [ ] Review container logs: `docker logs container-name`

---

## Success Criteria

✅ Pipeline successfully builds on every branch push
✅ Tests pass and are reported in GitLab
✅ Docker images are created and pushed to registry
✅ Manual deployments work for all environments
✅ Applications start and are accessible
✅ Environment-specific configurations are loaded
✅ Logs are generated and accessible
✅ Health checks pass
✅ Database connections work
✅ Email service (SMTP) is configured

---

## Quick Reference

**Start local development:**
```bash
docker-compose --profile dev up
```

**Validate configuration:**
```bash
chmod +x deploy-config.sh
./deploy-config.sh dev
```

**View pipeline:**
GitLab UI → CI/CD → Pipelines

**View Docker images:**
GitLab UI → Packages & Registries → Container Registry

**View deployments:**
GitLab UI → Deployments → Environments

---

**Status**: Ready for implementation ✅
