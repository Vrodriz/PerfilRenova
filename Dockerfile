
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["PerfilRenovaWeb.api.csproj", "./"]
RUN dotnet restore "PerfilRenovaWeb.api.csproj"


COPY . .

RUN dotnet build "PerfilRenovaWeb.api.csproj" -c Release -o /app/build

RUN dotnet publish "PerfilRenovaWeb.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PerfilRenovaWeb.api.dll"]
