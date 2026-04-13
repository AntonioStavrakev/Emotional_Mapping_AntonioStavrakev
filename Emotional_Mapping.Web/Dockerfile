FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Emotional_Mapping_AntonioStavrakev.sln", "./"]
COPY ["Emotional_Mapping.Web/Emotional_Mapping.Web.csproj", "Emotional_Mapping.Web/"]
COPY ["Emotional_Mapping.Application/Emotional_Mapping.Application.csproj", "Emotional_Mapping.Application/"]
COPY ["Emotional_Mapping.Domain/Emotional_Mapping.Domain.csproj", "Emotional_Mapping.Domain/"]
COPY ["Emotional_Mapping.Infrastructure/Emotional_Mapping.Infrastructure.csproj", "Emotional_Mapping.Infrastructure/"]
COPY ["Emotional_Mapping.Api/Emotional_Mapping.Api.csproj", "Emotional_Mapping.Api/"]

RUN dotnet restore "Emotional_Mapping.Web/Emotional_Mapping.Web.csproj"

COPY . .
RUN dotnet publish "Emotional_Mapping.Web/Emotional_Mapping.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Emotional_Mapping.Web.dll"]
