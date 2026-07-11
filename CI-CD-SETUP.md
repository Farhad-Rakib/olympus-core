# CI/CD Configuration Guide

This document explains the GitLab CI/CD pipeline setup for ProjectNamePlaceholder with Docker support across Development, Staging, and Production environments.

## Overview

The pipeline is configured to:
- **Automatically build** .NET 8 application on every push to dev, stage, or main branches
- **Run tests** during the build process
- **Build Docker images** using multi-stage Dockerfile
- **Push images** to GitLab Container Registry
- **Deploy** to respective environments (manual trigger)
- **Support environment-specific configurations**

## Branch Strategy

| Branch | Environment | Auto Deploy | Manual Deploy |
|--------|-------------|-------------|---------------|
| `dev` | Development | ❌ | ✅ |
| `stage` | Staging | ❌ | ✅ |
| `main` | Production | ❌ | ✅ |

## Pipeline Stages

### 1. Build Stage
- Compiles the .NET application in Release mode
- Publishes the application
- Creates build artifacts

### 2. Test Stage
- Runs unit tests
- Generates test reports
- Fails pipeline if tests fail

### 3. Docker Build & Push
- Builds Docker image using multi-stage build
- Tags with environment name and commit SHA
- Pushes to GitLab Container Registry

### 4. Deploy Stage (Manual)
- Pulls Docker image from registry
- Runs container with environment-specific variables
- Configures based on appsettings.{Environment}.json

## Required GitLab Variables

Set these variables in GitLab CI/CD Settings (**Settings → CI/CD → Variables**):

### Global Variables
```
CI_REGISTRY_USER         = gitlab user or robot account
CI_REGISTRY_PASSWORD     = gitlab token/password
```

### Development Environment Variables (DEV_*)
```
DEV_DB_CONNECTION_STRING     = postgres connection string
DEV_JWT_SECRET_KEY          = JWT secret (min 32 chars)
DEV_JWT_ISSUER              = JWT issuer claim
DEV_JWT_AUDIENCE            = JWT audience claim
DEV_SMTP_HOST               = SMTP host
DEV_SMTP_PORT               = SMTP port (587)
DEV_SMTP_USERNAME           = SMTP username
DEV_SMTP_PASSWORD           = SMTP password
DEV_SMTP_FROM_ADDRESS       = From email address
```

### Staging Environment Variables (STAGING_*)
```
STAGING_DB_CONNECTION_STRING
STAGING_JWT_SECRET_KEY
STAGING_JWT_ISSUER
STAGING_JWT_AUDIENCE
STAGING_SMTP_HOST
STAGING_SMTP_PORT
STAGING_SMTP_USERNAME
STAGING_SMTP_PASSWORD
STAGING_SMTP_FROM_ADDRESS
```

### Production Environment Variables (PROD_*)
```
PROD_DB_CONNECTION_STRING
PROD_JWT_SECRET_KEY
PROD_JWT_ISSUER
PROD_JWT_AUDIENCE
PROD_SMTP_HOST
PROD_SMTP_PORT
PROD_SMTP_USERNAME
PROD_SMTP_PASSWORD
PROD_SMTP_FROM_ADDRESS
```

## Docker Image Naming

Images are tagged in the following format:

```
registry.gitlab.com/group/projectnameplaceholder:{environment}-{commit-sha}
registry.gitlab.com/group/projectnameplaceholder:{environment}-latest
```

Example:
```
registry.gitlab.com/group/projectnameplaceholder:development-a1b2c3d4
registry.gitlab.com/group/projectnameplaceholder:staging-latest
registry.gitlab.com/group/projectnameplaceholder:production-a1b2c3d4
```

## Configuration Files

### appsettings.json
Base configuration with default values and placeholders

### appsettings.Development.json
Development-specific overrides (debug logging, local services)

### appsettings.Staging.json
Staging-specific overrides (uses environment variables)

### appsettings.Production.json
Production-specific overrides (uses environment variables, minimal logging)

## How to Use

### 1. Initial Setup

1. Navigate to your GitLab project
2. Go to **Settings → CI/CD → Variables**
3. Add all required variables for each environment
4. Ensure Docker is enabled in your GitLab runner

### 2. Running the Pipeline

**Development:**
```bash
# Push to dev branch
git push origin dev

# In GitLab, click "Run Pipeline" or wait for automatic trigger
# Click "play" button on "deploy:dev" job
```

**Staging:**
```bash
# Push to stage branch
git push origin stage

# Manually trigger deploy:staging job
```

**Production:**
```bash
# Push to main branch
git push origin main

# Manually trigger deploy:production job (requires approval)
```

### 3. Viewing Results

- **Pipeline Status**: Go to **CI/CD → Pipelines**
- **Build Logs**: Click on job name to see detailed logs
- **Docker Images**: Go to **Packages & Registries → Container Registry**
- **Deployments**: Go to **Deployments → Environments**

## Environment-Specific Configuration

The pipeline passes configuration via environment variables. The application reads these from:

1. **appsettings.json** (base config)
2. **appsettings.{ASPNETCORE_ENVIRONMENT}.json** (environment-specific)
3. **Environment variables** (runtime overrides)

Example environment variable mapping:
```
ConnectionStrings__PostgresConnection = ConnectionStrings:PostgresConnection
Jwt__SecretKey = Jwt:SecretKey
Smtp__Host = Smtp:Host
```

## Customization

### Change Image Registry
Edit `.gitlab-ci.yml`:
```yaml
REGISTRY: "your-registry.com"
```

### Change Port Mapping
In deployment jobs, modify:
```yaml
-p 5001:5001     # Change second number to your host port
```

### Add More Environments
1. Create new branch (e.g., `qa`)
2. Add new deployment job in `.gitlab-ci.yml`:
```yaml
deploy:qa:
  stage: deploy-qa
  # ... copy from another environment and customize
```
3. Add QA_* variables in GitLab

## Health Checks

The Docker container includes a health check:
```
GET https://localhost:5001/health
```

This helps Docker determine if the container is running properly.

## Troubleshooting

### Docker login failed
- Verify `CI_REGISTRY_USER` and `CI_REGISTRY_PASSWORD` are correct
- Ensure GitLab runner has Docker daemon access

### Build fails
- Check `.gitlab-ci.yml` syntax: `gitlab-runner lint .gitlab-ci.yml`
- Review build logs in GitLab UI
- Verify all .csproj files exist

### Deployment fails
- Check if all environment variables are set
- Verify Docker image exists in registry
- Check container logs: `docker logs projectnameplaceholder-dev`

### Database connection errors
- Verify `*_DB_CONNECTION_STRING` variable is correct
- Ensure database server is accessible from deployment host
- Check firewall rules

## Security Best Practices

1. **Variables**: Use GitLab Masked Variables for sensitive data
2. **Registry**: Set registry to private
3. **Secrets**: Never commit secrets to repository

## Default Super Admin Account

The bootstrapper seeds a default super admin user for initial access:

```text
Email: superadmin@localhost
Password: SuperAdmin@123!
```

Use this account only for first-time setup and rotate the password after deployment.
4. **SSL/TLS**: Always use HTTPS for production
5. **JWT Secret**: Use a strong, randomly generated secret (32+ characters)

## Maintenance

### Update .NET Version
Edit `.gitlab-ci.yml`:
```yaml
DOTNET_SDK_VERSION: "8.0"      # Change to desired version
DOTNET_ASPNET_VERSION: "8.0"   # Update runtime version too
```

### Update Docker Base Images
Same as above - change version numbers

### Clean Up Old Images
Use the cleanup jobs in `.gitlab-ci.yml` to remove old images from registry
