namespace Itm.Event.Api.Dtos
{
    public record EventItmDto(int EventId, string Name, decimal BasePrice, int Quantity);
    //public record ReserveDto(int EventId, int Quantity);
}
