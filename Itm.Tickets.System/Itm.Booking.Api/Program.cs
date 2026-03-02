using Microsoft.AspNetCore.Mvc;

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
app.MapGet("/api/booking{id}/summary", async (int id, IHttpClientFactory factory) =>
{
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    try
    {
        var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");
    }
})


internal record EventDto(int EventId, string Name, decimal BasePrice, int chairs);