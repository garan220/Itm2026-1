using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5056");
})
.AddStandardResilienceHandler();

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/bookings/{id}/check-event", async (int id, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("EventClient");
    try
    {
        var response = await client.GetAsync($"/api/events/{id}");
        if (response.IsSuccessStatusCode)
        {
            var eventInfo = await response.Content.ReadFromJsonAsync<EventDto>();
            return Results.Ok(new { Event = id, EventInfo = eventInfo });
        }
        return Results.Problem($"El Evento respondio con error {response.StatusCode}");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error de conexion: {ex.Message}");
    }
})
    .WithName("CheckEventInfo")// nombre claro para la api
    .WithOpenApi();//Para que aparezca swagger

//paralelismo
app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    //Lectura en paralelo
    var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");

    //validar si existe el codigo de descuento
    DiscountDto? discountData = null;
    try
    {
        var discountTask = discountClient.GetFromJsonAsync<DiscountDto>($"/api/discount/{request.DiscountCode}");
        await Task.WhenAll(eventTask, discountTask);
        discountData = discountTask.Result;
    }
    catch
    {
        await eventTask;
    }
    var eventData = eventTask.Result;

    decimal discountAplied = discountData?.Percent ?? 0m;
    decimal total = (eventData.BasePrice * request.Tickets) * (1 - discountAplied);

    Console.WriteLine($"Total a cobrar: {total:C} (Descuento: {discountAplied * 100}%)");

    //Reservar sillas
    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve", new
    {
        EventId = request.EventId,
        Quantity = request.Tickets
    });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas suficientes o el evento no existe.");

    //Simulacion del pago
    try
    {
        bool paymentSuccess = new Random().Next(1, 10) > 5;
        if (!paymentSuccess) throw new Exception("Fondos insuficientes en la tarjeta de crédito.");

        return Results.Ok(new
        {
            Status = "Éxito",
            Message = "¡Disfruta el concierto ITM!",
            Factura = new
            {
                Evento = eventData.Name,
                Boletas = request.Tickets,
                TotalPagado = total,
                CodigoDescuento = discountData?.CodeDiscount ?? "Sin descuento"
            }

        });
    }
    catch (Exception ex)
    {
        //COMPENSACIÓN
        Console.WriteLine($"[SAGA] Error en pago: {ex.Message}. Liberando sillas...");

        await eventClient.PostAsJsonAsync("/api/events/release",
        new { EventId = request.EventId, Quantity = request.Tickets });

        return Results.Problem("Tu pago fue rechazado. No te preocupes, no te cobramos y tus sillas fueron liberadas.");
    }
})

    //este return deja ver si hay datos en eventData y discountData
    //return Results.Ok(new
    //    {
    //        EventDetails = eventData,
    //        DiscountDetails = discountData,
    //        CalculatedAt = DateTime.UtcNow

    //    });

.WithName("GetEventSummary")
.WithOpenApi();

app.Run();


//internal record EventResponse(int EventId, string Name, decimal BasePrice, int chairs);
internal record EventDto(int EventId, string Name, decimal BasePrice, int chairsAvailable);
internal record DiscountDto(string CodeDiscount, decimal Percent);
internal record BookingRequest(int EventId, int Tickets, string DiscountCode);