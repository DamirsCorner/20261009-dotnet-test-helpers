using System.Configuration;
using EndpointMaxResponseTime.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRequestTimeouts();

builder.Services.AddSingleton<SubmissionsRepository>();
builder.Services.AddScoped<SubmissionsService>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration.GetValue<string>("BaseAddress")
            ?? throw new ConfigurationErrorsException("BaseAddress configuration value is missing.")
    );
});

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRequestTimeouts();
app.UseAuthorization();

app.MapControllers();

app.Run();
