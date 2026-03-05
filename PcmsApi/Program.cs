using MySqlConnector;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// 1. Tambahkan CORS agar Frontend (Vue.js) nanti diizinkan mengambil data
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// Konfigurasi Database Laragon
string dbConnection = "Server=localhost;User ID=root;Password=;Database=pcms_iot;";

// ==============================================================================
// ENDPOINT 1: Mengambil Status Mesin Paling Terakhir (Real-time Gauge)
// ==============================================================================
app.MapGet("/api/machine/{machineId}/current", async (string machineId) =>
{
    using var connection = new MySqlConnection(dbConnection);
    string query = @"
        SELECT timestamp, machine_id, status_code, status_text, voltage, ampere, operating_hours 
        FROM Machine_Telemetry_Log 
        WHERE machine_id = @Id 
        ORDER BY timestamp DESC 
        LIMIT 1";
    
    var result = await connection.QuerySingleOrDefaultAsync(query, new { Id = machineId });
    
    if (result == null) return Results.NotFound(new { message = "Data mesin tidak ditemukan." });
    return Results.Ok(result);
});

// ==============================================================================
// ENDPOINT 2: Mengambil 20 Data Terakhir (Untuk Grafik Garis / Trend)
// ==============================================================================
app.MapGet("/api/machine/{machineId}/history", async (string machineId) =>
{
    using var connection = new MySqlConnection(dbConnection);
    string query = @"
        SELECT timestamp, ampere, voltage 
        FROM Machine_Telemetry_Log 
        WHERE machine_id = @Id 
        ORDER BY timestamp DESC 
        LIMIT 20";
    
    var results = await connection.QueryAsync(query, new { Id = machineId });
    return Results.Ok(results);
});

Console.WriteLine("🚀 PCMS Backend API Berjalan...");
app.Run();