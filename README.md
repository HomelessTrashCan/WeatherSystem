# README - WeatherSystem

## Übersicht

Das WeatherSystem ist eine moderne, verteilte .NET 9-Anwendung zur Simulation und Visualisierung von Wetterdaten in Echtzeit. Das System besteht aus verschiedenen Komponenten, die über gRPC miteinander kommunizieren und eine realistische Wettersimulation mit Tag/Nacht-Zyklus ermöglichen. Die Anwendung wurde entwickelt, um die Verwendung moderner .NET-Technologien wie Worker Services, Blazor und gRPC zu demonstrieren.
Architektur
## Kernkomponenten

## 1. WeatherSystem.DomainCore
Die zentrale Bibliothek, die die Kernlogik der Wettersimulation enthält:
•	BusinessLogic: Enthält die Sensorklassen (Temperatur, Luftdruck, Feuchtigkeit) und die Simulationslogik
•	Infrastructure: Basisklassen und Hilfsklassen für die Infrastruktur
Die Sensoren nutzen intelligente Algorithmen, um realistische Wetterdaten zu erzeugen, die auch von der Tageszeit abhängen:
•	Temperatur: Wärmer am Tag, kühler in der Nacht
•	Luftfeuchtigkeit: Niedrigere Werte tagsüber, höher in der Nacht
•	Niederschlag: Höhere Wahrscheinlichkeit während der Nacht

## 2. WeatherSystem.Simulator
Ein Worker Service, der die Wettersimulation ausführt und Daten an den gRPC-Server sendet:
•	Nutzt BackgroundService für kontinuierliche Messungen
•	Konfigurierbare Parameter (Messintervall, Simulationsgeschwindigkeit)
•	Sendet Daten über den GrpcWeatherPublisher an den Server
•	Simuliert Tag/Nacht-Zyklen mit dem WeatherStationTimeProvider
Konfigurationsmöglichkeiten über appsettings.json:

{
  "WeatherPublisher": {
    "GrpcServiceUrl": "http://localhost:5043",
    "SimulatorId": "simulator-1"
  },
  "Simulation": {
    "MeasurementIntervalMinutes": 15,
    "SimulationSpeedFactor": 10
  }
}

## 3. WeatherSystem.Grpc.Server
Der zentrale Server, der Wetterdaten empfängt und an Clients verteilt:
•	Implementiert den in weatherbroadcast.proto definierten gRPC-Service
•	Verwaltet Clientverbindungen und Datenströme
•	Speichert Messdaten optional in einer Datenbank für spätere Analysen


## 4. WeatherSystem.Client
Konsolenbasierter Client zur Anzeige der Wetterdaten:
•	Verbindet sich mit dem gRPC-Server über Streaming
•	Zeigt Wetterdaten mit farbcodierter Formatierung an
•	Reagiert auf Verbindungsprobleme und Fehler


## 5. WeatherSystem.WebApp
Eine Blazor-basierte Webanwendung zur grafischen Darstellung der Wetterdaten:
•	Interaktive Diagramme und Visualisierungen
•	Echtzeit-Updates durch SignalR
•	Responsive Benutzeroberfläche


## 6. WeatherSystem.Node
Eine Erweiterung zur Integration von echten oder externen Wetterstationen:
•	Kann als Proxy für physische Wetterstationen dienen
•	Unterstützt bidirektionale gRPC-Kommunikation
•	Integriert sich nahtlos in das bestehende System

## 7. WeatherSystem.Tests
Enthält Tests für die verschiedenen Komponenten:
•	Unit-Tests für die Geschäftslogik
•	Integration-Tests für die gRPC-Kommunikation
•	Mock-basierte Tests für externe Abhängigkeiten

## Technologien
•	.NET 9: Nutzt die neuesten Features und Verbesserungen
•	gRPC: Hochperformante bidirektionale Kommunikation zwischen Diensten
•	Blazor: Moderne Weboberfläche mit C#
•	Worker Services: Für Hintergrundaufgaben und kontinuierliche Prozesse
•	Entity Framework Core: Zur Datenpersistenz (im Node-Projekt)
•	Dependency Injection: Für lose Kopplung und bessere Testbarkeit
•	OpenTelemetry: Für Monitoring und Tracing

## Starten der Anwendung

Das System kann auf verschiedene Arten gestartet werden:
### Über den AppHost (empfohlen)
Der WeatherSystem.AppHost orchestriert alle Komponenten und startet sie in der richtigen Reihenfolge:

dotnet run --project WeatherSystem.AppHost/WeatherSystem.AppHost.csproj

Dies startet:
•	Einen gRPC-Server
•	Einen Simulator
•	Mehrere Konsolen-Clients
•	Die Blazor-Webanwendung

### Komponenten einzeln starten
Alternativ können die Komponenten auch einzeln gestartet werden:
1.	Zuerst den gRPC-Server:
dotnet run --project WeatherSystem.Grpc.Server/WeatherSystem.Grpc.Server.csproj
2. 	Dann den Simulator:
dotnet run --project WeatherSystem.Simulator/WeatherSystem.Simulator.csproj
3. Danach die Konsolen-Clients:
dotnet run --project WeatherSystem.Client/WeatherSystem.Client.csproj
4.	Die Blazor-Webanwendung:
dotnet run --project WeatherSystem.WebApp/WeatherSystem.WebApp.csproj

## Konfiguration
Jede Komponente kann über ihre appsettings.json-Datei konfiguriert werden. Die wichtigsten Einstellungen sind:
•	Simulator:
•	Messintervall in Minuten
•	Simulationsgeschwindigkeit
•	Server-URL
•	Server:
•	Netzwerkeinstellungen
•	Datenbank-Verbindungsstring
•	Logging-Optionen
•	WebApp:
•	Server-URL
•	Aktualisierungsintervall
•	UI-Einstellungen

## Erweiterungen
Das System wurde mit Erweiterbarkeit im Blick entworfen:
•	Zusätzliche Sensoren: Neue Sensortypen können durch Ableiten von der Sensor-Basisklasse hinzugefügt werden
•	Externe Datenquellen: Durch Implementierung eines eigenen Publishers können Daten von externen Quellen eingespeist werden
•	Alternative Clients: Jede Anwendung, die gRPC unterstützt, kann sich als Client verbinden
•	Datenanalyse: Die persistenten Daten können für Wetteranalysen und -vorhersagen verwendet werden

## Tests und Qualitätssicherung
Die WeatherSystem.Tests-Sammlung enthält umfangreiche Tests für die Hauptfunktionalitäten:
•	GrpcWeatherPublisherTests: Überprüft die korrekte Funktion des Datenversands
•	SensorTests: Validiert die Genauigkeit und Plausibilität der generierten Wetterdaten
•	IntegrationTests: Testet das Zusammenspiel aller Komponenten


