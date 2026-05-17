# Entity + SQLite update guide

Este documento define la metodología para reflejar en SQLite los cambios hechos en clases de entidad de Entity Framework Core.

## Caso actual: campo `DriverType` en `Camera`

Se agregó el campo `DriverType` en la entidad `Camera` con valor por defecto `generic`.

## Pasos obligatorios cuando se modifica una entidad

1. Modificar la clase de entidad
   - Archivo ejemplo: `Database/EntityRepo/Entities/Camera.cs`
   - Agregar, cambiar o eliminar la propiedad.

2. Actualizar el mapeo en `DbContext`
   - Archivo: `Database/EntityRepo/ThorlabsDbContext.cs`
   - Reflejar nombre de columna, tipo, longitud, default, required, relaciones, etc.

3. Crear una migración nueva
   - Comando:
   - `dotnet ef migrations add NombreDescriptivo --project .\GotsThorlabs.csproj`
   - Ejemplo:
   - `dotnet ef migrations add AddCameraDriverType --project .\GotsThorlabs.csproj`

4. Revisar archivos generados por la migración
   - `Migrations\<timestamp>_<Nombre>.cs`
   - `Migrations\<timestamp>_<Nombre>.Designer.cs` si aplica
   - `Migrations\ThorlabsDbContextModelSnapshot.cs`

5. Aplicar la migración a la base SQLite
   - Comando:
   - `dotnet ef database update --project .\GotsThorlabs.csproj`

6. Verificar que la app también la pueda aplicar al arrancar
   - En `Program.cs` ya existe:
   - `db.Database.Migrate();`
   - Esto ayuda a aplicar migraciones pendientes al iniciar, pero no reemplaza validar que la migración exista correctamente.

7. Confirmar el cambio en SQLite
   - Revisar esquema de tabla o consultar la DB.
   - Ejemplo con sqlite3:
   - `sqlite3 .\database\app.sqlite ".schema camera"`

## Checklist rápido

- [ ] Entidad actualizada
- [ ] `ThorlabsDbContext` actualizado
- [ ] Migración creada
- [ ] Migración aplicada con `database update`
- [ ] Esquema SQLite verificado
- [ ] Build exitoso

## Error común

Si el campo existe en código pero no en la base:
- probablemente falta correr `dotnet ef migrations add ...`
- o falta correr `dotnet ef database update`
- o la migración no quedó registrada en `__EFMigrationsHistory`

## Convención propuesta para drivers

Valores permitidos para `DriverType`:
- `generic`
- `ids_peak_dotnet`

## Nota práctica

Si se modifica una entidad y no se crea migración, SQLite no se actualiza solo por cambiar la clase C#. Entity Framework necesita la migración para traducir el cambio al esquema real de la base de datos.
