FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1

ARG BUILD_NUMBER
ARG BUILD_DATE

RUN apt-get update && \
    apt-get install -y curl sudo dos2unix jq apt-utils wget apt-transport-https ca-certificates curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Download the Microsoft repository GPG keys
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb

# Register the Microsoft repository GPG keys, update and install PowerShell
RUN sudo dpkg -i packages-microsoft-prod.deb
RUN sudo apt-get update
RUN sudo apt-get install -y powershell

# Install az modules
RUN pwsh -Command "& {Install-Module az -Scope AllUsers -Force}" > /tmp/azModuleInstall.log
RUN pwsh -Command "& {Install-Module Az.App -Scope AllUsers -Force}" > /tmp/azAppModuleInstall.log

# Install Az cli
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Allow dynamic installation of Az extensions
RUN az config set extension.use_dynamic_install=yes_without_prompt

# Download the Google Cloud public signing key
RUN curl -sS https://packages.cloud.google.com/apt/doc/apt-key.gpg | gpg --dearmor | sudo tee /etc/apt/trusted.gpg.d/kubernetes-archive-keyring.gpg

# Add the Kubernetes apt repository, update and install kubectl
RUN echo "deb [signed-by=/etc/apt/trusted.gpg.d/kubernetes-archive-keyring.gpg] https://apt.kubernetes.io/ kubernetes-xenial main" | sudo tee /etc/apt/sources.list.d/kubernetes.list
RUN sudo apt-get update
RUN sudo apt-get install -y kubectl

# Download the Helm repository public signing key and add the Helm repository
RUN curl https://baltocdn.com/helm/signing.asc | gpg --dearmor | sudo tee /usr/share/keyrings/helm.gpg > /dev/null
RUN echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/helm.gpg] https://baltocdn.com/helm/stable/debian/ all main" | sudo tee /etc/apt/sources.list.d/helm-stable-debian.list
RUN sudo apt-get update

# Install Helm
RUN sudo apt-get install helm

# Install Postgres client tools
RUN echo "deb http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" >>  /etc/apt/sources.list.d/pgdg.list
RUN sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
RUN wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
RUN sudo apt-get update
RUN sudo apt-get install -y postgresql-client

# Install AzCopy
# Download and extract
RUN wget https://aka.ms/downloadazcopy-v10-linux
RUN tar -xvf downloadazcopy-v10-linux
# Move AzCopy
RUN sudo rm -f /usr/bin/azcopy
RUN sudo cp ./azcopy_linux_amd64_*/azcopy /usr/bin/
RUN sudo chmod 755 /usr/bin/azcopy
# Clean the kitchen
RUN rm -f downloadazcopy-v10-linux
RUN rm -rf ./azcopy_linux_amd64_*/

# end of software installation

EXPOSE 10933

WORKDIR /tmp

COPY docker/linux/install-scripts/* /install-scripts/
RUN chmod +x /install-scripts/*.sh

COPY docker/linux/scripts/* /scripts/
RUN chmod +x /scripts/*.sh

# Install Docker daemon and CLI
COPY docker/linux/scripts/dockerd-entrypoint.sh /usr/local/bin/
RUN /install-scripts/install-docker.sh

# Install Tentacle
COPY _artifacts/deb/tentacle_${BUILD_NUMBER}_amd64.deb /tmp/
RUN apt-get update && \
    apt install ./tentacle_${BUILD_NUMBER}_amd64.deb && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /

# We know this won't reduce the image size at all. It's just to make the filesystem a little tidier.
RUN rm -rf /tmp/*

ENV OCTOPUS_RUNNING_IN_CONTAINER=Y
ENV ACCEPT_EULA=N
ENV CustomPublicHostName=""
ENV DISABLE_DIND=N
ENV ListeningPort=""
ENV MachinePolicy="Default Machine Policy"
ENV PublicHostNameConfiguration="ComputerName"
ENV ServerApiKey=""
ENV ServerPassword=""
ENV ServerPort=""
ENV ServerUrl=""
ENV ServerUsername=""
ENV Space="Default"
ENV TargetEnvironment=""
ENV TargetName=""
ENV TargetRole=""
ENV TargetTenant=""
ENV TargetTenantTag=""
ENV TargetTenantedDeploymentParticipation=""
ENV TargetWorkerPool=""

VOLUME /var/lib/docker

CMD /scripts/configure-tentacle.sh && /scripts/run-tentacle.sh

LABEL \
    org.label-schema.schema-version="1.0" \
    org.label-schema.name="Octopus Deploy Tentacle" \
    org.label-schema.vendor="Octopus Deploy" \
    org.label-schema.url="https://octopus.com" \
    org.label-schema.vcs-url="https://github.com/OctopusDeploy/OctopusTentacle" \
    org.label-schema.license="Apache"  \
    org.label-schema.description="Octopus Tentacle instance with auto-registration to Octopus Server" \
    org.label-schema.version=${BUILD_NUMBER} \
    org.label-schema.build-date=${BUILD_DATE}
