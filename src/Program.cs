using SizzlingHotProducts.Repositories;
using SizzlingHotProducts.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Today as per readme
var today = new DateOnly(2021, 7, 21);

var inputFolder = Path.Combine(builder.Environment.ContentRootPath, "..", "inputs");

builder.Services.AddSingleton<IOrderRepository>(_ => new OrderRepository(inputFolder));
builder.Services.AddSingleton<ISalesService>(sp =>
    new SalesService(sp.GetRequiredService<IOrderRepository>(), today));

var app = builder.Build();

app.MapControllers();

app.Run();
