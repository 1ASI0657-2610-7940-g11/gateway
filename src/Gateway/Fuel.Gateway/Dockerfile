FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Gateway/Fuel.Gateway/Fuel.Gateway.csproj", "src/Gateway/Fuel.Gateway/"]
RUN dotnet restore "src/Gateway/Fuel.Gateway/Fuel.Gateway.csproj"

COPY . .
WORKDIR "/src/src/Gateway/Fuel.Gateway"
RUN dotnet publish "Fuel.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fuel.Gateway.dll"]
