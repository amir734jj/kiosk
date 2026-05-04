FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY . .

RUN dotnet restore &&  \
    dotnet publish UI/UI.csproj -c Release -o /publish/ui --no-restore && \
    dotnet publish Api/Api.csproj -c Release -o /publish/api --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=build /publish/api .
COPY --from=build /publish/ui/wwwroot ./wwwroot

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=America/Chicago

RUN apt-get update && \
    apt-get install -y --no-install-recommends tzdata

ENTRYPOINT ["dotnet", "Api.dll"]
