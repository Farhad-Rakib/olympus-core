# Redis Deployment Setup

This project can use Redis as the distributed cache backend for permissions, menus, roles, and other cache-backed data.

## Configure the app

Set these values in your environment or deployment config:

```json
{
  "Caching": {
    "UseRedis": true,
    "RedisInstanceName": "olympus:"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379"
  }
}
```

Recommended environment variables:

```bash
Caching__UseRedis=true
Caching__RedisInstanceName=olympus:
ConnectionStrings__Redis=redis:6379
```

## Docker Compose example

Add a Redis service alongside the API:

```yaml
services:
  redis:
    image: redis:7-alpine
    restart: unless-stopped
    command: ["redis-server", "--appendonly", "yes"]
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

Point `ConnectionStrings__Redis` at the Redis service name when the API runs in the same compose network.

## Operational notes

- The API stores cache entries with the `olympus:` prefix by default.
- The Redis admin endpoints are protected by permissions:
  - `system.cache.read`
  - `system.cache.flush`
- Use the read endpoint to inspect cached keys and values before flushing.
- The flush endpoint removes only keys with the configured instance prefix, not the whole Redis database.

## Suggested deployment checklist

1. Deploy Redis first and verify the service is reachable.
2. Set `Caching:UseRedis=true` in the API environment.
3. Set `ConnectionStrings:Redis` to the deployed Redis host and port.
4. Confirm the app starts with Redis enabled.
5. Use the system cache endpoints to verify keys are being written.