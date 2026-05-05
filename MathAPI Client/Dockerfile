FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER root
RUN mkdir /keys && chown app:app /keys
RUN apt-get update && apt-get install -y libgssapi-krb5-2
USER app

WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .
RUN dotnet restore "./MathAPIClient.csproj"
RUN dotnet publish "./MathAPIClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MathAPIClient.dll"]