# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY . .
RUN dotnet restore

# copy everything else and build app
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN apt-get update
RUN apt-get install -y python3 python3-pip
RUN pip3 install broadlink --break-system-packages

WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "HueControlServer.dll"]
