FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["BigLineconnect.Relay/BigLineconnect.Relay.csproj", "BigLineconnect.Relay/"]
RUN dotnet restore "BigLineconnect.Relay/BigLineconnect.Relay.csproj"

# Copy all source
COPY . .
WORKDIR "/src/BigLineconnect.Relay"
RUN dotnet build "BigLineconnect.Relay.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BigLineconnect.Relay.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV PORT=5080
EXPOSE 5080

ENTRYPOINT ["dotnet", "BigLineconnect.Relay.dll"]
