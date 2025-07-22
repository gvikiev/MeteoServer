using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySensorApi.Migrations
{
    public partial class InitialWithComfortProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Створення таблиці ComfortRecommendations
            migrationBuilder.CreateTable(
                name: "ComfortRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RoomName = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComfortRecommendations", x => x.Id);
                });

            // Створення таблиці SensorData
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
                    MQ2Analog = table.Column<double>(type: "float", nullable: true),
                    LightAnalog = table.Column<double>(type: "float", nullable: true),
                    MQ2AnalogPercent = table.Column<double>(type: "float", nullable: true),
                    LightAnalogPercent = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                });
            // ======== Users ========
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                          .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            // Якщо процедури ще немає — створити пусту
            migrationBuilder.Sql(@"
                IF OBJECT_ID('GenerateComfortRecommendations', 'P') IS NULL
                EXEC('CREATE PROCEDURE GenerateComfortRecommendations AS BEGIN SET NOCOUNT ON; RETURN; END');
            ");

            // Оновлення процедури з повним кодом
            migrationBuilder.Sql(@"-- тут вставляєш увесь твій довгий ALTER PROCEDURE, який ти вже писав
                -- скорочено тут для прикладу
                ALTER PROCEDURE GenerateComfortRecommendations
                    @Room NVARCHAR(100) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH LatestByRoom AS (
                        SELECT sd.*
                        FROM SensorData sd
                        JOIN (
                            SELECT RoomName, MAX(Timestamp) AS MaxTime
                            FROM SensorData
                            WHERE @Room IS NULL OR RoomName = @Room
                            GROUP BY RoomName
                        ) latest
                        ON sd.RoomName = latest.RoomName AND sd.Timestamp = latest.MaxTime
                    )
                    SELECT *
                    INTO #Latest
                    FROM LatestByRoom;

                    DECLARE 
                        @RoomName NVARCHAR(100),
                        @Temp FLOAT,
                        @Humidity FLOAT,
                        @Pressure FLOAT,
                        @Gas FLOAT,
                        @Light FLOAT,
                        @Recommendation NVARCHAR(MAX),
                        @PMV FLOAT,
                        @PPD FLOAT;

                    DECLARE cur CURSOR FOR
                        SELECT RoomName, TemperatureBme, HumidityBme, Pressure, MQ2Analog, LightAnalog
                        FROM #Latest;

                    OPEN cur;
                    FETCH NEXT FROM cur INTO @RoomName, @Temp, @Humidity, @Pressure, @Gas, @Light;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @Recommendation = N'';

                        IF @Temp IS NULL 
                            SET @Recommendation += N'Температурний сенсор не передав значення. ';
                        IF @Humidity IS NULL 
                            SET @Recommendation += N'Значення вологості відсутнє. ';
                        IF @Gas IS NULL 
                            SET @Recommendation += N'Дані про забруднення повітря відсутні. ';
                        IF @Light IS NULL 
                            SET @Recommendation += N'Освітленість не виміряна. ';

                        IF @Temp IS NOT NULL AND @Temp < 18
                            SET @Recommendation += N'Низька температура. Увімкніть обігрів. ';
                        ELSE IF @Temp IS NOT NULL AND @Temp > 27
                            SET @Recommendation += N'Занадто жарко. Провітріть кімнату або ввімкніть кондиціонер. ';

                        IF @Humidity IS NOT NULL AND @Humidity > 70
                            SET @Recommendation += N'Занадто висока вологість. Можливе утворення конденсату. ';
                        ELSE IF @Humidity IS NOT NULL AND @Humidity < 30
                            SET @Recommendation += N'Занадто низька вологість. Зволожіть повітря. ';

                        IF @Gas IS NOT NULL AND @Gas > 1000
                            SET @Recommendation += N'Підвищене забруднення повітря. Провітріть кімнату. ';

                        IF @Temp IS NOT NULL AND @Humidity IS NOT NULL
                        BEGIN
                            SET @PMV = 0.303 * EXP(-0.036 * 1.2) + 0.028 * ((0.5 * @Temp) - (0.31 * @Humidity));
                            SET @PPD = 100 - 95 * EXP(-0.03353 * POWER(@PMV, 4) - 0.2179 * POWER(@PMV, 2));

                            IF @PPD > 10
                                SET @Recommendation += N'Індекс PMV/PPD свідчить про низький комфорт. Відкоригуйте мікроклімат. ';
                        END

                        IF LEN(@Recommendation) = 0
                            SET @Recommendation = N'Мікроклімат у нормі.';

                        INSERT INTO ComfortRecommendations (RoomName, Recommendation)
                        VALUES (@RoomName, @Recommendation);

                        FETCH NEXT FROM cur INTO @RoomName, @Temp, @Humidity, @Pressure, @Gas, @Light;
                    END

                    CLOSE cur;
                    DEALLOCATE cur;
                    DROP TABLE #Latest;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GenerateComfortRecommendations;");
            migrationBuilder.DropTable(name: "ComfortRecommendations");
            migrationBuilder.DropTable(name: "SensorData");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
