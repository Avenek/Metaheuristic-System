FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Metaheuristic_system/Metaheuristic_system.csproj", "Metaheuristic_system/"]
RUN dotnet restore "Metaheuristic_system/Metaheuristic_system.csproj"
COPY . .
WORKDIR "/src/Metaheuristic_system"
RUN dotnet build "Metaheuristic_system.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Metaheuristic_system.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Metaheuristic_system.dll"]