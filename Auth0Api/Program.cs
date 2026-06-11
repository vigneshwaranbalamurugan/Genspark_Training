using Auth0.AspNetCore.Authentication.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuth0ApiAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.Audience = builder.Configuration["Auth0:Audience"];
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();


// Public endpoint - no authentication required
app.MapGet("/api/public", () => 
    Results.Ok(new { Message = "This endpoint is public" }))
    .WithName("GetPublic");

// Protected endpoint - requires authentication
app.MapGet("/api/private", () => 
    Results.Ok(new { Message = "This endpoint requires authentication" }))
    .RequireAuthorization()
    .WithName("GetPrivate");


app.Run();