using Microsoft.EntityFrameworkCore;
using SehaNotebook.DAL.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//! Inject my own services
// => connect to postgres through the AppDbContext
builder.Services.AddEntityFrameworkNpgsql().AddDbContext<AppDbContext>(
    options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("conn")
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
