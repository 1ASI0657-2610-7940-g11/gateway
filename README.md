# FuelTrack Microservices

Proyecto independiente de microservicios para FuelTrack. El frontend debe consumir solamente el Gateway; ningún cliente web se conecta directo a MySQL, RabbitMQ ni Redis.

## Servicios

- Gateway: `http://localhost:5000`
- Identity: `http://localhost:5001`
- Orders: `http://localhost:5002`
- Payments: `http://localhost:5003`
- Reporting/Client: `http://localhost:5004`

Rutas públicas por Gateway:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/profile/me`
- `GET /api/client/dashboard`
- `GET /api/client/kpis`
- `GET /api/orders`
- `POST /api/orders`
- `GET /api/payments/methods`
- `POST /api/payments/methods`
- `GET /api/payments/history`

## Desarrollo local

Levantar infraestructura:

```bash
docker compose up -d
```

Ejecutar servicios:

```bash
dotnet run --project src/Identity/Fuel.Identity.Service/Fuel.Identity.Service.csproj
dotnet run --project src/Orders/Fuel.Orders.Service/Fuel.Orders.Service.csproj
dotnet run --project src/Payments/Fuel.Payments.Service/Fuel.Payments.Service.csproj
dotnet run --project src/Reporting/Fuel.Reporting.Service/Fuel.Reporting.Service.csproj
dotnet run --project src/Gateway/Fuel.Gateway/Fuel.Gateway.csproj
```

Frontend local:

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

## Despliegue Railway

Crear un servicio Railway por cada microservicio usando el mismo repositorio y estos Dockerfile paths:

- Gateway: `src/Gateway/Fuel.Gateway/Dockerfile`
- Identity: `src/Identity/Fuel.Identity.Service/Dockerfile`
- Orders: `src/Orders/Fuel.Orders.Service/Dockerfile`
- Payments: `src/Payments/Fuel.Payments.Service/Dockerfile`
- Reporting: `src/Reporting/Fuel.Reporting.Service/Dockerfile`

Healthcheck path para todos:

```text
/health
```

El Gateway debe tener dominio público. Los demás servicios pueden quedar internos si Railway permite referencias internas entre servicios.

## Variables Railway

Gateway:

```env
AllowedOrigins=https://front-38m.pages.dev
ReverseProxy__Clusters__identity-cluster__Destinations__identity-dest__Address=https://<identity-url>/
ReverseProxy__Clusters__orders-cluster__Destinations__orders-dest__Address=https://<orders-url>/
ReverseProxy__Clusters__payments-cluster__Destinations__payments-dest__Address=https://<payments-url>/
ReverseProxy__Clusters__reporting-cluster__Destinations__reporting-dest__Address=https://<reporting-url>/
ASPNETCORE_ENVIRONMENT=Production
```

Identity, Orders, Payments y Reporting:

```env
MYSQLHOST=${{MySQL.MYSQLHOST}}
MYSQLPORT=${{MySQL.MYSQLPORT}}
MYSQLUSER=${{MySQL.MYSQLUSER}}
MYSQLPASSWORD=${{MySQL.MYSQLPASSWORD}}
MYSQLDATABASE=
JWT_SECRET=
JWT_ISSUER=FuelTrack.Api
JWT_AUDIENCE=FuelTrack.Web
JWT_EXPIRATION_MINUTES=120
RabbitMQ__Host=${{RabbitMQ.RABBITMQ_HOST}}
RabbitMQ__Port=${{RabbitMQ.RABBITMQ_PORT}}
RabbitMQ__Username=${{RabbitMQ.RABBITMQ_USER}}
RabbitMQ__Password=${{RabbitMQ.RABBITMQ_PASSWORD}}
ASPNETCORE_ENVIRONMENT=Production
ENABLE_SWAGGER=true
```

Reporting además necesita Redis:

```env
ConnectionStrings__Redis=${{Redis.REDIS_URL}}
```

Si se usa una sola base MySQL compartida, deja `MYSQLDATABASE` vacío para que cada servicio use su base lógica por defecto: `identity_db`, `orders_db`, `payments_db` y `reporting_db`. Si Railway exige una base específica, configura `ConnectionStrings__IdentityConnection`, `ConnectionStrings__OrdersConnection`, `ConnectionStrings__PaymentsConnection` o `ConnectionStrings__ReportingConnection`.

## Cloudflare Pages

Cuando el Gateway esté desplegado, configurar en el frontend:

```env
VITE_API_BASE_URL=https://<gateway-url>/api
NODE_VERSION=22
```

Build:

```text
npm run build
```

Output:

```text
dist
```
