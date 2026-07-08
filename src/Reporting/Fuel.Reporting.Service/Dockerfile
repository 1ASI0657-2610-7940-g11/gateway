FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/Fuel.Events/Fuel.Events.csproj", "shared/Fuel.Events/"]
COPY ["src/Reporting/Fuel.Reporting.Service/Fuel.Reporting.Service.csproj", "src/Reporting/Fuel.Reporting.Service/"]
RUN dotnet restore "src/Reporting/Fuel.Reporting.Service/Fuel.Reporting.Service.csproj"

COPY . .
WORKDIR "/src/src/Reporting/Fuel.Reporting.Service"
RUN dotnet publish "Fuel.Reporting.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fuel.Reporting.Service.dll"]
