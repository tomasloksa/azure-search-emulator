#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
COPY ./cert/ ../srv/cert
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV SEARCH_URL=http://solr:8983/solr/
ENV Logging__Console__FormatterName=Simple
#For debugging requests
#ENV http_proxy "http://host.docker.internal:8888/"
#ENV https_proxy "https://host.docker.internal:8888/"

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SearchQueryService.csproj", "SearchQueryService/"]
RUN dotnet restore "SearchQueryService/SearchQueryService.csproj"
COPY . SearchQueryService/
WORKDIR /src/SearchQueryService/
RUN dotnet build "SearchQueryService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SearchQueryService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SearchQueryService.dll"]
