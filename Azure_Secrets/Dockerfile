FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Azure_Secrets/Azure_Secrets.csproj", "Azure_Secrets/"]
RUN dotnet restore "Azure_Secrets/Azure_Secrets.csproj"
COPY . .
WORKDIR "/src/Azure_Secrets"
RUN dotnet build "Azure_Secrets.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Azure_Secrets.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Azure_Secrets.dll"]
