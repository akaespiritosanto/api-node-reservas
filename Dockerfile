FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["api-node-reservas.csproj", "."]
RUN dotnet restore "api-node-reservas.csproj"
COPY . .
RUN dotnet publish "api-node-reservas.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "api-node-reservas.dll"]
