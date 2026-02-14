FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install useful tools
RUN apt-get update && apt-get install -y procps lsof && rm -rf /var/lib/apt/lists/*

# Install EF Core tools globally
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /workspace
