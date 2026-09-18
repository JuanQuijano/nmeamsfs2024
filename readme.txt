NMEA MSFS BRIDGE - README
=========================

What this program does
----------------------
This program connects Microsoft Flight Simulator (MSFS) with applications that understand NMEA (for example XCSoar).

Summary of operation:
1) Reads flight telemetry from MSFS through SimConnect.
2) Converts that telemetry into NMEA-compatible sentences.
3) Publishes the NMEA stream over TCP at 127.0.0.1:4353 (configurable).
4) Shows a status window with simulator connection, XCSoar clients and telemetry age.

NMEA sentences emitted (current compatibility mode):
- GPGGA
- GPRMC
- WIMWV (true wind)

Configuration file
------------------
The configuration is saved in appsettings.json.

Common locations:
- If you run from the publish folder:
  dist\NmeaMsfsBridge-win-x64\appsettings.json
- If you run from the project:
  NmeaMsfsBridge.App\appsettings.json

Important parameters:
- Bridge.Telemetry.PollHz: telemetry read frequency
- Bridge.Output.TransmitHz: NMEA transmission frequency
- Bridge.Output.Host and Bridge.Output.Port: TCP destination (default 127.0.0.1:4353)
- Bridge.StreamStability.Enabled: pauses NMEA while a new location is stabilised
- Bridge.StreamStability.GeographicJumpKilometers: distance that starts a clean stream restart (default 100)
- Bridge.StreamStability.StableSamplesRequired: complete samples required before resuming output (default 3)

How to start it (without VS Code)
---------------------------------
1) Start Microsoft Flight Simulator.
2) Go to the folder where you downloaded it:
3) Run:
   NmeaMsfsBridge.App.exe
4) Leave the window open (if you close it, the bridge stops).

Status window
-------------
The window shows whether MSFS is connected, the number of connected XCSoar TCP clients,
the configured frequencies and the output destination. It refreshes every second.
Closing the window also stops the bridge.

Quick check
-----------
When started correctly, you should see messages such as:
NMEA MSFS2024 XCSoar Bridge 1.0.1
Created by Juan Carlos Quijano Abad - 2026

Setting up the Bridge: TCP at 127.0.0.1:4353, at 5Hz tx and 5Hz telemetry.
Press Ctrl+C to close the application

Waiting for connection with the simulator
Connected to the simulator

Integration with XCSoar
-----------------------
Configure a device in XCSoar as a TCP client with:
- Host: 127.0.0.1
- Port: 4353
- Driver: Generic

If wind does not appear in XCSoar, check:
- That the device is connected.
- That the external wind option is enabled in XCSoar.

Stopping the program
--------------------
- Close the bridge console window.
- Or press Ctrl+C inside the console.

Notes
-----
- This executable is standalone (it does not need VS Code to be opened).
- If you change appsettings.json, restart the executable to apply the changes.

Publishing a new release
------------------------
Run these commands:
- git tag v1.0.2
- git push origin v1.0.2
