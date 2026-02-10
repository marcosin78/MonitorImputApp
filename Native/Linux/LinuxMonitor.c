#if LINUX
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <string.h>
#include <unistd.h>

typedef struct {
    char timestamp[32];
    char monitor[64];
    char app[64];
    char eventType[32];
    int keyCode;
    int valid;
} InteractionInfo;

// Helper para obtener timestamp ISO8601
void get_timestamp(char* buffer, size_t size) {
    time_t now = time(NULL);
    struct tm* tm_info = localtime(&now);
    strftime(buffer, size, "%Y-%m-%dT%H:%M:%S", tm_info);
}

// Helper para obtener el nombre de la app activa (simplificado)
void get_active_app(char* buffer, size_t size) {
    strncpy(buffer, "Desconocido", size);
}

// Helper para obtener el nombre del monitor (simplificado)
void get_monitor_name(char* buffer, size_t size) {
    strncpy(buffer, "MonitorPrincipal", size);
}

// Escribe el JSON en la carpeta Logs
void write_interaction_json(const InteractionInfo* info) {
    char json[512];
    snprintf(json, sizeof(json),
        "{\n"
        "  \"Timestamp\": \"%s\",\n"
        "  \"Monitor\": \"%s\",\n"
        "  \"App\": \"%s\",\n"
        "  \"EventType\": \"%s\",\n"
        "  \"KeyCode\": %d\n"
        "}\n",
        info->timestamp, info->monitor, info->app, info->eventType, info->keyCode);

    char filename[128];
    snprintf(filename, sizeof(filename), "Logs/log_%s_%s.json", info->timestamp, info->eventType);

    for (int i = 0; filename[i]; ++i) {
        if (filename[i] == ':') filename[i] = '-';
    }

    FILE* f = fopen(filename, "w");
    if (f) {
        fputs(json, f);
        fclose(f);
        printf("Log guardado: %s\n", filename);
    } else {
        printf("No se pudo guardar el log.\n");
    }
}

void start_monitoring() {
    Display *display;
    Window root;
    XEvent event;
    InteractionInfo lastInteraction = {0};
    time_t lastLogTime = time(NULL);

    display = XOpenDisplay(NULL);
    if (display == NULL) {
        printf("No se pudo abrir la pantalla X11\n");
        return;
    }

    root = DefaultRootWindow(display);
    XSelectInput(display, root, KeyPressMask | ButtonPressMask);

    printf("Monitorización iniciada (Ctrl+C para salir)...\n");

    while (1) {
        // Espera por evento o timeout de 1 segundo
        if (XPending(display)) {
            XNextEvent(display, &event);
            if (event.type == KeyPress) {
                int keycode = event.xkey.keycode;
                get_timestamp(lastInteraction.timestamp, sizeof(lastInteraction.timestamp));
                get_monitor_name(lastInteraction.monitor, sizeof(lastInteraction.monitor));
                get_active_app(lastInteraction.app, sizeof(lastInteraction.app));
                strncpy(lastInteraction.eventType, "Tecla", sizeof(lastInteraction.eventType));
                lastInteraction.keyCode = keycode;
                lastInteraction.valid = 1;
                printf("Tecla presionada: %d\n", keycode);
            }
            if (event.type == ButtonPress) {
                int button = event.xbutton.button;
                get_timestamp(lastInteraction.timestamp, sizeof(lastInteraction.timestamp));
                get_monitor_name(lastInteraction.monitor, sizeof(lastInteraction.monitor));
                get_active_app(lastInteraction.app, sizeof(lastInteraction.app));
                const char* eventType = (button == 1) ? "RatonIzq" : (button == 3) ? "RatonDer" : "RatonOtro";
                strncpy(lastInteraction.eventType, eventType, sizeof(lastInteraction.eventType));
                lastInteraction.keyCode = 0;
                lastInteraction.valid = 1;
                printf("Botón del ratón presionado: %d\n", button);
            }
        }

        // Cada 30 segundos, guarda el último log si hubo interacción
        time_t now = time(NULL);
        if (now - lastLogTime >= 30) {
            if (lastInteraction.valid) {
                write_interaction_json(&lastInteraction);
                lastInteraction.valid = 0; // Solo guarda una vez por lapso
            } else {
                printf("No hubo interacción en este lapso de 30 segundos.\n");
            }
            lastLogTime = now;
        }
        usleep(100000); // Espera 0.1 segundos para evitar uso excesivo de CPU
    }

    XCloseDisplay(display);
}
#endif