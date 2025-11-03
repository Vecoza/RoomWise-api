# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5184
ENV ASPNETCORE_URLS=http://+:5184
ENV ASPNETCORE_ENVIRONMENT=Production

# Build & publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copy csproj first for better cache
COPY RoomWise.Api/RoomWise.Api.csproj RoomWise.Api/
RUN dotnet restore RoomWise.Api/RoomWise.Api.csproj

# 2) Copy remaining source
COPY . .

# 3) Publish
WORKDIR /src/RoomWise.Api
RUN dotnet publish RoomWise.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RoomWise.Api.dll"]
