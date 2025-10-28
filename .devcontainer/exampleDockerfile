# context is the root of the project
# /projects/nopCommerce
# this devcontainer dockerfile in the ./.devcontainer folder

# create the build instance 
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
USER root
RUN dotnet workload update && \
    apt-get update && \
    apt-get install -y \
        sudo \
        openssh-client \
        rsync && rm -rf /var/lib/apt/lists/* \
        zsh
WORKDIR /workspace
# Ensure the .zshrc file exists in the build context before uncommenting the next line
# COPY .zshrc /root/.zshrc

# Accept build arguments for username, UID, and GID from docker-compose
ARG IDE_USER
ARG IDE_UID
ARG IDE_GID

# Accept a debug argument for conditional logging during build
ARG DEBUG=false

# Set DEBUG as an environment variable for runtime
ENV DEBUG=${DEBUG}

# in debug mode, show these values
RUN if [ "$DEBUG" = "true" ]; then \
        echo "the passed arg IDE-USER= ${IDE_USER}"; \
        echo "the passed arg IDE-UID= ${IDE_UID}"; \
        echo "the passed arg IDE-GID= ${IDE_GID}"; \
        echo "the passed env DEBUG= ${DEBUG}"; \
    fi


    
# Remove container image user and group if they conflict with the remoteUser from docker-compose/devcontainer.json
# This is important to avoid conflicts with the user and group IDs
# Step 2: Delete user (if exists)
RUN if grep -q ":${IDE_UID}:" /etc/passwd; then \
      userdel --force -r $(grep ":${IDE_UID}:" /etc/passwd | cut -d: -f1); \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "User with UID ${IDE_UID} deleted if it existed."; \
    fi && \
    # Delete group if it matches IDE_GID
    if grep -q ":${IDE_GID}:" /etc/group; then \
      groupdel $(grep ":${IDE_GID}:" /etc/group | cut -d: -f1); \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Group with GID ${IDE_GID} deleted if it existed."; \
    fi



# Create the docker-compose/devcontainer.json user and add to the sudo group
# only echo out the status if in debug mode
RUN set -eux && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Creating group if it does not exist..."; \
    fi && \
    if ! getent group "${IDE_GID}" >/dev/null; then \
        groupadd --gid "${IDE_GID}" "${IDE_USER}" || (echo "Error: Failed to create group with GID ${IDE_GID}" && exit 1); \
    else \
        if [ "$DEBUG" = "true" ]; then \
            echo "Group with GID ${IDE_GID} already exists."; \
        fi; \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Creating user if it does not exist..."; \
    fi && \
    if ! id -u "${IDE_USER}" >/dev/null 2>&1; then \
        useradd --uid "${IDE_UID}" --gid "${IDE_GID}" -m "${IDE_USER}" || (echo "Error: Failed to create user ${IDE_USER} with UID ${IDE_UID} and GID ${IDE_GID}" && exit 1); \
    else \
        if [ "$DEBUG" = "true" ]; then \
            echo "User ${IDE_USER} already exists."; \
        fi; \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Adding user to sudoers..."; \
    fi && \
    SUDO_FILE="/etc/sudoers.d/${IDE_USER}" && \
    SUDO_LINE="${IDE_USER} ALL=(ALL) NOPASSWD:ALL" && \
    touch "${SUDO_FILE}" && \
    grep -qF -- "${SUDO_LINE}" "${SUDO_FILE}" || echo "${SUDO_LINE}" >> "${SUDO_FILE}" && \
    chmod 0440 "${SUDO_FILE}" && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Verifying user creation..."; \
    fi && \
    getent passwd "${IDE_USER}" || (echo "Error: User ${IDE_USER} was not created!" && exit 1)
   
WORKDIR /src
COPY ["./src/Directory.Build.props", "."]
COPY ["./src/NopCommerce.sln", "."]
COPY ["./src/Presentation/Nop.Web/Nop.Web.csproj", "Presentation/Nop.Web/"]
COPY ["./src/Presentation/Nop.Web.Framework/Nop.Web.Framework.csproj", "Presentation/Nop.Web.Framework/"]
COPY ["./src/Libraries/Nop.Core/Nop.Core.csproj", "Libraries/Nop.Core/"]
COPY ["./src/Libraries/Nop.Data/Nop.Data.csproj", "Libraries/Nop.Data/"]
COPY ["./src/Libraries/Nop.Services/Nop.Services.csproj", "Libraries/Nop.Services/"]

# in debug mode, show the current working directory and its contents
RUN if [ "$DEBUG" = "true" ]; then \
        echo "Current WORKDIR is:"; \
        pwd; \
        echo "Subdirectories under WORKDIR:"; \
        ls -l; \
    fi

# we do this twice!!!! this is important to make subsequent re-builds faster
# The source of the core Nop project does not often change. So this layer will be cached and reused.
# It prepares the project for building
# The --disable-parallel option is used to avoid issues with parallel restores
RUN dotnet restore Presentation/Nop.Web/Nop.Web.csproj --disable-parallel && \
    dotnet clean Presentation/Nop.Web/Nop.Web.csproj

# Copy the solution and project files as root
WORKDIR /workspace
COPY . .
# Ensure ./bin does not exist, because we will create it as $IDE_USER and copy the contents of ./src to ./bin as $IDE_USER
USER root
RUN rm -rf ./bin 

# in debug mode, show the current working directory and its contents
RUN if [ "$DEBUG" = "true" ]; then \
        echo "Current WORKDIR is:"; \
        pwd; \
        echo "Subdirectories under WORKDIR:"; \
        ls -l; \
    fi

RUN dotnet restore src/NopCommerce.sln --disable-parallel && \
dotnet clean src/NopCommerce.sln

# Ensure the IDE_USER has ownership of the workspace directory - dont know if this works. copilot says it cannot be done
# only echo out the status if in debug mode
RUN set -eux && \
    echo "Fixing ownership and permissions for /workspace..." && \
    chown -R ${IDE_USER}:${IDE_USER} /workspace && \
    if [ $? -eq 0 ]; then \
        echo "Ownership successfully changed for /workspace to ${IDE_USER}:${IDE_USER}"; \
    else \
        echo "Failed to change ownership for /workspace"; \
    fi && \
    chmod -R u+rw /workspace

# Switch to the non-root user running vscode
# This is important for security and to avoid permission issues
# The user should have the same UID and GID as the host user
# This allows the container to access files on the host without permission issues
USER ${IDE_USER}
# Build the solution in Debug mode
# The --no-incremental option is used to force a full rebuild
# This is useful when you want to ensure that all projects are built from scratch
# The -c Debug option specifies the configuration to use (Debug or Release)
RUN dotnet build /workspace/src/NopCommerce.sln --no-incremental -c Debug

# Build the projects separately to avoid dependency issues (only if needed for debugging)
# Note: The --no-incremental flag is used to force a full rebuild
# This is useful when you want to ensure that all projects are built from scratch
# Build the projects in the correct order to avoid dependency issues
# leave commented out until needed for debugging
#RUN dotnet build ./src/Presentation/Nop.Web.Framework/Themes/Nop.Web.Framework.Themes.csproj --no-incremental -c Debug -o ./bin/Presentation/Nop.Web.Framework.Themes
#RUN do/net build ./sr//Presentation//op.Web.Framewo/k/Themes/Nop.W/b.Framework.Th/mes.Libraries/Nop.Web.Framework.Themes.Libraries.csproj --no-incremental -c Debug -o ./bin/Presentation/Nop.Web.Framework.Themes/Nop.Web.Framework.Themes.Libraries
#RUN dotnet build ./src/Presentation/Nop.Web.Framework/Nop.Web.Framework.csproj --no-incremental -c Debug -o ./bin/Presentation/Nop.Web.Framework
#RUN dotnet build ./src/Presentation/Nop.Web/Nop.Web.csproj --no-incremental -c Debug -o ./bin/Presentation/Nop.Web
#RUN dotnet build ./src/Libraries/Nop.Services/Nop.Services.csproj --no-incremental -c Debug -o ./bin/Libraries/Nop.Services
#RUN dotnet build ./src/Libraries/Nop.Core/Nop.Core.csproj --no-incremental -c Debug -o ./bin/Libraries/Nop.Core
#RUN dotnet build ./src/Libraries/Nop.Data/Nop.Data.csproj --no-incremental -c Debug -o ./bin/Libraries/Nop.Data

# Copy the contents of ./src to ./bin as the non-root user
# This is important to avoid permission issues when removing this folder using the entrypoint script which runs under the IDE_USER
USER ${IDE_USER}
RUN mkdir ./bin && cp -r ./src/* ./bin/
CMD ["./.devcontainer/containers/Nop.Web/entrypoint.sh"]