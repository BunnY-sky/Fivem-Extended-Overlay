# FiveMOverlay Dashboard V2 – Sidebar und Spielerübersicht

**Wichtig, wenn du keinen Unterschied siehst:** Dieses ZIP hat `FiveMOverlay.csproj` direkt im Hauptordner. Entpacke es in einen neuen Ordner, baue es dort neu und starte anschließend ausschließlich `publish\FiveMOverlay.exe`. Eine alte EXE wird durch das Entpacken nicht aktualisiert.

Das Hauptfenster ist neu aufgebaut: linke Navigation mit **Home**, **Overlay 1–3**, **Shortcuts & Chat-Befehle**, **Overlay-Einstellungen** und **Vio-V Konto**. Home enthält Karten für **ID, Nutzername, Spielzeit, letzte Anmeldung, Registriert, Telefon, Fahrzeuglimit, Bewertung und Nickchanges**.

## Neu bauen
```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

Die fertige EXE wird unter `publish\FiveMOverlay.exe` erstellt.

---

# FiveM Overlay – 3 Overlays

Externes, transparentes Windows-Overlay für eigene Texte. Es schreibt nicht in den FiveM-Chat und verändert keine FiveM-Dateien.

## Funktionen
- Drei unabhängig konfigurierbare Overlays mit getrenntem Text, Position und Größe.
- Beim Anzeigen eines Overlays werden die anderen beiden automatisch ausgeblendet.
- Mit der Maus am Overlay-Körper verschieben; an der Ecke unten rechts skalieren.
- Einstellungen für alle drei Overlays werden unter `%LOCALAPPDATA%\FiveMOverlay\settings.json` gespeichert.
- Einstellungen aus der früheren Ein- und Zwei-Overlay-Version werden übernommen; Overlay 3 erhält Standardwerte, falls noch keine gespeichert sind.

## Globale Tasten
- `NUMPAD 1`: Overlay 1 anzeigen, Overlay 2 und 3 ausblenden.
- `NUMPAD 2`: Overlay 2 anzeigen, Overlay 1 und 3 ausblenden.
- `NUMPAD 3`: Overlay 3 anzeigen, Overlay 1 und 2 ausblenden.
- `NUMPAD 0`: Aktives Overlay ein-/ausblenden (wenn noch keines aktiv war, wird Overlay 1 angezeigt).

Die Tasten funktionieren, solange das Programm läuft. Falls eine Taste von einem anderen Programm belegt ist, kann Windows die Registrierung ablehnen; den Hinweis zeigt die GUI an.

## Build unter Windows
Voraussetzung: .NET 8 SDK.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

Die EXE wird in `publish\FiveMOverlay.exe` erstellt.


### Schriftgröße anpassen

Für jedes der drei Overlays gibt es im Hauptfenster einen eigenen Schieberegler für die Schriftgröße (12–72). Die Änderung wird sofort auf das jeweilige Overlay angewendet. Klicke auf „Einstellungen speichern“, damit Texte, Größen und Positionen beim nächsten Start erhalten bleiben.


### Overlay fixieren / Maus durchlassen
Aktiviere im Hauptfenster **„Overlays fixieren (Maus durchlassen)”**, damit das Overlay keine Mausbewegungen oder Klicks mehr abfängt und die Maussteuerung im Spiel bleibt. Zum Verschieben oder Skalieren die Fixierung wieder ausschalten. Die Einstellung wird mit „Einstellungen speichern” gespeichert.


### Textfarbe pro Overlay

Im Hauptfenster kann für jedes Overlay eine Textfarbe gewählt werden (Weiß, Rot, Grün, Blau, Gelb, Türkis, Orange, Pink oder Lila). Die Auswahl wird beim Speichern zusammen mit Text, Position, Größe und Schriftgröße gespeichert.


### Einzelne Wörter farblich markieren

Du kannst innerhalb des Textes einzelne Wörter oder Textabschnitte mit Farbtags einfärben. Die Tags werden im Overlay nicht angezeigt; nur der Text dazwischen erscheint in der gewünschten Farbe. Beispiel:

```text
Status: [gruen]ERLEDIGT[/gruen] - [rot]ACHTUNG[/rot] - [blau]INFORMATION[/blau]
```

Verfügbare Tags:
- `[weiß]Text[/weiß]` oder `[weiss]Text[/weiss]`
- `[rot]Text[/rot]`
- `[grün]Text[/grün]` oder `[gruen]Text[/gruen]`
- `[blau]Text[/blau]`
- `[gelb]Text[/gelb]`
- `[türkis]Text[/türkis]` oder `[turkis]Text[/turkis]`
- `[orange]Text[/orange]`
- `[pink]Text[/pink]`
- `[lila]Text[/lila]`

Du kannst mehrere Farben im selben Overlay verwenden. Der Text außerhalb der Tags nutzt weiterhin die normale Textfarbe, die für das jeweilige Overlay eingestellt ist.


### Fehlerbehebung: Prozess bleibt nach dem Schließen offen
Beim Schließen des Hauptfensters werden nun alle drei Overlay-Fenster explizit geschlossen, auch wenn sie gerade ausgeblendet sind. Zusätzlich beendet WPF die Anwendung beim Schließen des Hauptfensters. Dadurch sollte `FiveMOverlay.exe` nicht mehr als Hintergrundprozess hängen bleiben.


### Saubere Liste mit gleichmäßigen Zeilenabständen

Schreibe jeden Eintrag im Textfeld in eine eigene Zeile. Die Reihenfolge bleibt genau so, wie du sie eingibst; es wird nichts alphabetisch umsortiert. Die Anwendung normalisiert Windows-Zeilenumbrüche und verwendet einen einheitlichen Zeilenabstand von ungefähr 1,3 × Schriftgröße. Wortfarben funktionieren weiterhin auch innerhalb einzelner Zeilen. Beispiel:

```text
Name: Marek
Job: [gruen]Mechaniker[/gruen]
Status: [gelb]Bereit[/gelb]
Hinweis: [rot]Nicht verfügbar[/rot]
```


### Automatische Spaltenformatierung für Listen

Wenn die Textzeilen `|` als Trenner enthalten, zeigt das Overlay sie automatisch als echte, sauber ausgerichtete drei Spalten an: Sterne, Bezeichnung und Kürzel. Die Spalten werden wie in einer Tabelle ausgerichtet; es wird nicht mit eingefügten Leerzeichen gearbeitet. Du kannst deine vorhandenen Zeilen unverändert lassen, zum Beispiel:

```text
[gelb]★[/gelb] | Illegale Gegenstände | illstuff
[gelb]★[/gelb] | Verweigerung | bv -refusal
[gelb]★★[/gelb] | Geldautomatenraub | cash
```

Keine zusätzlichen Leerzeichen oder Leerzeilen nötig. Farb-Tags wie `[gelb]★[/gelb]` funktionieren weiterhin. Das Overlay vergrößert sich bei Bedarf, damit die Spalten lesbar bleiben.

Beispiel:
```text
[gelb]★[/gelb] | Illegale Gegenstände | illstuff
[gelb]★[/gelb] | Verweigerung | bv -refusal
[gelb]★★[/gelb] | Geldautomatenraub | cash
```

## Neuer Chat-Kurzbefehl `/br`

Wenn das FiveM-Spiel im Vordergrund ist, kannst du im Chat `/br` tippen und danach die Leertaste drücken. Das Programm ersetzt `/br` durch `/su  br` und setzt den Cursor zwischen die beiden Leerzeichen, sodass du den Spielernamen eintippen kannst. Es wird **nichts automatisch abgeschickt**. Danach musst du selbst Enter drücken.

Hinweis: Die Funktion läuft als Tastatur-Hook nur solange FiveMOverlay geöffnet ist und reagiert nur, wenn ein FiveM-Fenster im Vordergrund ist. Sie setzt voraus, dass im Chat-Eingabefeld nur `/br` steht, wenn du die Leertaste drückst.

### Eigene FiveM-Chat-Kurzbefehle

Im Hauptfenster **„Chat-Befehle konfigurieren“** öffnen. Jede Tabellenzeile enthält einen Auslöser und den Text, der beim Tippen des Auslösers plus Leertaste eingesetzt werden soll. Der Text wird nicht abgesendet.

Beispiel:

| Auslöser | Einfügetext |
|---|---|
| `/br` | `/su {cursor} br` |
| `/report` | `/report {cursor}` |
| `/cash` | `/su {cursor} cash` |

`{cursor}` ist optional und wird nicht als Text eingefügt. Der Cursor landet genau an dieser Stelle, sodass du den Namen oder weitere Angaben ergänzen kannst. Wenn du `{cursor}` weglässt, bleibt der Cursor am Ende des eingefügten Textes. Auslöser müssen mit `/` beginnen und dürfen keine Leerzeichen enthalten.

Die Kurzbefehle werden in `%LOCALAPPDATA%\FiveMOverlay\settings.json` gespeichert. Das Programm muss laufen und FiveM muss das aktive Vordergrundfenster sein. Nach dem Tippen des Auslösers die Leertaste drücken; Enter drückst du selbst.


## Vio-V Branding
The login and main GUI use the Vio-V-inspired charcoal/red palette. `VioVLogo.png` is bundled as a WPF resource for the login screen.


### Vio-V-Anmeldung und Kontofenster

Nach der erfolgreichen OAuth2-/PKCE-Anmeldung wird das Zugriffstoken für die laufende Sitzung im Arbeitsspeicher gehalten. Der Button **„Vio-V Konto öffnen“** verwendet dieses bereits erhaltene Token, um Profildaten zu laden; er startet nicht erneut den Browser-Login. Beim Beenden der Anwendung wird dieses Sitzungstoken nicht dauerhaft gespeichert.

### Einheitliches GUI-Design

Login, Hauptfenster und Chat-Befehlsdialog verwenden ein gemeinsames dunkles Anthrazit-Design mit Vio-V-Rot als Akzent. Die drei Overlays, ihre Einstellungen, Hotkeys und die konfigurierbaren Chat-Befehle bleiben erhalten.


## Neues Dashboard
- Linke Navigation mit Home, Overlay 1–3, Shortcuts/Chat-Befehlen und Einstellungen.
- Home zeigt ID, Nutzername, Spielzeit, letzte Anmeldung, Registrierungsdatum, Telefon, Fahrzeuglimit, Bewertung und Nickchanges. Die Felder werden aus den bekannten `/self`- und `/self/playingtime`-Antworten erkannt; nicht gelieferte Werte bleiben als „Nicht verfügbar“ sichtbar. Die angegebene ID 84814 wird als Fallback angezeigt, wenn die API keine ID zurückgibt.
- Die Overlay-Editoren werden einzeln über die linke Navigation geöffnet.


## Profilfelder: API-Verfügbarkeit

Die Home-Seite fragt jetzt zusätzlich `GET /self/nickchanges` ab und zählt Nickchange-Einträge, wenn der Endpunkt eine Liste liefert. Die Vio-V-Kontoseite zeigt die Rohantwort dieses Endpunkts ebenfalls in der technischen Diagnose. Die aktuell öffentlich auffindbare Vio-V-Dokumentation nennt die Berechtigung `read.self`, veröffentlicht dort aber keine bestätigten Feldnamen/Endpunkte für letzte Anmeldung, Registrierungsdatum, Telefonnummer, Fahrzeuglimit oder Bewertung. Diese Werte werden daher nicht erfunden; wenn sie in `/self` nicht enthalten sind, bleiben sie als „Nicht verfügbar“ stehen.


### Spielerübersicht (aktualisiert)

Die Home-Seite zeigt nur noch Spieler-ID, Nutzername, Spielzeit, Gruppe/Fraktion, Gruppenrang und Nickchanges. Die App fragt dafür `/self`, `/self/playingtime`, `/self/nickchanges`, `/group`, `/group/ranks` und `/group/members` ab. Die tatsächlich angezeigten Werte hängen davon ab, welche Felder die API für das Konto und die vorhandenen Berechtigungen zurückgibt. Nicht bestätigte Felder wie letzte Anmeldung, Registrierung, Telefonnummer, Fahrzeuglimit und Bewertung wurden aus der Übersicht entfernt.


### Spielzeitformatierung
Die Home-Seite zeigt numerische Spielzeitwerte als „X Stunden Y Minuten“ an. Wenn die API explizit Sekunden, Minuten oder Stunden benennt, wird diese Einheit berücksichtigt. Bei einer reinen Zahl ohne Einheit wird der Wert als Minuten interpretiert; das muss mit der tatsächlichen API-Antwort abgeglichen werden.
