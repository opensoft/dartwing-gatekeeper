# DevContainer User Configuration Guide

This guide explains how to set up the DartWing Gateway devcontainer for team development.

## Overview

The devcontainer is configured to:
- **Match your host user** - Files created in the container will have the correct ownership on your host system
- **Use your personal configuration** - Git config, SSH keys, and shell settings are mounted from your host
- **Share infrastructure** - Connects to shared Docker networks for service communication

## Quick Start

### 1. Create Your Personal .env File

Copy the example template to create your personal configuration:

```bash
cd .devcontainer
cp .env.example .env
```

The `.env` file is gitignored and will not be committed to the repository.

### 2. Verify User Settings (Optional)

The template uses shell commands to auto-detect your user settings:
- `USER_NAME=$(whoami)` - Your username
- `USER_UID=$(id -u)` - Your user ID (usually 1000)
- `USER_GID=$(id -g)` - Your group ID (usually 1000)

These values work automatically on Linux and WSL. If you encounter issues, you can manually set them:

```bash
# Check your values
id

# Example output:
# uid=1000(brett) gid=1000(brett) groups=1000(brett),27(sudo),999(docker)
```

Then update `.env` with explicit values:
```env
USER_NAME=brett
USER_UID=1000
USER_GID=1000
```

### 3. Open in VS Code

Open the project in VS Code and click "Reopen in Container" when prompted, or use:

```
Ctrl+Shift+P → "Dev Containers: Reopen in Container"
```

## Configuration Details

### Project Configuration

```env
PROJECT_NAME=dartwing-gateway          # Unique container identifier
COMPOSE_PROJECT_NAME=dartwingers       # Groups related containers
```

**Important**: `PROJECT_NAME` must be unique across all your projects to avoid container name conflicts.

### User Configuration

```env
USER_NAME=$(whoami)     # Container username matches your host user
USER_UID=$(id -u)       # File permission UID
USER_GID=$(id -g)       # File permission GID
```

**Why this matters**: Matching UIDs/GIDs ensures files created in the container have the correct ownership on your host system, preventing permission issues.

### .NET Configuration

```env
DOTNET_VERSION=8.0      # .NET SDK version
```

### Gateway Service Ports

```env
GATEWAY_HTTP_PORT=5000          # HTTP endpoint
GATEWAY_HTTPS_PORT=5001         # HTTPS endpoint
GATEWAY_MANAGEMENT_PORT=5002    # Health/management API
```

These ports are exposed to your host system for testing and debugging.

### Network Configuration

```env
NETWORK_NAME=dartnet    # Shared Docker network for service communication
```

The container connects to a shared `dartnet` network, allowing communication with other Dartwingers services (like dartwing-app, databases, etc.).

## Team Best Practices

### DO ✅
- Copy `.env.example` to `.env` for your personal config
- Keep your `.env` file private (it's gitignored)
- Update `.env.example` when adding new configuration options (so teammates know about them)
- Commit changes to `Dockerfile`, `docker-compose.yml`, and `devcontainer.json`

### DON'T ❌
- Never commit your personal `.env` file
- Don't hardcode personal paths or credentials in Docker files
- Don't modify the devcontainer while running - rebuild instead

## Troubleshooting

### Permission Issues

If you encounter permission errors:

1. Verify your `.env` settings match your host user:
   ```bash
   id
   ```

2. Rebuild the container:
   ```
   Ctrl+Shift+P → "Dev Containers: Rebuild Container"
   ```

### Container Name Conflicts

If you get "container name already in use" errors:

```bash
docker rm -f dartwing-gateway
```

Then reopen in VS Code.

### File Ownership Issues

If files created in the container have wrong ownership on the host:

1. Check the container user matches your host user:
   ```bash
   # Inside container
   id
   
   # On host
   id
   ```

2. Ensure `USER_UID` and `USER_GID` in `.env` match your host values
3. Rebuild the container

## Advanced Configuration

### Customizing Container Resources

You can adjust memory and CPU limits in `.env`:

```env
CONTAINER_MEMORY=4g     # 4 gigabytes
CONTAINER_CPUS=2        # 2 CPU cores
```

### Debug Mode

Enable verbose logging during container build:

```env
DEBUG_MODE=true
```

This shows detailed information about user creation and configuration during the build process.

### Custom API Configuration

Point to different backend services:

```env
API_BASE_URL=http://localhost:5000    # Local development
API_TIMEOUT=60                        # Longer timeout for debugging
```

## Support

For issues or questions:
1. Check this documentation
2. Review `.env.example` for configuration options
3. Ask in the team channel
4. Check Docker logs: `docker logs dartwing-gateway`

## File Checklist

Ensure these files exist:
- ✅ `.devcontainer/.env` - Your personal config (gitignored)
- ✅ `.devcontainer/.env.example` - Team template (committed to git)
- ✅ `.devcontainer/Dockerfile` - Container definition
- ✅ `.devcontainer/docker-compose.yml` - Service orchestration
- ✅ `.devcontainer/devcontainer.json` - VS Code configuration
- ✅ `.gitignore` - Should include `.devcontainer/.env`
