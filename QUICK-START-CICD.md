# GitLab CI/CD Pipeline - Quick Start Guide

Complete CI/CD pipeline for ProjectNamePlaceholder with Docker support across Development, Staging, and Production environments.

## What Was Created

### 1. **Dockerfile** (Multi-stage build)
   - Build stage: Compiles .NET 8 application
   - Publish stage: Packages application
   - Runtime stage: Minimal ASP.NET image for production

### 2. **.dockerignore**
   - Excludes unnecessary files from Docker build context
   - Reduces image size

### 3. **Environment-Specific Configuration**
   - `appsettings.Staging.json` - Staging environment config
   - `appsettings.Production.json` - Production environment config
   - Uses environment variables for sensitive data

### 4. **.gitlab-ci.yml** (Complete Pipeline)
   - **Build Stage**: Compiles .NET application
   - **Test Stage**: Runs unit tests
   - **Docker Build**: Creates and pushes Docker images
   - **Deploy Stage**: Manual deployment to each environment

### 5. **Configuration Files**
   - `.env.template` - Environment variable template
   - `CI-CD-SETUP.md` - Detailed setup guide
   - `deploy-config.sh` - Configuration validation script
   - `docker-compose.yml` - Local development setup

---

## Quick Setup (5 minutes)

### Step 1: Copy .env.template
```bash
cp .env.template .env.local
# Edit with your actual values
nano .env.local
```

### Step 2: Set GitLab Variables
1. Go to **GitLab → Your Project → Settings → CI/CD → Variables**
2. Add all variables from your `.env.local` file
3. Mark sensitive values as **Masked**

### Step 3: Test Locally with Docker Compose
```bash
# Development environment
docker-compose --profile dev up

# Staging environment
docker-compose --profile staging up

# Production environment
docker-compose --profile prod up
```

### Step 4: Push Code
```bash
git add .
git commit -m "Add CI/CD pipeline"
git push origin dev  # or stage, or main
```

---

## Branch Strategy

| Branch | Purpose | Deploy To | Notes |
|--------|---------|-----------|-------|
| `dev` | Feature development | Development | Manual deploy |
| `stage` | Testing & QA | Staging | Manual deploy |
| `main` | Release ready | Production | Manual deploy (requires approval) |

---

## How to Deploy

### Deploy to Development
```bash
# 1. Push to dev branch
git push origin dev

# 2. In GitLab:
# - Go to CI/CD → Pipelines
# - Wait for build & test to complete
# - Click "play" button on "docker:build:dev" job
# - Click "play" button on "deploy:dev" job
```

### Deploy to Staging
```bash
# 1. Push to stage branch
git push origin stage

# 2. Follow same process as Development
# (but for staging jobs)
```

### Deploy to Production
```bash
# 1. Push to main branch
git push origin main

# 2. Follow same process as Development
# (but for production jobs - requires approval)
```

---

## What Happens in the Pipeline

### Build & Test
1. .NET application is compiled in Release mode
2. Unit tests are executed
3. Build artifacts are created
4. Test results are reported

### Docker Build
1. Docker image is built using multi-stage Dockerfile
2. Image is tagged with environment name and commit SHA
3. Image is pushed to GitLab Container Registry

### Deployment (Manual)
1. Docker image is pulled from registry
2. Environment variables are injected
3. Container is started with proper port mapping
4. Health check is configured

---

## Configuration by Environment

### Development
- Debug logging enabled
- Swagger UI available
- Local services supported
- **URL**: `https://dev-api.yourproject.local`

### Staging
- Information level logging
- Swagger UI disabled
- Production-like environment
- **URL**: `https://staging-api.yourproject.local`

### Production
- Warning level logging
- Swagger UI disabled
- Maximum security & performance
- **URL**: `https://api.yourproject.local`

---

## Environment Variables Reference

All variables use `{ENVIRONMENT}_` prefix (DEV_, STAGING_, PROD_):

```
{ENV}_DB_CONNECTION_STRING      → PostgreSQL connection
{ENV}_JWT_SECRET_KEY            → JWT signing key (min 32 chars)
{ENV}_JWT_ISSUER                → Token issuer claim
{ENV}_JWT_AUDIENCE              → Token audience claim
{ENV}_SMTP_HOST                 → Email server host
{ENV}_SMTP_PORT                 → Email server port
{ENV}_SMTP_USERNAME             → Email server username
{ENV}_SMTP_PASSWORD             → Email server password
{ENV}_SMTP_FROM_ADDRESS         → From email address
```

---

## Troubleshooting

### Build Fails
```bash
# Check pipeline syntax
gitlab-runner lint .gitlab-ci.yml

# View build logs in GitLab UI
# CI/CD → Pipelines → Click job name
```

### Docker Image Not Found
```bash
# Verify image exists in registry
# Packages & Registries → Container Registry

# Or manually check
docker pull registry.gitlab.com/group/projectnameplaceholder:dev-latest
```

### Deployment Fails
```bash
# Check all environment variables are set
./deploy-config.sh dev

# View container logs
docker logs projectnameplaceholder-dev
```

### Database Connection Error
- Verify DB connection string is correct
- Ensure database server is accessible
- Check firewall rules
- Test connection manually

---

## Security Best Practices

✅ **Always:**
- Use strong JWT secrets (32+ characters, random)
- Mark sensitive variables as **Masked** in GitLab
- Keep `.env.local` out of version control
- Use HTTPS for all environments
- Rotate secrets regularly
- Limit registry access to authorized users

❌ **Never:**
- Commit `.env.local` to repository
- Log sensitive data
- Use same secret across environments
- Hardcode passwords/tokens
- Share credentials via chat/email

---

## Customization

### Use Different Registry
Edit `.gitlab-ci.yml`:
```yaml
REGISTRY: "your-registry.com"
```

### Use Different .NET Version
Edit `.gitlab-ci.yml` and `Dockerfile`:
```yaml
DOTNET_SDK_VERSION: "9.0"
DOTNET_ASPNET_VERSION: "9.0"
```

### Add More Environments
1. Create new branch (e.g., `qa`)
2. Duplicate deployment job in `.gitlab-ci.yml`
3. Customize for new environment
4. Add environment variables in GitLab

### Change Deployment Server
Modify deployment jobs in `.gitlab-ci.yml` to use your deployment method (Kubernetes, SSH, etc.)

---

## Next Steps

1. ✅ Copy `.env.template` to `.env.local`
2. ✅ Fill in configuration values
3. ✅ Set variables in GitLab
4. ✅ Test locally with docker-compose
5. ✅ Push code and trigger pipeline
6. ✅ Monitor deployment in GitLab UI

---

## Support

For detailed setup information, see: **CI-CD-SETUP.md**

For validation script help:
```bash
chmod +x deploy-config.sh
./deploy-config.sh dev
./deploy-config.sh staging
./deploy-config.sh prod
```

---

**Happy deploying! 🚀**
