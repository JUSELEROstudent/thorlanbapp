using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotsThorlabs.Migrations
{
    public partial class AddCalibrationTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "camera",
                columns: table => new
                {
                    cameraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    localIdentifier = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    features = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_camera", x => x.cameraId);
                });

            migrationBuilder.CreateTable(
                name: "increase",
                columns: table => new
                {
                    increaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    aditionalInfo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_increase", x => x.increaseId);
                });

            migrationBuilder.CreateTable(
                name: "microscope",
                columns: table => new
                {
                    microscopeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    brand = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    site = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    aditionalInfo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_microscope", x => x.microscopeId);
                });

            migrationBuilder.CreateTable(
                name: "tour",
                columns: table => new
                {
                    idTour = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    nameFolder = table.Column<string>(type: "TEXT", nullable: false),
                    NumberX = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberY = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberZ = table.Column<int>(type: "INTEGER", nullable: false),
                    Camera = table.Column<int>(type: "INTEGER", nullable: false),
                    endStatus = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tour", x => x.idTour);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    idUser = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nickname = table.Column<string>(type: "TEXT", nullable: false),
                    eMail = table.Column<string>(type: "TEXT", nullable: false),
                    password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.idUser);
                });

            migrationBuilder.CreateTable(
                name: "groupCalibration",
                columns: table => new
                {
                    groupCailbrationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    cameraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    microscopeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    increaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    aditionalInfo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groupCalibration", x => x.groupCailbrationId);
                    table.ForeignKey(
                        name: "FK_groupCalibration_camera_cameraId",
                        column: x => x.cameraId,
                        principalTable: "camera",
                        principalColumn: "cameraId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_groupCalibration_increase_increaseId",
                        column: x => x.increaseId,
                        principalTable: "increase",
                        principalColumn: "increaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_groupCalibration_microscope_microscopeId",
                        column: x => x.microscopeId,
                        principalTable: "microscope",
                        principalColumn: "microscopeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "image",
                columns: table => new
                {
                    idImage = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    gausianVal = table.Column<double>(type: "REAL", nullable: true),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Z = table.Column<int>(type: "INTEGER", nullable: false),
                    idTour = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image", x => x.idImage);
                    table.ForeignKey(
                        name: "FK_image_tour_idTour",
                        column: x => x.idTour,
                        principalTable: "tour",
                        principalColumn: "idTour",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "picsCalibration",
                columns: table => new
                {
                    picsCalibrationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    groupCailbrationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    pic1 = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    pic2 = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    axeDirectionCalibration = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    acepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    dx = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    dy = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    confidence = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    measureUnit = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    movementValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picsCalibration", x => x.picsCalibrationId);
                    table.ForeignKey(
                        name: "FK_picsCalibration_groupCalibration_groupCailbrationId",
                        column: x => x.groupCailbrationId,
                        principalTable: "groupCalibration",
                        principalColumn: "groupCailbrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_groupCalibration_cameraId",
                table: "groupCalibration",
                column: "cameraId");

            migrationBuilder.CreateIndex(
                name: "IX_groupCalibration_increaseId",
                table: "groupCalibration",
                column: "increaseId");

            migrationBuilder.CreateIndex(
                name: "IX_groupCalibration_microscopeId",
                table: "groupCalibration",
                column: "microscopeId");

            migrationBuilder.CreateIndex(
                name: "IX_image_idTour",
                table: "image",
                column: "idTour");

            migrationBuilder.CreateIndex(
                name: "IX_picsCalibration_groupCailbrationId",
                table: "picsCalibration",
                column: "groupCailbrationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "image");

            migrationBuilder.DropTable(
                name: "picsCalibration");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "tour");

            migrationBuilder.DropTable(
                name: "groupCalibration");

            migrationBuilder.DropTable(
                name: "camera");

            migrationBuilder.DropTable(
                name: "increase");

            migrationBuilder.DropTable(
                name: "microscope");
        }
    }
}
