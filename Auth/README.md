# AUTH ASP.NET Core

Microservicio AUTH en `Auth/Auth`, separado del AUTH Java. La comparación de comportamiento está documentada en [JAVA-COMPARISON.md](JAVA-COMPARISON.md). Este proyecto no modifica Java, Gateway ni otros microservicios, y no incluye migraciones de esquema.

## Ejecutar

Desde `C:\SmartWeight-ASP`:

```powershell
dotnet run --project .\Auth\Auth.csproj
```

La configuración local de `Auth/appsettings.json` contiene la conexión SQL Server de AUTH y la configuración JWT tomada de la configuración Java efectiva. El servicio escucha en el puerto 8081 por defecto. Ese archivo contiene credenciales y un secreto real; no se deben publicar ni incluir en commits.

La configuración se puede sobreescribir mediante variables de entorno:

```powershell
$env:ConnectionStrings__DefaultConnection = '<cadena SQL Server de AUTH>'
$env:Jwt__Secret = '<secreto compartido con Java AUTH>'
$env:Jwt__Expiration = '43200000'
$env:ASPNETCORE_URLS = 'http://0.0.0.0:8081'
```

La duración configurada es 43.200.000 ms (12 horas). El JWT usa HS256, `sub`, `rol`, `iat` y `exp`.

## Persistencia y contratos

La entidad mapea la tabla `dbo.Usuarios` sin cambios de esquema. `Est_Usu` usa `byte` según SQL Server `tinyint`; `Fecha_Cre` y `Fecha_Mod` usan `DateTime?` según las columnas `datetime`. `UsuarioDTO` mantiene las fechas de respuesta como cadenas y convierte las fechas de entidad mediante `DateTime.ToString()`; el formato depende de la cultura del proceso .NET.

Las rutas conservan el prefijo Java: `POST /api/auth/login`, `GET /api/auth/usuarios`, `GET /api/auth/test`, `GET /api/auth/admin/test`, `GET /api/auth/operador/test` y `PUT /api/auth/password`.

## Validación

El login real se comprobó desde Postman contra SQL Server: devuelve usuario, rol, estado activo, `passwordHash` y JWT con expiración de 12 horas. `Auth.Tests` son pruebas unitarias de apoyo; no sustituyen esa comprobación real de conexión y consulta.

La consulta incremental `lastSync` de Java usa SQL vacío. AUTH .NET documenta la limitación y no inventa una consulta de sincronización.
