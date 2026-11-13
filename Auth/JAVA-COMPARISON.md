# Referencia de migración Java AUTH

Fuente revisada: `C:\Proyectos-Java\auth`, incluyendo `AuthController`, `LoginService`, `UsuarioDTO`, `UsuarioEntity`, `UsuarioRepositoryImpl`, `JwtTokenProvider`, `SecurityConfig` y la configuración externa `C:\Proyectos-Java\config\auth.properties`. La implementación Java permanece intacta.

## Equivalencias finales

| Comportamiento Java | AUTH ASP.NET Core |
|---|---|
| Contexto `/api` y controlador `/auth` | Rutas con prefijo `api/auth` |
| Login por `POST /api/auth/login` con `username` y `password`; respuesta `token` + `usuario` | Mismo método, ruta y concepto `LoginResponse` con `UsuarioDTO` |
| Estado activo cuando `Est_Usu == 1` | Propiedad derivada del byte `Est_Usu` con la misma condición |
| BCrypt validado; password heredado migrado a hash | BCrypt compatible y hash generado con costo 10 |
| DTO tiene `id`, `username`, `passwordHash`, `password`, `nombre`, `rol`, `activo`, `fechaCre`, `fechaMod`, `modifica`; `password` es `null` | `UsuarioDTO` conserva esas propiedades y el login expone el hash como Java |
| `/auth/test` público; `/auth/admin/**` requiere ADMIN; `/auth/operador/**` acepta ADMIN y OPERADOR | Mismas reglas, con `rol` como claim de rol de ASP.NET |
| Password usa query `username` y `nuevoPassword`, persiste solo `password_hash` | Mismo contrato y campo actualizado |
| JWT HS256 con `sub`, `rol`, `iat`, `exp` | Mismo algoritmo y claims; duración externa Java efectiva: 43.200.000 ms |
| Tabla `Usuarios` y columnas mapeadas explícitamente | SQL Server `dbo.Usuarios`, sin migraciones ni cambios al esquema |

## Fechas

Java declara `Fecha_Cre` y `Fecha_Mod` como `String` en entidad y DTO, y pasa los getters directamente al construir `UsuarioDTO`; AUTH no contiene un formateador explícito para esos valores. La tabla real tiene ambas columnas como SQL Server `datetime`. .NET refleja eso en la entidad con `DateTime?`, pero el DTO sigue entregando `string?` mediante `DateTime.ToString()`. La estructura del contrato (cadenas) se conserva; el formato exacto del texto puede depender de la cultura del proceso y no se fija en Java mediante un patrón.

## Configuración SQL y JWT

La configuración Java efectiva usa SQL Server en `localhost:1433`, base de datos `BdDespacho`, con `encrypt=false`; AUTH ASP.NET tiene el equivalente en su `appsettings.json`. Java configura expiración JWT en 43.200.000 ms (12 horas), valor que usa también ASP.NET. Los valores de usuario, contraseña y secreto son sensibles y no se reproducen en este documento.

## `lastSync`

Java pasa `lastSync` al repositorio JDBC aunque la cadena SQL está vacía. AUTH .NET no ejecuta SQL vacío ni inventa una consulta incremental; con `lastSync` informado responde HTTP 500 mediante el manejador genérico. El error detallado del SQL vacío Java depende del driver y servidor. Sin `lastSync`, ambos listan usuarios.

## Errores

El servicio Java lanza excepciones ante usuario inexistente, inactivo o credenciales incorrectas; su respuesta normal es HTTP 500. AUTH .NET conserva HTTP 500 con un envelope genérico compatible (`timestamp`, `status`, `error`, `path`), sin filtrar detalles internos. Las rutas protegidas conservan 401 para no autenticado y 403 para rol insuficiente.
