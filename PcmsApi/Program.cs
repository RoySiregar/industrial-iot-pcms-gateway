using MySqlConnector;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// KONFIGURASI KEAMANAN (JWT AUTHENTICATION)
// ==============================================================================
var jwtSecretKey = "SistemPCMSIndustriSangatAmanSekali2026!@#"; 
var keyBytes = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

// Konfigurasi CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication(); 
app.UseAuthorization();  

string dbConnection = "Server=localhost;User ID=root;Password=;Database=pcms_iot;";

// ==============================================================================
// ENDPOINT 0: LOGIN & GENERATE TOKEN
// ==============================================================================
app.MapPost("/api/auth/login", ([FromBody] UserCredentials user) =>
{
    // LOGGING: Ini akan mencetak data yang masuk ke terminalmu!
    Console.WriteLine($"\n[Auth Guard] Ada yang mencoba login...");
    Console.WriteLine($"[Auth Guard] Username ditangkap: '{user?.Username}'");
    Console.WriteLine($"[Auth Guard] Password ditangkap: '{user?.Password}'");

    if (user != null && user.Username == "admin" && user.Password == "pcms2026")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Username) }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        Console.WriteLine("? [Auth Guard] Kredensial Valid. Token Diterbitkan!");
        return Results.Ok(new { token = tokenHandler.WriteToken(token), message = "Login Sukses" });
    }
    
    Console.WriteLine("? [Auth Guard] Akses Ditolak! Kredensial tidak cocok.");
    return Results.Unauthorized();
});

// ==============================================================================
// ENDPOINT 1 & 2: DATA TERKUNCI
// ==============================================================================
app.MapGet("/api/machine/{machineId}/current", async (string machineId) =>
{
    using var connection = new MySqlConnection(dbConnection);
    string query = "SELECT timestamp, machine_id, status_code, status_text, voltage, ampere, operating_hours FROM Machine_Telemetry_Log WHERE machine_id = @Id ORDER BY timestamp DESC LIMIT 1";
    var result = await connection.QuerySingleOrDefaultAsync(query, new { Id = machineId });
    if (result == null) return Results.NotFound();
    return Results.Ok(result);
}).RequireAuthorization(); 

app.MapGet("/api/machine/{machineId}/history", async (string machineId) =>
{
    using var connection = new MySqlConnection(dbConnection);
    string query = "SELECT timestamp, ampere, voltage FROM Machine_Telemetry_Log WHERE machine_id = @Id ORDER BY timestamp DESC LIMIT 20";
    var results = await connection.QueryAsync(query, new { Id = machineId });
    return Results.Ok(results);
}).RequireAuthorization(); 

Console.WriteLine("?? PCMS Backend API + Secure JWT Berjalan...");
app.Run();

// ==============================================================================
// MODEL DATA (Sekarang PUBLIC dan NULLABLE agar tidak diblokir .NET)
// ==============================================================================
public class UserCredentials 
{ 
    public string? Username { get; set; } 
    public string? Password { get; set; } 
}