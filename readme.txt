NMEA MSFS BRIDGE - README
=========================

Que hace este programa
----------------------
Este programa conecta Microsoft Flight Simulator (MSFS) con aplicaciones que entienden NMEA (por ejemplo XCSoar).

Funcionamiento resumido:
1) Lee telemetria de vuelo desde MSFS por SimConnect.
2) Convierte esa telemetria a sentencias NMEA compatibles.
3) Publica el flujo NMEA por TCP en 127.0.0.1:4353 (configurable).

Sentencias NMEA emitidas (modo compatibilidad actual):
- GPGGA
- GPRMC
- WIMWV (viento true)

Archivo de configuracion
------------------------
La configuracion se guarda en appsettings.json.

Ubicaciones habituales:
- Si ejecutas desde la carpeta de publicacion:
  dist\NmeaMsfsBridge-win-x64\appsettings.json
- Si ejecutas desde el proyecto:
  NmeaMsfsBridge.App\appsettings.json

Parametros importantes:
- Bridge.Telemetry.PollHz: frecuencia de lectura de telemetria
- Bridge.Output.TransmitHz: frecuencia de envio NMEA
- Bridge.Output.Host y Bridge.Output.Port: destino TCP (por defecto 127.0.0.1:4353)

Como inicializarlo (sin VS Code)
--------------------------------
1) Arranca Microsoft Flight Simulator.
2) Ve a la carpeta de distribucion:
   dist\NmeaMsfsBridge-win-x64
3) Ejecuta:
   NmeaMsfsBridge.App.exe
4) Deja la ventana abierta (si la cierras, el bridge se detiene).

Comprobacion rapida
-------------------
Al iniciar correctamente deberias ver mensajes como:
- Starting bridge...
- Telemetry service started...
- TCP output listening on 127.0.0.1:4353
- Connected to MSFS through SimConnect...

Integracion con XCSoar
----------------------
Configura un dispositivo en XCSoar como cliente TCP con:
- Host: 127.0.0.1
- Puerto: 4353
- Driver: Generic

Si no aparece viento en XCSoar, revisa:
- Que el dispositivo este conectado.
- Que la opcion de viento externo este habilitada en XCSoar.

Parar el programa
-----------------
- Cierra la ventana de consola del bridge.
- O pulsa Ctrl+C dentro de la consola.

Notas
-----
- Este ejecutable es standalone (no necesita abrir VS Code).
- Si cambias appsettings.json, reinicia el ejecutable para aplicar cambios.
