FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

################################################################################
################################################################################


RUN  apt-get update \
    && apt-get -yqq install ssh

################################################################################
################################################################################

ARG DEBIAN_FRONTEND=noninteractive

ENV pip_packages "ansible cryptography"

# Install dependencies.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       sudo systemd systemd-sysv \
       build-essential wget libffi-dev libssl-dev procps \
       python3-pip python3-dev python3-setuptools python3-wheel python3-apt \
       iproute2 \
    && rm -rf /var/lib/apt/lists/* \
    && rm -Rf /usr/share/doc && rm -Rf /usr/share/man \
    && apt-get clean

# Upgrade pip to latest version.
RUN pip3 install --upgrade pip

# Install Ansible via pip.
RUN pip3 install $pip_packages

COPY initctl_faker .
RUN chmod +x initctl_faker && rm -fr /sbin/initctl && ln -s /initctl_faker /sbin/initctl

# Install Ansible inventory file.
RUN mkdir -p /etc/ansible
RUN echo "[local]\nlocalhost ansible_connection=local" > /etc/ansible/hosts
RUN echo "[defaults]\nhost_key_checking = False\nprivate_key_file=/root/.ssh/id_rsa" > /etc/ansible/ansible.cfg

# Make sure systemd doesn't start agettys on tty[1-6].
RUN rm -f /lib/systemd/system/multi-user.target.wants/getty.target

VOLUME ["/sys/fs/cgroup"]

################################################################################
################################################################################
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["kiop.csproj", "."]
RUN dotnet restore "./kiop.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "kiop.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "kiop.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kiop.dll"]