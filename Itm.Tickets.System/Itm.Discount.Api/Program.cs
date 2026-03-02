using Itm.Discount.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var discountDb = new List<DiscountEventDto>
{
    new(1, "ITM50", 0.5m)
};

app.MapGet("/api/discount/{code}", (string code) =>
{
    var discount = discountDb.FirstOrDefault(p => p.CodeDiscount == code);

    return discount is not null ? Results.Ok(discount) : Results.NotFound();
})

.WithName("GetDiscountById")
.WithOpenApi();

app.Run();