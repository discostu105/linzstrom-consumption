#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app


#####################
#PUPPETEER RECIPE
#####################
# Install latest chrome dev package and fonts to support major charsets (Chinese, Japanese, Arabic, Hebrew, Thai and a few others)
# Note: this installs the necessary libs to make the bundled version of Chromium that Puppeteer
# installs, work.
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils

RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-unstable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
      --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD true

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-unstable"
#####################
#END PUPPETEER RECIPE
#####################



FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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