# FiveMOverlay

Windows-Overlay-Projekt auf Basis von C# und WPF (.NET 8).

## Funktionen

- Drei konfigurierbare Overlay-Seiten
- Globale Hotkeys (NumPad 0–3)
- Einstellbare Schriftgröße, Textfarbe und Overlay-Position
- Konfigurierbare Chat-Kurzbefehle
- Vio-V-Anmeldung über OAuth Authorization Code + PKCE

## Voraussetzungen

- Windows
- .NET 8 SDK (Windows Desktop / WPF)
- Für den Installer zusätzlich Inno Setup 6

## Projekt bauen

1. Repository herunterladen oder klonen.
2. PowerShell im Projektordner `FiveMOverlay` öffnen.
3. Ausführen:

   ```powershell
   dotnet restore .\FiveMOverlay.csproj
   dotnet build .\FiveMOverlay.csproj -c Release
   ```

Falls `build.ps1` im Projekt enthalten ist, kann es je nach lokaler PowerShell-Ausführungsrichtlinie separat gestartet werden. Prüfe das Skript vor dem Ausführen.

## GitHub-Release

Der Quellcode gehört in das Repository. Den fertigen Installer veröffentlichst du separat unter **Releases** als Release-Asset. Build-Ordner wie `bin/`, `obj/` und `publish/` müssen nicht in das Repository.

## Sicherheit und Zugangsdaten

Keine Access-Tokens, Passwörter, privaten Schlüssel, Signaturzertifikate oder persönlichen Konfigurationsdateien committen. Die Laufzeit-Einstellungen werden lokal im Benutzerprofil gespeichert. Prüfe alle API-/OAuth-Einstellungen vor einer öffentlichen Veröffentlichung.

## Hinweis

Dies ist Quellcode; ein erfolgreicher Build wurde nicht im Rahmen dieses Pakets garantiert. Vor der Veröffentlichung bitte lokal bauen und die Anwendung testen.
