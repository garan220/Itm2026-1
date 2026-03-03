namespace Itm.Event.Api.Dtos
{
    public record EventItmDto(int EventId, string Name, decimal BasePrice, int chairsAvailable);
    public record ReserveDto(int EventId, int Quantity);
}
