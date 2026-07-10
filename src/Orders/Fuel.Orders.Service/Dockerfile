FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Railway redeploy trigger for Orders edit/delete production fixes.
WORKDIR /src

COPY ["shared/Fuel.Events/Fuel.Events.csproj", "shared/Fuel.Events/"]
COPY ["src/Orders/Fuel.Orders.Service/Fuel.Orders.Service.csproj", "src/Orders/Fuel.Orders.Service/"]
RUN dotnet restore "src/Orders/Fuel.Orders.Service/Fuel.Orders.Service.csproj"

COPY . .
WORKDIR "/src/src/Orders/Fuel.Orders.Service"
RUN dotnet publish "Fuel.Orders.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fuel.Orders.Service.dll"]
