# Actualizar entidades (Scaffold) desde la base de datos

Este proyecto usa Entity Framework Core. Si hiciste cambios en la DB (nuevas columnas, relaciones, tablas) y queres re-mapear las entidades desde la base, usa **Scaffold**.

> Importante: Scaffold **regenera** clases y puede sobrescribir cambios manuales en entidades o en el `DbContext`. Hacer backup o usar control de versiones antes de ejecutar.

## Requisitos

1) Instalar `dotnet-ef` si no lo tenes:

```bash
dotnet tool install --global dotnet-ef
```

2) Verificar paquetes en el proyecto:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

> Si la DB es SQL Server u otro motor, cambiar el paquete del provider.

## Paso a paso (SQLite)

1) Ir a la carpeta del proyecto (donde esta `GotsThorlabs.csproj`).

2) Ejecutar el scaffold. Este comando sobrescribe las entidades:

```bash
dotnet ef dbcontext scaffold "Data Source=Database\app.sqlite" Microsoft.EntityFrameworkCore.Sqlite \
	-o Database/EntityRepo/Entities \
	-c ThorlabsDbContext \
	--context-dir Database/EntityRepo \
	--no-onconfiguring \
	-f
```

## Paso a paso (SQL Server)

1) Definir el connection string (ejemplo):

```bash
"Server=localhost;Database=MiDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

2) Ejecutar el scaffold cambiando el provider:

```bash
dotnet ef dbcontext scaffold "Server=localhost;Database=MiDb;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer \
	-o Database/EntityRepo/Entities \
	-c ThorlabsDbContext \
	--context-dir Database/EntityRepo \
	--no-onconfiguring \
	-f
```

## Salvedades y buenas practicas

- **Sobrescritura:** `-f` fuerza overwrite. Si queres comparar, genera en otra carpeta y luego move.
- **Migrations:** si ya usas migraciones, despues de scaffold pueden quedar desalineadas. Revisa tu historial y sincroniza el flujo.
- **OnModelCreating:** cualquier configuracion manual que no este en la DB se va a perder. Si necesitas reglas adicionales, reaplicalas luego.
- **Rutas en Windows:** usa comillas en el connection string si tiene backslashes o espacios.
- **DB actualizada:** el scaffold refleja **la DB actual**, no los cambios de tus clases.
- **Namespace:** si necesitas un namespace especifico, usa `--namespace` y `--context-namespace`.

## Tips utiles

- Listar tablas especificas:

```bash
dotnet ef dbcontext scaffold "Data Source=Database\app.sqlite" Microsoft.EntityFrameworkCore.Sqlite \
	-o Database/EntityRepo/Entities \
	-c ThorlabsDbContext \
	--context-dir Database/EntityRepo \
	--table user --table tour \
	--no-onconfiguring \
	-f
```

- Si cambiaste el modelo en codigo y queres actualizar la DB, **no uses scaffold**. Usa migrations:

```bash
dotnet ef migrations add NombreDeLaMigracion
dotnet ef database update
```
