using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace MonitorInputApp.Models
{
    public static class LogManager
    {
        private static readonly string LogFolder = Path.Combine("Logs");

        public static void SaveInteraction(InteractionInfo info)
        {
            Directory.CreateDirectory(LogFolder);
            string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(LogFolder, fileName);
            string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public static void TrySendPendingLogs()
        {
            var files = Directory.GetFiles(LogFolder, "*.json");
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);

                // Simulación de envío al servidor
                bool sent = SendLogToServer(json);

                if (sent)
                {
                    File.Delete(file);
                }
                // Si no se envía, el archivo permanece para el siguiente intento
            }
        }

        // Simulación de envío al servidor (debes reemplazar por tu lógica real)
        private static bool SendLogToServer(string json)
        {
            try
            {
                // Aquí iría tu código real de envío HTTP
                // Por ahora, simula éxito:
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ProcessLogs()
        {
            Directory.CreateDirectory(LogFolder);
            var files = Directory.GetFiles(LogFolder, "*.json");
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                try
                {
                    var interaction = JsonSerializer.Deserialize<InteractionInfo>(json);
                    // Procesa el log como necesites (enviar, mostrar, etc.)
                    Console.WriteLine($"Log procesado: {interaction?.EventType} en {interaction?.App} ({interaction?.Timestamp})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando {file}: {ex.Message}");
                }
            }
        }
    }
}