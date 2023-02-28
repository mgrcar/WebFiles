FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WebFiles/WebFiles.csproj", "WebFiles/"]
RUN dotnet restore "WebFiles/WebFiles.csproj"
COPY . .
WORKDIR "/src/WebFiles"
RUN dotnet build "WebFiles.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebFiles.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebFiles.dll"]
