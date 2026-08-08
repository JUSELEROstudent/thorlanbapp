using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotsThorlabs.Migrations
{
    /// <summary>
    /// Caracterización mecánica del motor: parámetros de accionamiento y tamaño real del
    /// paso medido con pie de rey, por eje y por sentido.
    ///
    /// NOTA sobre lo que este archivo NO hace. El andamiaje automático incluía además un
    /// renombrado de la columna picsCalibrationId de la tabla tour, el renombrado de su
    /// índice y la reconstrucción de tour, picsCalibration y camera. Esas operaciones se
    /// retiraron a mano porque no corresponden a este cambio y además fallaban contra la
    /// base real: el índice IX_tour_picsCalibrationId que pretendían renombrar no existe,
    /// y la columna ya está como PicsCalibrationId con su clave foránea creada.
    ///
    /// El origen es una desincronización entre el snapshot del modelo y la base: en
    /// __EFMigrationsHistory figura aplicada la migración 20260211013119_RelacionYCampoNuevo,
    /// cuyo archivo no está en el repositorio. Mientras eso no se resuelva, cada migración
    /// nueva volverá a arrastrar esas operaciones y habrá que retirarlas igual.
    /// </summary>
    public partial class AddMotorStepCalibration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "motorCalibration",
                columns: table => new
                {
                    motorCalibrationId = table.Column<string>(type: "TEXT", nullable: false),
                    groupCailbrationId = table.Column<string>(type: "TEXT", nullable: false),
                    kimDeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    stepRate = table.Column<long>(type: "INTEGER", nullable: false),
                    stepAcceleration = table.Column<long>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    acepted = table.Column<long>(type: "INTEGER", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    aditionalInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorCalibration", x => x.motorCalibrationId);
                    table.ForeignKey(
                        name: "FK_motorCalibration_groupCalibration_groupCailbrationId",
                        column: x => x.groupCailbrationId,
                        principalTable: "groupCalibration",
                        principalColumn: "groupCailbrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "axisStepCalibration",
                columns: table => new
                {
                    axisStepCalibrationId = table.Column<string>(type: "TEXT", nullable: false),
                    motorCalibrationId = table.Column<string>(type: "TEXT", nullable: false),
                    axisName = table.Column<string>(type: "TEXT(5)", nullable: false),
                    stepSizeNmForward = table.Column<string>(type: "TEXT", nullable: true),
                    stepSizeNmBackward = table.Column<string>(type: "TEXT", nullable: true),
                    stepSizeNm = table.Column<string>(type: "TEXT", nullable: true),
                    hysteresisPct = table.Column<string>(type: "TEXT", nullable: true),
                    relativeErrorPct = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_axisStepCalibration", x => x.axisStepCalibrationId);
                    table.ForeignKey(
                        name: "FK_axisStepCalibration_motorCalibration_motorCalibrationId",
                        column: x => x.motorCalibrationId,
                        principalTable: "motorCalibration",
                        principalColumn: "motorCalibrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "axisStepMeasurement",
                columns: table => new
                {
                    axisStepMeasurementId = table.Column<string>(type: "TEXT", nullable: false),
                    axisStepCalibrationId = table.Column<string>(type: "TEXT", nullable: false),
                    direction = table.Column<string>(type: "TEXT(10)", nullable: false),
                    sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    stepsCommanded = table.Column<long>(type: "INTEGER", nullable: false),
                    caliperReadingMm = table.Column<string>(type: "TEXT", nullable: false),
                    measuredAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_axisStepMeasurement", x => x.axisStepMeasurementId);
                    table.ForeignKey(
                        name: "FK_axisStepMeasurement_axisStepCalibration_axisStepCalibrationId",
                        column: x => x.axisStepCalibrationId,
                        principalTable: "axisStepCalibration",
                        principalColumn: "axisStepCalibrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_motorCalibration_groupCailbrationId",
                table: "motorCalibration",
                column: "groupCailbrationId");

            migrationBuilder.CreateIndex(
                name: "IX_axisStepCalibration_motorCalibrationId",
                table: "axisStepCalibration",
                column: "motorCalibrationId");

            migrationBuilder.CreateIndex(
                name: "IX_axisStepMeasurement_axisStepCalibrationId",
                table: "axisStepMeasurement",
                column: "axisStepCalibrationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "axisStepMeasurement");
            migrationBuilder.DropTable(name: "axisStepCalibration");
            migrationBuilder.DropTable(name: "motorCalibration");
        }
    }
}
