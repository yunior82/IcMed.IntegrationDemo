FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IcMed.IntegrationDemo.sln", "/src/"]
COPY ["src/Core/IcMed.IntegrationDemo.Domain/IcMed.IntegrationDemo.Domain.csproj", "src/Core/IcMed.IntegrationDemo.Domain/"]
COPY ["src/Core/IcMed.IntegrationDemo.Application/IcMed.IntegrationDemo.Application.csproj", "src/Core/IcMed.IntegrationDemo.Application/"]
COPY ["src/Infrastructure/IcMed.IntegrationDemo.Infrastructure/IcMed.IntegrationDemo.Infrastructure.csproj", "src/Infrastructure/IcMed.IntegrationDemo.Infrastructure/"]
COPY ["src/WebApi/IcMed.IntegrationDemo.WebApi/IcMed.IntegrationDemo.WebApi.csproj", "src/WebApi/IcMed.IntegrationDemo.WebApi/"]
RUN dotnet restore "IcMed.IntegrationDemo.sln"
COPY . .
WORKDIR "/src/src/WebApi/IcMed.IntegrationDemo.WebApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IcMed.IntegrationDemo.WebApi.dll"]
