FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RaceDay.Mvc/RaceDay.Mvc.csproj", "RaceDay.Mvc/"]
RUN dotnet restore "RaceDay.Mvc/RaceDay.Mvc.csproj"

COPY . .
WORKDIR /src/RaceDay.Mvc
RUN dotnet publish "RaceDay.Mvc.csproj" --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
