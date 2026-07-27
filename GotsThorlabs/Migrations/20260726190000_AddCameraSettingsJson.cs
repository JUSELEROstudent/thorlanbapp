using GotsThorlabs.Database.EntityRepo;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotsThorlabs.Migrations
{
    /// <summary>
    /// Agrega la columna settingsJson a la tabla camera, donde se guardan los
    /// parámetros de captura configurados por cámara (exposición, ganancia, etc.),
    /// serializados como JSON.
    ///
    /// Escrita a mano siguiendo la convención de AddCameraDriverType: es un
    /// AddColumn puntual. Se evita generarla con 'dotnet ef migrations add' porque
    /// el snapshot arrastra diferencias con el modelo actual (CameraId pasó de Guid
    /// a string sin migración, y driverType tiene el default declarado solo en el
    /// DbContext), y la herramienta emitiría AlterColumn no deseados sobre columnas
    /// que hoy funcionan.
    /// </summary>
    public partial class AddCameraSettingsJson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "settingsJson",
                table: "camera",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "settingsJson",
                table: "camera");
        }
    }
}
