FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shipping.Service/Shipping.Service.csproj", "Shipping.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Shipping.Service/Shipping.Service.csproj"
COPY . .
RUN dotnet build "Shipping.Service/Shipping.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Shipping.Service/Shipping.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Shipping.Service.dll"]