using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MySensorApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChipId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemperatureDht = table.Column<float>(type: "real", nullable: true),
                    HumidityDht = table.Column<float>(type: "real", nullable: true),
                    GasDetected = table.Column<bool>(type: "bit", nullable: true),
                    Light = table.Column<bool>(type: "bit", nullable: true),
                    Pressure = table.Column<float>(type: "real", nullable: true),
                    Altitude = table.Column<float>(type: "real", nullable: true),
                    TemperatureBme = table.Column<float>(type: "real", nullable: true),
                    HumidityBme = table.Column<float>(type: "real", nullable: true),
                    Mq2Analog = table.Column<int>(type: "int", nullable: true),
                    Mq2AnalogPercent = table.Column<float>(type: "real", nullable: true),
                    LightAnalog = table.Column<int>(type: "int", nullable: true),
                    LightAnalogPercent = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LowValue = table.Column<float>(type: "real", nullable: false),
                    HighValue = table.Column<float>(type: "real", nullable: false),
                    LowValueMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighValueMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensorOwnerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChipId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorOwnerships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettingsUserAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SettingId = table.Column<int>(type: "int", nullable: false),
                    LowValueAdjustment = table.Column<float>(type: "real", nullable: false),
                    HighValueAdjustment = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingsUserAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingsUserAdjustments_Settings_SettingId",
                        column: x => x.SettingId,
                        principalTable: "Settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SettingsUserAdjustments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComfortRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChipId = table.Column<int>(type: "int", nullable: false),
                    SensorOwnershipId = table.Column<int>(type: "int", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComfortRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComfortRecommendations_SensorOwnerships_SensorOwnershipId",
                        column: x => x.SensorOwnershipId,
                        principalTable: "SensorOwnerships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "RoleName" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComfortRecommendations_SensorOwnershipId",
                table: "ComfortRecommendations",
                column: "SensorOwnershipId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorOwnerships_ChipId",
                table: "SensorOwnerships",
                column: "ChipId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorOwnerships_ChipId_UserId",
                table: "SensorOwnerships",
                columns: new[] { "ChipId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorOwnerships_UserId",
                table: "SensorOwnerships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingsUserAdjustments_SettingId",
                table: "SettingsUserAdjustments",
                column: "SettingId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingsUserAdjustments_UserId",
                table: "SettingsUserAdjustments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComfortRecommendations");

            migrationBuilder.DropTable(
                name: "SensorData");

            migrationBuilder.DropTable(
                name: "SettingsUserAdjustments");

            migrationBuilder.DropTable(
                name: "SensorOwnerships");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
