#if MacOs
import Foundation
import AppKit
import Quartz

@objc public class MacMonitor: NSObject {
    static var eventTap: CFMachPort?
    static var lastInteraction: [String: Any] = [:]
    static var lastLogTime: Date = Date()

    static func writeInteractionJSON(eventType: String, keyCode: Int?) {
        let timestamp = ISO8601DateFormatter().string(from: Date())
        let monitor = "MonitorPrincipal" // Puedes mejorar esto si tienes info real
        let app = NSWorkspace.shared.frontmostApplication?.localizedName ?? "Desconocido"

        var dict: [String: Any] = [
            "Timestamp": timestamp,
            "Monitor": monitor,
            "App": app,
            "EventType": eventType
        ]
        if let keyCode = keyCode {
            dict["KeyCode"] = keyCode
        } else {
            dict["KeyCode"] = NSNull()
        }

        // Carpeta Logs en el directorio actual
        let logsDir = FileManager.default.currentDirectoryPath + "/Logs"
        try? FileManager.default.createDirectory(atPath: logsDir, withIntermediateDirectories: true)

        // Nombre de archivo único
        let safeTimestamp = timestamp.replacingOccurrences(of: ":", with: "-")
        let filename = "\(logsDir)/log_\(safeTimestamp)_\(eventType).json"

        // Serializa y guarda
        if let data = try? JSONSerialization.data(withJSONObject: dict, options: [.prettyPrinted]) {
            try? data.write(to: URL(fileURLWithPath: filename))
            print("Log guardado: \(filename)")
        }
    }
}

@_cdecl("startMonitoring")
public func startMonitoring() {
    let mask: CGEventMask =
        (1 << CGEventType.keyDown.rawValue) |
        (1 << CGEventType.leftMouseDown.rawValue) |
        (1 << CGEventType.rightMouseDown.rawValue)

    MacMonitor.eventTap = CGEvent.tapCreate(
        tap: .cgSessionEventTap,
        place: .headInsertEventTap,
        options: .defaultTap,
        eventsOfInterest: mask,
        callback: { (proxy, type, event, refcon) -> Unmanaged<CGEvent>? in
            let now = Date()
            var shouldLog = false

            switch type {
            case .keyDown:
                let keyCode = event.getIntegerValueField(.keyboardEventKeycode)
                MacMonitor.lastInteraction = [
                    "eventType": "Tecla",
                    "keyCode": keyCode
                ]
                shouldLog = true
            case .leftMouseDown:
                MacMonitor.lastInteraction = [
                    "eventType": "RatonIzq",
                    "keyCode": NSNull()
                ]
                shouldLog = true
            case .rightMouseDown:
                MacMonitor.lastInteraction = [
                    "eventType": "RatonDer",
                    "keyCode": NSNull()
                ]
                shouldLog = true
            default:
                break
            }

            // Guarda cada 30 segundos si hubo interacción
            if shouldLog && now.timeIntervalSince(MacMonitor.lastLogTime) >= 30 {
                let eventType = MacMonitor.lastInteraction["eventType"] as? String ?? "Desconocido"
                let keyCode = MacMonitor.lastInteraction["keyCode"] as? Int
                MacMonitor.writeInteractionJSON(eventType: eventType, keyCode: keyCode)
                MacMonitor.lastLogTime = now
            }

            return Unmanaged.passUnretained(event)
        },
        userInfo: nil
    )

    if let eventTap = MacMonitor.eventTap {
        let runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, eventTap, 0)
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: eventTap, enable: true)
        print("Monitorización iniciada. Pulsa Ctrl+C para salir.")
        CFRunLoopRun()
    } else {
        print("No se pudo crear el event tap. ¿Tiene permisos de accesibilidad la app?")
    }
}
#endif