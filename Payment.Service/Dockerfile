FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Payment.Service/Payment.Service.csproj", "Payment.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Payment.Service/Payment.Service.csproj"
COPY . .
RUN dotnet build "Payment.Service/Payment.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.Service/Payment.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.Service.dll"]