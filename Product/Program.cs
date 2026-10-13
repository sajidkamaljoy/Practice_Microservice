using EntityGraphQL.AspNet;
using JwtConfiguration;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using ProductService.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IProductRepo, ProductRepo>();

builder.Services.AddScoped<ProductMutation>();
builder.Services.AddScoped<ProductType>();
builder.Services.AddScoped<ProductQuery>();
builder.Services.AddScoped<ProductSchema>();

builder.Services.AddControllers();
builder.Services.AddCustomJwtAuth(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSwaggerGen();

// Add GraphQL server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("InMem"));
builder.Services.AddGraphQLSchema<AppDbContext>(config =>
{
    config.AddMutationsFrom<ProductMutation>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL<AppDbContext>(); // default url: /graphql
app.MapControllers();


PrepDb.PrepPopulation(app);

app.MapGraphQLPlayground();


app.Run();
