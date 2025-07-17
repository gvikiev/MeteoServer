using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySensorApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemperatureDht = table.Column<double>(type: "float", nullable: true),
                    HumidityDht = table.Column<double>(type: "float", nullable: true),
                    GasDetected = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Light = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pressure = table.Column<double>(type: "float", nullable: true),
                    Altitude = table.Column<double>(type: "float", nullable: true),
                    TemperatureBme = table.Column<double>(type: "float", nullable: true),
                    HumidityBme = table.Column<double>(type: "float", nullable: true),
                    MQ2Analog = table.Column<int>(type: "int", nullable: true),
                    LightAnalog = table.Column<int>(type: "int", nullable: true),
                    MQ2AnalogPercent = table.Column<double>(type: "float", nullable: true),
                    LightAnalogPercent = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorData");
        }
    }
}
