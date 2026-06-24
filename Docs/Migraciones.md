# Migraciones de base de datos

Este microservicio usa Entity Framework Core con PostgreSQL/Npgsql para aplicar el esquema de reportes en Neon.

## Configurar conexion

La cadena de conexion esta configurada en `appsettings.json` y `appsettings.Development.json` con la clave `ConnectionStrings:ReportsDb`.

Si se quiere evitar guardar credenciales en archivos versionables, se puede sobrescribir con user-secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:ReportsDb" "postgresql://USUARIO:PASSWORD@HOST/neondb?sslmode=require&channel_binding=require"
```

Tambien puedes usar una variable de entorno:

```powershell
$env:ConnectionStrings__ReportsDb = "postgresql://USUARIO:PASSWORD@HOST/neondb?sslmode=require&channel_binding=require"
```

## Aplicar migraciones

```powershell
dotnet ef database update
```

EF Core creara la tabla `__EFMigrationsHistory` para registrar el historial aplicado.

## Ver migraciones

```powershell
dotnet ef migrations list
```

## Revertir a cero

Esto elimina los objetos creados por la migracion inicial. Usalo solo en ambientes donde sea aceptable perder el esquema.

```powershell
dotnet ef database update 0
```

## Migracion automatica al iniciar

Por defecto esta desactivada:

```json
"Database": {
  "ApplyMigrationsOnStartup": false
}
```

Para ambientes controlados se puede activar con:

```powershell
$env:Database__ApplyMigrationsOnStartup = "true"
```

## Endpoints de verificacion

- `GET /` confirma que el servicio esta activo.
- `GET /health/db` confirma conexion a PostgreSQL.
- `GET /health/db/migrations` muestra migraciones aplicadas y pendientes.
