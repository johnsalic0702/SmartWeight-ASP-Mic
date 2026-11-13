# Core-Pesaje ASP.NET Core

Servicio ASP.NET Core que conserva las rutas del microservicio Java y delega la lógica de negocio en los procedimientos de SQL Server. No ejecuta lógica equivalente a los procedimientos en C#.

## Configuración

Target framework: .NET 9. Puerto predeterminado: `8082`. Las rutas conservan el prefijo `/api` para coincidir con el context path Java y el Gateway.

Configure la conexión mediante la variable de ambiente `ConnectionStrings__CorePesaje`. El valor no está versionado en este proyecto. Configure el puerto mediante `ASPNETCORE_URLS` si el entorno requiere otra URL.

Ejemplo local (sustituir por credenciales locales, no guardar el valor en Git):

```powershell
$env:ConnectionStrings__CorePesaje = '<connection string>'
$env:ASPNETCORE_URLS = 'http://localhost:8082'
dotnet run
```

## Endpoints

- `POST /api/controlpeso`
- `GET /api/controlpeso/listporid/id/{id}`
- `GET /api/controlpeso/listporfecha/empresa/{empresaId}/sucursal/{sucursalId}/almacen/{almacenId}/fecha/{fecha}/estado/{estado}`
- `PUT /api/controlpeso/estado?id=...&estado=...&usuarioId=...`
- `GET /api/controlpeso/combo/emp/{empresaId}/suc/{sucursalId}/alm/{almacenId}/fec/{fecha}`
- `GET /api/controlpeso/{id}/ticketpdf`
- `GET /api/controlpeso/{id}/ticketid`
- `POST /api/guias`
- `GET /api/guias/id/{id}`
- `GET /api/guias/fecha?fecha=...&estado=...`
- `PUT /api/guias/estado?id=...&estado=...&usuarioId=...`
- `GET /api/guias/combo/emp/{empresaId}/suc/{sucursalId}/alm/{almacenId}/fec/{fecha}`
- `POST /api/pedidos`
- `GET /api/pedidos/id/{id}`
- `GET /api/pedidos/fecha?fecha=...&estado=...`
- `PUT /api/pedidos/estado?id=...&estado=...&usuarioId=...`
- `GET /api/pedidos/replica?fecha=...`

Los resultados `FOR JSON PATH` se entregan como JSON sin volver a serializarlos como una cadena JSON. Los endpoints tabulares de control de peso se mapean a los DTOs Java. La llamada de escritura lee todos los Result Sets y selecciona el que contiene `Indice_Id`, incluso cuando el INSERT produce previamente un Result Set con `ID`.

## Seguridad y Gateway

La configuración Gateway inspeccionada contiene una ruta `/api/controlpeso/**` hacia Core-Pesaje y no presenta filtros de autenticación JWT en ese archivo. No se añadió autenticación dentro del servicio. Esa configuración no contiene rutas de gateway para `/api/guias` ni `/api/pedidos`; estos endpoints sí quedan disponibles directamente en el servicio, como en los controladores Java.

## Discrepancia confirmada de estado

La llamada Java para estado de ControlPeso se corrigió a tipo 1 de `Web_ActualizaEstado`, conforme al SP. Guía continúa con tipo 2 y Pedido con tipo 4.

## Observaciones de compatibilidad

- Pedido usa únicamente ramas SQL 0–3; no implementa tipo 4.
- Ticket usa `Rpt_ControlPesoQR` tipo 2. Token, URL y hashing los produce SQL y no se recalculan en C#.
- Guía y Pedido conservan el agrupamiento previo de detalles que hacía Java.
- El agrupamiento C# conserva el orden de primera aparición de las claves; el orden de iteración del `HashMap` Java no está garantizado y puede diferir.
- `Web_Guia` puede producir un Result Set intermedio `ID` al insertar y otro final `Indice_Id/Msg`; el repositorio consume ambos.
