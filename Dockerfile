FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
COPY nuget.config stylecop.json /source/
COPY src/Directory.Build.props src/Packages.props /source/src/
COPY src/AutoCrane/*.csproj /source/src/AutoCrane/
WORKDIR /source/src/AutoCrane
RUN dotnet restore -r linux-musl-x64

COPY src/AutoCrane/. /source/src/AutoCrane
#RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained true --no-restore /p:PublishTrimmed=true /p:PublishReadyToRun=true
#RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained true --no-restore
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
RUN apk add git zstd
WORKDIR /app
COPY --from=build /app ./
COPY --from=build /source/src/AutoCrane/my.runtimeconfig.json ./AutoCrane.runtimeconfig.json
ENTRYPOINT ["dotnet", "./AutoCrane.dll"]