database important

1. cadena de conexxion y remapeo de ladeb "necearia para incluir lso cambios de la bd a la aplicacion " 
dotnet ef dbcontext scaffold "Data Source=Database\app.sqlite" 
Microsoft.EntityFrameworkCore.Sqlite -o Database/EntityRepo/Entities  
 -c ThorlabsDbContext   --context-dir Database/EntityRepo --no-onconfiguring
-f