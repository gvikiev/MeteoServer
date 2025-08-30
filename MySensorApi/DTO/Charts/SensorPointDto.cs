namespace MySensorApi.DTO.Charts
{
    public enum TimeBucket { Raw = 0, Hour = 1, Day = 2 }

    public record SensorPointDto(
        DateTime TimestampUtc,
        int? Temperature,
        int? Humidity
    );
}
