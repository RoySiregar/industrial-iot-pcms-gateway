using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MySqlConnector;

namespace PcmsDataLogger
{
    class Program
    {
        // Konfigurasi Database bawaan Laragon
        static string connectionString = "Server=localhost;User ID=root;Password=;Database=pcms_iot;";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Mulai PCMS Data Logger (MQTT to MySQL) ===");

            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateMqttClient();
            
            // Koneksi ke Public Broker yang sama dengan aplikasi Modbus
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 1883)
                .WithClientId("PcmsLogger_" + Guid.NewGuid().ToString())
                .Build();

            // Event saat ada pesan MQTT masuk dari cloud
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"\n[MQTT Masuk] {payload}");

                try
                {
                    // 1. Parsing JSON dari pesan MQTT
                    using JsonDocument doc = JsonDocument.Parse(payload);
                    JsonElement root = doc.RootElement;

                    DateTime timestamp = root.GetProperty("timestamp").GetDateTime();
                    string machineId = root.GetProperty("machine_id").GetString();
                    int statusCode = root.GetProperty("status_code").GetInt32();
                    string statusText = root.GetProperty("status_text").GetString();
                    int voltage = root.GetProperty("voltage").GetInt32();
                    int ampere = root.GetProperty("ampere").GetInt32();
                    int operatingHours = root.GetProperty("operating_hours").GetInt32();

                    // 2. Simpan ke Database MySQL
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string query = @"INSERT INTO Machine_Telemetry_Log 
                                         (timestamp, machine_id, status_code, status_text, voltage, ampere, operating_hours) 
                                         VALUES (@timestamp, @machine_id, @status_code, @status_text, @voltage, @ampere, @operating_hours)";
                        
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@timestamp", timestamp);
                            command.Parameters.AddWithValue("@machine_id", machineId);
                            command.Parameters.AddWithValue("@status_code", statusCode);
                            command.Parameters.AddWithValue("@status_text", statusText);
                            command.Parameters.AddWithValue("@voltage", voltage);
                            command.Parameters.AddWithValue("@ampere", ampere);
                            command.Parameters.AddWithValue("@operating_hours", operatingHours);

                            await command.ExecuteNonQueryAsync();
                            Console.WriteLine("✅ [Database] Data berhasil disimpan ke MySQL!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [Error] Gagal memproses/menyimpan: {ex.Message}");
                }
            };

            // Mulai Koneksi dan Subscribe
            await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);
            Console.WriteLine("✅ Terhubung ke Broker EMQX!");

            var subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("pcms/machinery/weld-001/telemetry"))
                .Build();

            await mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
            Console.WriteLine("✅ Subscribed ke topik: pcms/machinery/weld-001/telemetry");
            Console.WriteLine("Menunggu data masuk dari mesin...\n");

            // Biarkan program terus mendengarkan di background
            await Task.Delay(Timeout.Infinite);
        }
    }
}