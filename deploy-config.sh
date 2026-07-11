#!/bin/bash

# Deployment Configuration Helper Script
# Usage: ./deploy-config.sh [dev|staging|prod]

set -e

ENVIRONMENT=${1:-dev}
COLORS_RED='\033[0;31m'
COLORS_GREEN='\033[0;32m'
COLORS_YELLOW='\033[1;33m'
COLORS_NC='\033[0m' # No Color

echo -e "${COLORS_YELLOW}ProjectNamePlaceholder - Deployment Configuration${COLORS_NC}"
echo "=================================================="
echo ""

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    echo -e "${COLORS_RED}Error: Invalid environment. Use: dev, staging, or prod${COLORS_NC}"
    exit 1
fi

# Set environment variables based on argument
case $ENVIRONMENT in
    dev)
        ENV_NAME="Development"
        ENV_PREFIX="DEV"
        ENV_URL="https://dev-api.yourproject.local"
        ;;
    staging)
        ENV_NAME="Staging"
        ENV_PREFIX="STAGING"
        ENV_URL="https://staging-api.yourproject.local"
        ;;
    prod)
        ENV_NAME="Production"
        ENV_PREFIX="PROD"
        ENV_URL="https://api.yourproject.local"
        ;;
esac

echo -e "Environment: ${COLORS_GREEN}${ENV_NAME}${COLORS_NC}"
echo -e "URL: ${COLORS_GREEN}${ENV_URL}${COLORS_NC}"
echo ""

# Check required environment variables
echo "Checking required environment variables..."
echo ""

REQUIRED_VARS=(
    "${ENV_PREFIX}_DB_CONNECTION_STRING"
    "${ENV_PREFIX}_JWT_SECRET_KEY"
    "${ENV_PREFIX}_JWT_ISSUER"
    "${ENV_PREFIX}_JWT_AUDIENCE"
    "${ENV_PREFIX}_SMTP_HOST"
    "${ENV_PREFIX}_SMTP_PORT"
    "${ENV_PREFIX}_SMTP_USERNAME"
    "${ENV_PREFIX}_SMTP_PASSWORD"
    "${ENV_PREFIX}_SMTP_FROM_ADDRESS"
)

MISSING_VARS=0

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${COLORS_RED}✗${COLORS_NC} Missing: $var"
        MISSING_VARS=$((MISSING_VARS + 1))
    else
        # Mask sensitive values
        if [[ "$var" == *"_PASSWORD" ]] || [[ "$var" == *"_SECRET" ]]; then
            echo -e "${COLORS_GREEN}✓${COLORS_NC} $var (set)"
        else
            echo -e "${COLORS_GREEN}✓${COLORS_NC} $var = ${!var}"
        fi
    fi
done

echo ""

if [ $MISSING_VARS -gt 0 ]; then
    echo -e "${COLORS_RED}Error: $MISSING_VARS required variable(s) not set${COLORS_NC}"
    echo ""
    echo "Please set the following variables in GitLab Settings → CI/CD → Variables:"
    for var in "${REQUIRED_VARS[@]}"; do
        echo "  - $var"
    done
    exit 1
fi

echo -e "${COLORS_GREEN}All required variables are configured!${COLORS_NC}"
echo ""

# Display configuration summary
echo "Configuration Summary:"
echo "====================="
echo "Database: ${!ENV_PREFIX}_DB_CONNECTION_STRING (set)"
echo "JWT Issuer: ${!ENV_PREFIX}_JWT_ISSUER"
echo "JWT Audience: ${!ENV_PREFIX}_JWT_AUDIENCE"
echo "SMTP Host: ${!ENV_PREFIX}_SMTP_HOST"
echo "SMTP Port: ${!ENV_PREFIX}_SMTP_PORT"
echo ""

# Suggest next steps
echo -e "${COLORS_YELLOW}Next Steps:${COLORS_NC}"
case $ENVIRONMENT in
    dev)
        echo "1. Push changes to the 'dev' branch"
        echo "2. GitLab pipeline will automatically build and test"
        echo "3. Go to GitLab CI/CD → Pipelines"
        echo "4. Click 'play' button on 'deploy:dev' job to deploy"
        ;;
    staging)
        echo "1. Push changes to the 'stage' branch"
        echo "2. GitLab pipeline will automatically build and test"
        echo "3. Go to GitLab CI/CD → Pipelines"
        echo "4. Click 'play' button on 'deploy:staging' job to deploy"
        ;;
    prod)
        echo "1. Push changes to the 'main' branch"
        echo "2. GitLab pipeline will automatically build and test"
        echo "3. Go to GitLab CI/CD → Pipelines"
        echo "4. Click 'play' button on 'deploy:production' job to deploy (requires approval)"
        ;;
esac

echo ""
echo -e "${COLORS_GREEN}Configuration check complete!${COLORS_NC}"
