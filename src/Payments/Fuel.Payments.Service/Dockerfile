FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/Fuel.Events/Fuel.Events.csproj", "shared/Fuel.Events/"]
COPY ["src/Payments/Fuel.Payments.Service/Fuel.Payments.Service.csproj", "src/Payments/Fuel.Payments.Service/"]
RUN dotnet restore "src/Payments/Fuel.Payments.Service/Fuel.Payments.Service.csproj"

COPY . .
WORKDIR "/src/src/Payments/Fuel.Payments.Service"
RUN dotnet publish "Fuel.Payments.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fuel.Payments.Service.dll"]
