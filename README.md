# README - WeatherSystem

�bersicht

Das WeatherSystem ist eine moderne, verteilte .NET 9-Anwendung zur Simulation und Visualisierung von Wetterdaten in Echtzeit. Das System besteht aus verschiedenen Komponenten, die �ber gRPC miteinander kommunizieren und eine realistische Wettersimulation mit Tag/Nacht-Zyklus erm�glichen. Die Anwendung wurde entwickelt, um die Verwendung moderner .NET-Technologien wie Worker Services, Blazor und gRPC zu demonstrieren.
Architektur
Kernkomponenten

1. WeatherSystem.DomainCore
Die zentrale Bibliothek, die die Kernlogik der Wettersimulation enth�lt:
�	BusinessLogic: Enth�lt die Sensorklassen (Temperatur, Luftdruck, Feuchtigkeit) und die Simulationslogik
�	Infrastructure: Basisklassen und Hilfsklassen f�r die Infrastruktur
Die Sensoren nutzen intelligente Algorithmen, um realistische Wetterdaten zu erzeugen, die auch von der Tageszeit abh�ngen:
�	Temperatur: W�rmer am Tag, k�hler in der Nacht
�	Luftfeuchtigkeit: Niedrigere Werte tags�ber, h�her in der Nacht
�	Niederschlag: H�here Wahrscheinlichkeit w�hrend der Nacht

2. WeatherSystem.Simulator
Ein Worker Service, der die Wettersimulation ausf�hrt und Daten an den gRPC-Server sendet:
�	Nutzt BackgroundService f�r kontinuierliche Messungen
�	Konfigurierbare Parameter (Messintervall, Simulationsgeschwindigkeit)
�	Sendet Daten �ber den GrpcWeatherPublisher an den Server
�	Simuliert Tag/Nacht-Zyklen mit dem WeatherStationTimeProvider
Konfigurationsm�glichkeiten �ber appsettings.json:

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

3. WeatherSystem.Grpc.Server
Der zentrale Server, der Wetterdaten empf�ngt und an Clients verteilt:
�	Implementiert den in weatherbroadcast.proto definierten gRPC-Service
�	Verwaltet Clientverbindungen und Datenstr�me
�	Speichert Messdaten optional in einer Datenbank f�r sp�tere Analysen


4. WeatherSystem.Client
Konsolenbasierter Client zur Anzeige der Wetterdaten:
�	Verbindet sich mit dem gRPC-Server �ber Streaming
�	Zeigt Wetterdaten mit farbcodierter Formatierung an
�	Reagiert auf Verbindungsprobleme und Fehler


5. WeatherSystem.WebApp
Eine Blazor-basierte Webanwendung zur grafischen Darstellung der Wetterdaten:
�	Interaktive Diagramme und Visualisierungen
�	Echtzeit-Updates durch SignalR
�	Responsive Benutzeroberfl�che


6. WeatherSystem.Node
Eine Erweiterung zur Integration von echten oder externen Wetterstationen:
�	Kann als Proxy f�r physische Wetterstationen dienen
�	Unterst�tzt bidirektionale gRPC-Kommunikation
�	Integriert sich nahtlos in das bestehende System

7. WeatherSystem.Tests
Enth�lt Tests f�r die verschiedenen Komponenten:
�	Unit-Tests f�r die Gesch�ftslogik
�	Integration-Tests f�r die gRPC-Kommunikation
�	Mock-basierte Tests f�r externe Abh�ngigkeiten

Technologien
�	.NET 9: Nutzt die neuesten Features und Verbesserungen
�	gRPC: Hochperformante bidirektionale Kommunikation zwischen Diensten
�	Blazor: Moderne Weboberfl�che mit C#
�	Worker Services: F�r Hintergrundaufgaben und kontinuierliche Prozesse
�	Entity Framework Core: Zur Datenpersistenz (im Node-Projekt)
�	Dependency Injection: F�r lose Kopplung und bessere Testbarkeit
�	OpenTelemetry: F�r Monitoring und Tracing

Starten der Anwendung

Das System kann auf verschiedene Arten gestartet werden:
�ber den AppHost (empfohlen)
Der WeatherSystem.AppHost orchestriert alle Komponenten und startet sie in der richtigen Reihenfolge:

dotnet run --project WeatherSystem.AppHost/WeatherSystem.AppHost.csproj

Dies startet:
�	Einen gRPC-Server
�	Einen Simulator
�	Mehrere Konsolen-Clients
�	Die Blazor-Webanwendung

Komponenten einzeln starten
Alternativ k�nnen die Komponenten auch einzeln gestartet werden:
1.	Zuerst den gRPC-Server:
dotnet run --project WeatherSystem.Grpc.Server/WeatherSystem.Grpc.Server.csproj
2. 	Dann den Simulator:
dotnet run --project WeatherSystem.Simulator/WeatherSystem.Simulator.csproj
3. Danach die Konsolen-Clients:
dotnet run --project WeatherSystem.Client/WeatherSystem.Client.csproj
4.	Die Blazor-Webanwendung:
dotnet run --project WeatherSystem.WebApp/WeatherSystem.WebApp.csproj

Konfiguration
Jede Komponente kann �ber ihre appsettings.json-Datei konfiguriert werden. Die wichtigsten Einstellungen sind:
�	Simulator:
�	Messintervall in Minuten
�	Simulationsgeschwindigkeit
�	Server-URL
�	Server:
�	Netzwerkeinstellungen
�	Datenbank-Verbindungsstring
�	Logging-Optionen
�	WebApp:
�	Server-URL
�	Aktualisierungsintervall
�	UI-Einstellungen

Erweiterungen
Das System wurde mit Erweiterbarkeit im Blick entworfen:
�	Zus�tzliche Sensoren: Neue Sensortypen k�nnen durch Ableiten von der Sensor-Basisklasse hinzugef�gt werden
�	Externe Datenquellen: Durch Implementierung eines eigenen Publishers k�nnen Daten von externen Quellen eingespeist werden
�	Alternative Clients: Jede Anwendung, die gRPC unterst�tzt, kann sich als Client verbinden
�	Datenanalyse: Die persistenten Daten k�nnen f�r Wetteranalysen und -vorhersagen verwendet werden

Tests und Qualit�tssicherung
Die WeatherSystem.Tests-Sammlung enth�lt umfangreiche Tests f�r die Hauptfunktionalit�ten:
�	GrpcWeatherPublisherTests: �berpr�ft die korrekte Funktion des Datenversands
�	SensorTests: Validiert die Genauigkeit und Plausibilit�t der generierten Wetterdaten
�	IntegrationTests: Testet das Zusammenspiel aller Komponenten


