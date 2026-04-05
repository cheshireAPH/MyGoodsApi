var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// HTTPS リダイレクトは最初に
app.UseHttpsRedirection();

// CORS は HTTPS の後、Controllers の前
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
