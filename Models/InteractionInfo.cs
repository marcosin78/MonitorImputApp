using System;

namespace MonitorInputApp.Models
{
    public class InteractionInfo
    {
        public DateTime Timestamp { get; set; }
        public string? Monitor { get; set; }
        public string? App { get; set; }
        public string? EventType { get; set; } // "Tecla", "RatonIzq", "RatonDer"
        public int? KeyCode { get; set; }      // Solo para teclas
    }
}