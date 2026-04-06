FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Inventory.Service/Inventory.Service.csproj", "Inventory.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Inventory.Service/Inventory.Service.csproj"
COPY . .
RUN dotnet build "Inventory.Service/Inventory.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Inventory.Service/Inventory.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Service.dll"]