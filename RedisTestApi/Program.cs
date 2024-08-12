using Microsoft.EntityFrameworkCore;
using RedisTest.DataAccess;
using RedisTest.Entities;
using RedisTest.Service.Impl;
using RedisTest.Service.IService;
using RedisTest.Share;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var services = builder.Services;

services.AddLogging();

services.AddDbContextPool<RedisTestDbContext>(options => options.UseMySql(builder.Configuration["ConnectionStrings:MySql"], ServerVersion.Parse("8.0"), b => b.MigrationsAssembly("RedisTest.Api")));
// ×¢Èëredis
services.AddSingleton(new RedisHelper(builder.Configuration["ConnectionStrings:redis"], "RedisTest"));

// ×¢Èërepository
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// ×¢Èëservice
services.AddScoped<IUserService, UserService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
