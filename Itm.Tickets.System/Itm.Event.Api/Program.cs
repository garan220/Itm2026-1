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

var EventDb = new List<EventItmDto>
{
    new (1, "Concierto ITM", 50000, 100)
};

app.MapGet("/api/events/{id}", (int id) =>
{
    var eventItm = EventDb.FirstOrDefault(p => p.EventId == id);

    return eventItm is not null ? Results.Ok(eventItm) : Results.NotFound();
})

.WithName("GetEvent")
.WithOpenApi();

app.Run();