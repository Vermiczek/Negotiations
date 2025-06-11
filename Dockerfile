FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["WebApi/Negotiations.csproj", "WebApi/"]
RUN dotnet restore "WebApi/Negotiations.csproj"
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY . .
WORKDIR "/src/WebApi"
RUN dotnet build "Negotiations.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Negotiations.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
COPY ["WebApi/docker-entrypoint.sh", "."]
# Install PostgreSQL client and other necessary tools
RUN apt-get update && \
    apt-get install -y postgresql-client curl jq && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    chmod +x ./docker-entrypoint.sh
ENV PATH="${PATH}:/root/.dotnet/tools"
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS="http://+:8080"
# Use shell form of ENTRYPOINT to ensure proper script execution
ENTRYPOINT ["/bin/bash", "./docker-entrypoint.sh"]
