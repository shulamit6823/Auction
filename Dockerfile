# Builds the whole site into one container: the React app + the .NET API that also serves it.
# Used by Render (render.yaml). Three stages - only the last one is in the final image.

# 1. Build the React app (static files: index.html + assets)
FROM node:20-alpine AS client
WORKDIR /client
COPY Auction.Client/package.json Auction.Client/package-lock.json ./
RUN npm ci
COPY Auction.Client/ ./
RUN npm run build

# 2. Publish the API (.NET 6, Release)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS api
WORKDIR /src
COPY . .
RUN dotnet publish Auction.Api/Auction.Api.csproj -c Release -o /app

# 3. The running image: only the .NET runtime, the published API and the React files in wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=client /client/dist ./wwwroot
# Render sends the traffic to port 10000. HTTPS is handled by Render, so the app listens on HTTP.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Auction.Api.dll"]
