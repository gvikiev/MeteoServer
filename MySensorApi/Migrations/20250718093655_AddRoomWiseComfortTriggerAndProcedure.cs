using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySensorApi.Migrations
{
    public partial class AddRoomWiseComfortTriggerAndProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Таблиця ComfortRecommendations
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ComfortRecommendations' AND xtype='U')
                CREATE TABLE ComfortRecommendations (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Timestamp DATETIME DEFAULT GETUTCDATE(),
                    RoomName NVARCHAR(100),
                    Recommendation NVARCHAR(MAX)
                );
            ");

            // Видалення старої процедури
            migrationBuilder.Sql(@"
                IF OBJECT_ID('GenerateComfortRecommendations', 'P') IS NOT NULL
                DROP PROCEDURE GenerateComfortRecommendations;
            ");

            // Реальна процедура
            migrationBuilder.Sql(@"
                CREATE PROCEDURE GenerateComfortRecommendations
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH LatestByRoom AS (
                        SELECT sd.*
                        FROM SensorData sd
                        JOIN (
                            SELECT RoomName, MAX(Timestamp) AS MaxTime
                            FROM SensorData
                            GROUP BY RoomName
                        ) latest
                        ON sd.RoomName = latest.RoomName AND sd.Timestamp = latest.MaxTime
                    )
                    SELECT *
                    INTO #Latest
                    FROM LatestByRoom;

                    DECLARE 
                        @Room NVARCHAR(100),
                        @Temp FLOAT,
                        @Humidity FLOAT,
                        @Pressure FLOAT,
                        @Gas INT,
                        @Light INT,
                        @Recommendation NVARCHAR(MAX);

                    DECLARE cur CURSOR FOR
                        SELECT RoomName, TemperatureBme, HumidityBme, Pressure, MQ2Analog, LightAnalog
                        FROM #Latest;

                    OPEN cur;
                    FETCH NEXT FROM cur INTO @Room, @Temp, @Humidity, @Pressure, @Gas, @Light;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @Recommendation = '';

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
                            DECLARE @PMV FLOAT = 0.303 * EXP(-0.036 * 1.2) + 0.028 * ((0.5 * @Temp) - (0.31 * @Humidity));
                            DECLARE @PPD FLOAT = 100 - 95 * EXP(-0.03353 * POWER(@PMV, 4) - 0.2179 * POWER(@PMV, 2));

                            IF @PPD > 10
                                SET @Recommendation += N'Індекс PMV/PPD свідчить про низький комфорт. Відкоригуйте мікроклімат. ';
                        END

                        IF LEN(@Recommendation) = 0
                            SET @Recommendation = N'Мікроклімат у нормі.';

                        INSERT INTO ComfortRecommendations (RoomName, Recommendation)
                        VALUES (@Room, @Recommendation);

                        FETCH NEXT FROM cur INTO @Room, @Temp, @Humidity, @Pressure, @Gas, @Light;
                    END

                    CLOSE cur;
                    DEALLOCATE cur;
                    DROP TABLE #Latest;
                END
            ");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AfterInsert_SensorData;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GenerateComfortRecommendations;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ComfortRecommendations;");
        }
    }
}
