using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja usług
builder.Services.AddControllers();
builder.Services.AddScoped<DbService>();

var app = builder.Build();

// Konfiguracja pipeline'u
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();