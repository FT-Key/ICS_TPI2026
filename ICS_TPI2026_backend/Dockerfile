# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Dsw2025Tpi.sln ./
COPY Dsw2025Tpi.Api/*.csproj Dsw2025Tpi.Api/
COPY Dsw2025Tpi.Application/*.csproj Dsw2025Tpi.Application/
COPY Dsw2025Tpi.Domain/*.csproj Dsw2025Tpi.Domain/
COPY Dsw2025Tpi.Data/*.csproj Dsw2025Tpi.Data/
RUN dotnet restore
COPY . .
RUN dotnet publish Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]
