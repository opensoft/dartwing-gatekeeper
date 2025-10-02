FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

# Install additional tools
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
    git \
    curl \
    wget \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Set up non-root user
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# Install global .NET tools
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-format

# Add .NET tools to PATH
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"
