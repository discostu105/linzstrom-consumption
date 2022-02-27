#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LinzNetzConsumptionToInfluxDb/LinzNetzConsumptionToInfluxDb.csproj", "LinzNetzConsumptionToInfluxDb/"]
COPY ["LinzNetzApi/LinzNetzApi.csproj", "LinzNetzApi/"]
COPY ["InfluxDbUtil/InfluxDbUtil.csproj", "InfluxDbUtil/"]
COPY ["LinzNetzCsvUtil/LinzNetzCsvUtil.csproj", "LinzNetzCsvUtil/"]
RUN dotnet restore "LinzNetzConsumptionToInfluxDb/LinzNetzConsumptionToInfluxDb.csproj"
COPY . .
WORKDIR "/src/LinzNetzConsumptionToInfluxDb"
RUN dotnet build "LinzNetzConsumptionToInfluxDb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LinzNetzConsumptionToInfluxDb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LinzNetzConsumptionToInfluxDb.dll"]