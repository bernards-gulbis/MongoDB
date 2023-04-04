using MongoDB.DataAccess;
using MongoDB.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ChoreDataAccess>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.MapGet("/users", async (ChoreDataAccess db) =>
    await db.GetAllUsers());

app.MapPut("/users", async (UserModel user, ChoreDataAccess db) =>
    await db.CreateUser(user));

app.Run();
