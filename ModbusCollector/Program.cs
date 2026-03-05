using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Device;
using MQTTnet;
using MQTTnet.Client;

namespace ModbusCollector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Konfigurasi Modbus
            string modbusIp = "127.0.0.1";
            int modbusPort = 502;

            // 2. Konfigurasi MQTT
            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateMqttClient();
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 1883) // Menggunakan public broker untuk testing
                .WithClientId("EdgeGateway_" + Guid.NewGuid().ToString())
                .Build();

            Console.WriteLine("Menghubungkan ke MQTT Broker...");
            await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);
            Console.WriteLine("✅ MQTT Broker Terhubung!");

            Console.WriteLine($"Menghubungkan ke Mesin Las Virtual di {modbusIp}:{modbusPort}...");

            try
            {
                using (TcpClient tcpClient = new TcpClient(modbusIp, modbusPort))
                {
                    ModbusIpMaster master = ModbusIpMaster.CreateIp(tcpClient);
                    Console.WriteLine("✅ Modbus Terhubung! Memulai data pipeline...\n");

                    // INISIALISASI RANDOM UNTUK EFEK FLUKTUASI MESIN
                    Random rnd = new Random();

                    // Loop terus-menerus
                    while (true)
                    {
                        try
                        {
                            // A. BACA DATA MODBUS
                            ushort[] registers = master.ReadHoldingRegisters(1, 0, 4);

                            int statusCode = registers[0];

                            // B. FORMAT DATA KE JSON (Standar IT/PCMS)
                            var payload = new
                            {
                                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                machine_id = "WELD-001",
                                status_code = statusCode,
                                status_text = statusCode == 2 ? "Welding" : (statusCode == 1 ? "Idle" : "Off"),
                                
                                // Berikan fluktuasi acak HANYA saat mesin berstatus Welding (2)
                                // Jika tidak, biarkan stabil di angka ModbusHD
                                voltage = statusCode == 2 ? registers[1] + rnd.Next(-2, 3) : registers[1],
                                ampere = statusCode == 2 ? registers[2] + rnd.Next(-5, 6) : registers[2],
                                
                                operating_hours = registers[3]
                            };

                            string jsonPayload = JsonSerializer.Serialize(payload);

                            // C. PUBLISH KE MQTT BROKER
                            string topic = "pcms/machinery/weld-001/telemetry";
                            var applicationMessage = new MqttApplicationMessageBuilder()
                                .WithTopic(topic)
                                .WithPayload(jsonPayload)
                                .Build();

                            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                            // Tampilkan log di console
                            Console.WriteLine($"[PUBLISHED] {topic}");
                            Console.WriteLine($"Payload: {jsonPayload}\n");

                            // Jeda 1 detik
                            await Task.Delay(1000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error membaca/mengirim data: {ex.Message}");
                            await Task.Delay(2000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Koneksi Gagal: {ex.Message}");
            }
        }
    }
}