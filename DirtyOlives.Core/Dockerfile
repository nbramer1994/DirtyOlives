# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore as distinct layers
COPY DirtyOlives.sln ./
COPY DirtyOlives/DirtyOlives.csproj DirtyOlives/
COPY DirtyOlives.Client/DirtyOlives.Client.csproj DirtyOlives.Client/
COPY DirtyOlives.Core/DirtyOlives.Core.csproj DirtyOlives.Core/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /src/DirtyOlives
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DirtyOlives.dll"]