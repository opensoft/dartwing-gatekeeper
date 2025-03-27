FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DartWing.Web/DartWing.Web.csproj", "DartWing.Web/"]
COPY ["DartWing.ErpNext/DartWing.ErpNext.csproj", "DartWing.ErpNext/"]
COPY ["DartWing.KeyCloak/DartWing.KeyCloak.csproj", "DartWing.KeyCloak/"]
COPY ["DartWing.Google/DartWing.Google.csproj", "DartWing.Google/"]
COPY ["DartWing.Microsoft/DartWing.Microsoft.csproj", "DartWing.Microsoft/"]
RUN dotnet restore "DartWing.Web/DartWing.Web.csproj"
COPY . .
WORKDIR "/src/DartWing.Web"
RUN dotnet build "DartWing.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DartWing.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DartWing.Web.dll"]
