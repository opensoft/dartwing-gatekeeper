FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-noble

USER root

# Install Node.js (needed for Claude Code and Codex CLI)
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y nodejs

# Install additional tools
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
    git \
    curl \
    wget \
    sudo \
    python3 \
    python3-pip \
    ffmpeg \
    zsh \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/* \
    && wget -O /usr/local/bin/hadolint https://github.com/hadolint/hadolint/releases/latest/download/hadolint-Linux-x86_64 \
    && chmod +x /usr/local/bin/hadolint \
    && wget -qO- https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-unknown-linux-gnu.tar.gz | tar -xzf - -C /usr/local/bin --strip-components=1 uv-x86_64-unknown-linux-gnu/uv uv-x86_64-unknown-linux-gnu/uvx \
    && chmod +x /usr/local/bin/uv /usr/local/bin/uvx

# Install Claude Code and OpenAI CLI globally
RUN npm install -g @anthropic-ai/claude-code openai @openai/codex

# Accept build arguments for username, UID, and GID from docker-compose
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# Accept a debug argument for conditional logging during build
ARG DEBUG=false

# Set DEBUG as an environment variable for runtime
ENV DEBUG=${DEBUG}

# In debug mode, show these values
RUN if [ "$DEBUG" = "true" ]; then \
        echo "the passed arg USERNAME= ${USERNAME}"; \
        echo "the passed arg USER_UID= ${USER_UID}"; \
        echo "the passed arg USER_GID= ${USER_GID}"; \
        echo "the passed env DEBUG= ${DEBUG}"; \
    fi

# Remove container image user and group if they conflict with the host user
# This is important to avoid conflicts with the user and group IDs
# Step 1: Delete user (if exists)
RUN if grep -q ":${USER_UID}:" /etc/passwd; then \
      userdel --force -r $(grep ":${USER_UID}:" /etc/passwd | cut -d: -f1); \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "User with UID ${USER_UID} deleted if it existed."; \
    fi && \
    # Delete group if it matches USER_GID
    if grep -q ":${USER_GID}:" /etc/group; then \
      groupdel $(grep ":${USER_GID}:" /etc/group | cut -d: -f1); \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Group with GID ${USER_GID} deleted if it existed."; \
    fi

# Create the user and group to match the host user
# Only echo out the status if in debug mode
RUN set -eux && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Creating group if it does not exist..."; \
    fi && \
    if ! getent group "${USER_GID}" >/dev/null; then \
        groupadd --gid "${USER_GID}" "${USERNAME}" || (echo "Error: Failed to create group with GID ${USER_GID}" && exit 1); \
    else \
        if [ "$DEBUG" = "true" ]; then \
            echo "Group with GID ${USER_GID} already exists."; \
        fi; \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Creating user if it does not exist..."; \
    fi && \
    if ! id -u "${USERNAME}" >/dev/null 2>&1; then \
        useradd --uid "${USER_UID}" --gid "${USER_GID}" -m "${USERNAME}" || (echo "Error: Failed to create user ${USERNAME} with UID ${USER_UID} and GID ${USER_GID}" && exit 1); \
    else \
        if [ "$DEBUG" = "true" ]; then \
            echo "User ${USERNAME} already exists."; \
        fi; \
    fi && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Adding user to sudoers..."; \
    fi && \
    SUDO_FILE="/etc/sudoers.d/${USERNAME}" && \
    SUDO_LINE="${USERNAME} ALL=(ALL) NOPASSWD:ALL" && \
    touch "${SUDO_FILE}" && \
    grep -qF -- "${SUDO_LINE}" "${SUDO_FILE}" || echo "${SUDO_LINE}" >> "${SUDO_FILE}" && \
    chmod 0440 "${SUDO_FILE}" && \
    if [ "$DEBUG" = "true" ]; then \
        echo "Verifying user creation..."; \
    fi && \
    getent passwd "${USERNAME}" || (echo "Error: User ${USERNAME} was not created!" && exit 1)

# Install global .NET tools as the non-root user
USER ${USERNAME}
RUN dotnet tool install --global dotnet-format

# Add .NET tools to PATH
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"

# Switch back to root for any final setup, container will run as USERNAME by default
USER root

# Keep container running for dev container
CMD ["sleep", "infinity"]
