FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore ./PaymentGateway.Api/PaymentGateway.Api.csproj
RUN dotnet publish ./PaymentGateway.Api/PaymentGateway.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081

ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]