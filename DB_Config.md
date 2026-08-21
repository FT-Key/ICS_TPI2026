# Configuracion de Base de Datos — SQL Server (Docker)

## Datos de Conexion

| Campo | Valor |
|---|---|
| Server | `localhost,1433` |
| User | `sa` |
| Password | `YourStrong!Password123` |
| Trust Server Certificate | `True` |
| Encrypt | `Obligatorio` |

## Connection String (para appsettings.json)

```
Server=localhost,1433;Database=Dsw2025Tpi;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=True;
```

## Docker

- Container name: `ics-sqlserver`
- Image: `mcr.microsoft.com/mssql/server:2022-latest`
- Port: `1433:1433`
