using Itm.Event.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var eventDb = new List<EventItmDto>
{
    new (1, "Concierto ITM", 50000, 100)
};

app.MapGet("/api/events/{id}", (int id) =>
{
    var eventItm = eventDb.FirstOrDefault(p => p.EventId == id);

    return eventItm is not null ? Results.Ok(eventItm) : Results.NotFound();
})

.WithName("GetEvent")
.WithOpenApi();

app.MapPost("/api/events/reduce", (ReserveDto request) =>
{
    var item = eventDb.FirstOrDefault(p => p.EventId == request.EventId);
    if (item is null) return Results.BadRequest("El evento no existe");
    if (item.chairsAvalaible < request.Quantity) return Results.BadRequest("No hay sillas suficientes");

    var index = eventDb.IndexOf(item);
    eventDb[index] = item with { chairsAvalaible = item.chairsAvalaible - request.Quantity };
    return Results.Ok(eventDb[index]);
});

app.MapPost("/api/event/release", (ReserveDto request) =>
{
    var item = eventDb.FirstOrDefault(p => p.EventId == request.EventId);
    if (item is null) return Results.NotFound();

    var index = eventDb.IndexOf(item);
    eventDb[index] = item with { chairsAvalaible = item.chairsAvalaible + request.Quantity };
    Console.WriteLine($"[COMPENSACION] se devolvieron {request.Quantity} sillas del evento {item.EventId}. Nuevo Numero de sillas {eventDb[index].chairsAvalaible}");
    return Results.Ok(new { Message = "Sillas liberadas por error en la transaccion", currentChairs = eventDb[index].chairsAvalaible });
});

app.Run();