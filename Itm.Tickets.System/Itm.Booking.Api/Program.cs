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

//app.MapGet("/api/booking/{id}/check-event", async (int id, IHttpClientFactory clientFactory) =>
//{
//    var client = clientFactory.CreateClient("EventClient");
//    try
//    {
//        var response = await client.GetAsync($"/api/event/{id}");
//        if (response.IsSuccessStatusCode)
//        {
//            var eventData = await response.Content.ReadFromJsonAsync<EventResponse>();
//            return Results.Ok(new {ProductId = id, })
//        }
//    }
//})

//paralelismo
app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    try
    {
        var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");
        var discountTask = eventClient.GetFromJsonAsync<DiscountDto>($"/api/discounts/{request.DiscountCode}");

        await Task.WhenAll(eventTask, discountTask);

        var eventData = eventTask.Result;
        var discountData = discountTask.Result;

        return Results.Ok(new
        {
            EventDetails = eventData,
            DiscountDetails = discountData,
            CalculatedAt = DateTime.UtcNow

        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error en el sistema: {ex.Message}");
    }
})

.WithName("GetEventSummary")
.WithOpenApi();

app.Run();


internal record EventDto(int EventId, string Name, decimal BasePrice, int chairs);
internal record DiscountDto(int id, string CodeDiscount, decimal Percent);
internal record BookingRequest(int EventId, string DiscountCode);