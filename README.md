# Projekt-Dokumentation - DocumentManagementSystem

## Architektur-Entscheidungen

### Backend (.NET 8, ASP.NET Core)
- **.NET 8 & C#**: Moderne, performante Plattform mit guter UnterstÃ¼tzung fÃ¼r Web-APIs, Entity Framework und Docker. Team-Expertise vorhanden.
- **Code-First REST API**: Endpunkte werden im Team abgestimmt und direkt im Code definiert. Das ermÃ¶glicht schnelle Iteration und Anpassung.
- **Repository-Pattern & ORM (EF Core)**: Trennung von Business-Logik und Datenzugriff, bessere Testbarkeit und Wartbarkeit. PostgreSQL als DB wegen StabilitÃ¤t und Open-Source.
- **Unit-Tests mit Mocking**: xUnit und Moq, produktive DB wird fÃ¼r Tests gemockt. So werden Seiteneffekte vermieden und die FunktionalitÃ¤t gesichert.
- **Containerisierung (docker-compose)**: Backend und Datenbank laufen als Container, Healthchecks und Umgebungsvariablen sind konfiguriert. Das Setup ist reproduzierbar und einfach zu deployen.

### Frontend (React, Web-UI)
- **React mit modernen Komponenten**: Flexibel, weit verbreitet, unterstÃ¼tzt schnelle UI-Entwicklung. Tailwind und Lucide-Icons sorgen fÃ¼r ein modernes, konsistentes Design.
- **nginx als Webserver**: Leichtgewichtig, performant, weit verbreitet. Trennung von Backend und UI ermÃ¶glicht unabhÃ¤ngige Entwicklung und Deployment.
- **Kommunikation per REST**: Die UI kommuniziert per HTTP mit dem REST-Server, API-Requests werden per Proxy weitergeleitet. Klare Trennung der Verantwortlichkeiten.
- **docker-compose fÃ¼r UI**: Die UI lÃ¤uft als eigener Container, Compose-File enthÃ¤lt jetzt drei Services (DB, Backend, UI). Modular und skalierbar.

---

## UI-Konzept und Interaktionen (Sprint 2)

Das Interface des Dokumentenmanagement Systems bietet drei Kernfunktionen:

### 1. Upload
- **Dokument hochladen**: Drag & Drop oder Dateiauswahl, Metadaten (Titel, Beschreibung, Tags) werden erfasst.
- **Verarbeitung**: Nach Upload erscheinen automatisch eine Vorschau und eine AI-Zusammenfassung (folgt).
  
![Upload_Mock](Doc/img/Upload_Mock.png)

### 2. Results
- **Ãœbersicht**: Alle Dokumente werden angezeigt, inkl. Name, Datum, Tags und AI-Zusammenfassung.
- **Suche**: Eingabe in die Suchleiste aktualisiert die Ansicht in Echtzeit.
- **Fuzzy-Toggle**: Erlaubt tolerante Suche (z.B. Tippfehler).
- **Filterfunktion**: Dokumenttypen kÃ¶nnen per Button ein-/ausgeblendet werden.

![Results_Mock](Doc/img/Results_Mock.png)

### 3. Manage
- **Dokumente suchen**: Suchfeld und Filter stehen zur VerfÃ¼gung.
- **Actions**: Dokumente kÃ¶nnen gelÃ¶scht oder aktualisiert werden.

![Manage_Mock](Doc/img/Manage_Mock.png)

### Interaktionen
- **Suche**: Echtzeit-Filterung, Fuzzy-Toggle fÃ¼r Tippfehler-Toleranz.
- **Upload**: Klick auf â€žSelect Fileâ€œ lÃ¤dt Beispiel-Dokument, nach Upload erscheinen Preview und Zusammenfassung.
- **Dokumentdetails**: Klick auf ein Ergebnis Ã¶ffnet die Vorschau. Tabs innerhalb der Vorschau:
    - **Summary**: Editierbar.
    - **Metadaten**: Titel, Autor, Tags editierbar.
    - **Activity Log**: Upload-Datum, Indexierung.

![Detail_Mock](Doc/img/Detail_Mock.png)

---

## Queues, Worker & Logging (Sprint 3)

### Ziele
- Integration von RabbitMQ als Messaging-System.  
- API sendet OCR-Nachricht nach Upload.  
- OCR-Worker empfÃ¤ngt und loggt Nachrichten (Proof-of-Concept).  
- Logging & Fehlertoleranz sicherstellen.  
- Keine HTTP-500 bei Messaging-Fehlern.

---

## RabbitMQ-Integration â€“ Technische Dokumentation

### ArchitekturÃ¼berblick
| Komponente | Rolle |
|-------------|-------|
| **Queue** | `ocr-queue` |
| **Publisher** | API (`DocumentsController â†’ RabbitMqService`) |
| **Consumer** | OCR-Worker (Konsolen-App / Container) |
| **Infrastruktur** | `docker-compose.yml` enthÃ¤lt `rabbitmq:3-management` (Ports 5672, 15672) |

---

### Ablauf beim Upload (technisch)

1. **Client â†’ API**  
   `POST /api/documents` (multipart/form-data) mit `file`, `title`, optional `description`, `tags`.

2. **Validierung**  
   `DocumentsController` prÃ¼ft `ModelState` â†’ bei Fehlern `400 ProblemDetails`.

3. **Business-Logik**  
   `DocumentService.CreateAsync`:
   - PrÃ¼ft Titel, Tags.
   - Ruft `ITagRepository.GetOrCreateAsync` auf.
   - Erstellt neues `Document`-Entity.

4. **Persistenz (Datenbank)**  
   - `IDocumentRepository.AddAsync(doc)` â†’ EF Core `SaveChangesAsync()`.  
   - Erfolgreiche Speicherung ist Voraussetzung fÃ¼r Queue-Publish.

5. **Datei speichern**  
   - Sicherer Name (`safeTitle_{DocumentId}.pdf`) in `files/` gespeichert.

6. **Nachricht in RabbitMQ senden**  
   - `RabbitMqService.SendOcrMessage(new { DocumentId, FileName })`.  
   - JSON serialisiert, `ocr-queue` deklariert, persistent publish.  
   - Publish-Fehler â†’ **nur Log**, kein HTTP-Fehler.

7. **Worker (Consumer)**  
   - Liest `ocr-queue`.  
   - Loggt Payload, fÃ¼hrt `BasicAck` aus.  
   - Proof-of-Concept (keine OCR-Verarbeitung in Sprint 3).  
   - SpÃ¤ter: OCR, Textspeicherung, Folge-Nachrichten.

---

## Logging Levels
| Level           | Einsatz                                                |
| --------------- | ------------------------------------------------------ |
| **Information** | Erfolgreiche High-Level Events (Upload, Queue Publish) |
| **Debug**       | Interne Schritte, z. B. Tag-AuflÃ¶sung                  |
| **Warning**     | Validierungswarnungen, Retry-Themen                    |
| **Error**       | AusnahmefÃ¤lle, Fehlersituationen                       |

---

## Sprint 4: OCR‑Worker, Garage (S3), RabbitMQ

- PDF‑Upload landet im **S3‑kompatiblen Object Store (Garage)**,
- es wird eine **Nachricht** an **RabbitMQ** gesendet,
- ein **OCR‑Worker** verarbeitet die Nachricht, führt OCR via **Ghostscript + Tesseract** aus,
- legt das **Text‑Ergebnis (.txt)** wieder im Object Store ab,
- Logging & Fehlverhalten sind nachvollziehbar (Ack/Nack, DLQ).

---

## Architekturüberblick
```
Client → REST‑API → (Garage S3: put PDF) → RabbitMQ: "ocr-queue" → OCR Worker
                                                       ↓
                                                  Garage S3: get PDF → OCR (gs + tesseract) → put TXT
```
**Wesentliche Ziele von Sprint 4**
- Separates **Worker‑Service** für OCR (eigenes Programm/Container)
- Integration von **Ghostscript** (Rendering) & **Tesseract** (Texterkennung)
- Persistenz der PDFs in **Garage** (S3‑kompatibel)
- **Queue‑Integration**: Upload triggert OCR asynchron
- Logs, Fehlerbehandlung, optional **Dead‑Letter‑Queue (DLQ)**


---

## Relevante Komponenten (Code)

### 1) Message Contract
```csharp
public sealed class OcrJob
{
    public Guid DocumentId { get; init; }
    public string S3Key { get; init; } = null!;                
    public string ContentType { get; init; } = "application/pdf";
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}
```
**Zweck:** Klares Schema für Nachrichten zwischen REST‑API und OCR‑Worker.  
**`S3Key`** bestimmt die genaue Position des Dokuments; **`DocumentId`** für Korrelation/Logs.

---

### 2) Publishing (REST‑Seite)
```csharp
public class RabbitMqService
{
    // ... ctor mit Host/User/Pass/Queue
    public void SendOcrMessage(object message)
    {
        var json = JsonSerializer.Serialize(message);
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Queue passiv prüfen (Topologie wird extern/zu einem anderen Zeitpunkt angelegt)
        try { channel.QueueDeclarePassive(_queueName); }
        catch { /* log warn; wir publizieren trotzdem */ }

        channel.BasicReturn += (_, args) => { /* log unroutable */ };
        var props = channel.CreateBasicProperties();
        props.Persistent = true;                 // DeliveryMode=2, bei durable Queue restart‑fest
        props.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "",                      // default exchange → routingKey == Queue
            routingKey: _queueName,
            mandatory: true,                    // löst BasicReturn bei Unzustellbarkeit aus
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(json));
    }
}
```
**Wichtig:** `Persistent=true`, `mandatory=true`, und **JSON** als Austauschformat.

---

### 3) OCR‑Worker – Options & Bootstrapping
```csharp
public sealed class RabbitOptions
{
    public string Host { get; set; } = "rabbitmq";
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Queue { get; set; } = "ocr-queue";
    public ushort Prefetch { get; set; } = 1;          // Back‑pressure
    public bool RequeueOnError { get; set; } = false;  // DLQ freundlich
}

public sealed class GarageS3Options
{
    public string Endpoint { get; set; } = "";        // z. B. http://garage:3900
    public string Region { get; set; } = "garage";    // muss zu garage.toml passen
    public string Bucket { get; set; } = "documents";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
```
**Worker‑Start (Program):**
- Lädt Konfiguration aus `/config/appsettings.json` und **ENV**
- Initialisiert **RabbitMQ** (durable Queue, Prefetch, optional DLQ)
- Initialisiert **S3‑Client** (Garage): `ServiceURL`, `ForcePathStyle=true`, `AuthenticationRegion="garage"`, `UseHttp` je nach Endpoint
- Loggt **Ghostscript**/**Tesseract**‑Versionen zur Kontrolle
- Startet asynchronen Konsumenten

---

### 4) OCR‑Engines (austauschbar)
**Interface:**
```csharp
public interface IOcrEngine
{
    Task<string> ExtractTextAsync(Stream pdf, CancellationToken ct = default);
}
```
**Variante A – CLI** (`CliOcrEngine`):
- `gs` (Ghostscript) rendert PDF → TIFF @ 300 dpi
- `tesseract` extrahiert Text (`-l deu+eng`), Ausgabe `stdout`
- temporäres Arbeitsverzeichnis wird bereinigt

**Variante B – Library** (`OcrEngine`):
- **Magick.NET** liest PDF‑Seiten @ 300 dpi (Ghostscript zur Laufzeit nötig)
- **Tesseract .NET** verarbeitet die Bilder im Prozess

**Vorteil:** Austauschbarkeit (Dev/Prod/CI), gute Testbarkeit.

---

### 5) Orchestrierung im Worker (`OcrJobHandler`/Main)
- **Download** PDF aus Garage (S3) → Stream
- **OCR** auf dem Stream → String
- **Upload** Ergebnis als `<key>.txt` (UTF‑8, `UseChunkEncoding=false`, `ContentLength` gesetzt)
- **Ack/Nack** der RabbitMQ‑Message (bei Fehler optional in DLQ)

---

## Technische Entscheidungen
- **Storage**: Garage (S3) Single-Node, persistente Volumes.
- **S3-Client**: Path-Style, Region „garage“, kein Chunked, ContentLength setzen.
- **Message**: OcrJob mit DocumentId + S3Key; Ergebnis als gleiches Key + .txt.
- **Publisher (Rabbit)**: Default-Exchange, persistent, mandatory=true, Topologie nicht erstellen.
- **Worker (Rabbit)**: Durable Queue, Prefetch=1, manual ack, Fehler → DLQ.
- **OCR**: Ghostscript @300 dpi → Tesseract (eng+deu); Alternative: Magick.NET + Tesseract.
- **Config/Logs**: appsettings + ENV Overrides; Versionen von gs/tesseract loggen.
- **Fehler/Resource**: 404 speziell behandeln, Temp-Ordner aufräumen, Streams disposen.
- **Security**: Secrets über ENV/Docker-Secrets, nicht im Repo.
- **Build**: .NET 8, RabbitMQ.Client 6.6.0, kein Trimming/AOT, Pakete konsistent.

---

## Sicherheit & Betrieb
- **Secrets** (Access/Secret, `rpc_secret`) **nie ins Repo**; via ENV/Docker‑Secrets injizieren.
- **Persistenz**: Garage‑Volumes für `meta` und `data` mounten; Backups einplanen.
- **TLS**: In Produktion `https://` für Garage und RabbitMQ nutzen.

---

## Nächste Schritte (Folgesprints)
- **Gen‑AI Worker**: OCR‑Text an LLM → **Zusammenfassung** speichern (Sprint 5)
- **Elasticsearch**: Indexierung des OCR‑Textes & Such‑Use‑Case (Sprint 6)
- **Integrationstests & Batch**: End‑to‑End Test, XML‑Batch für Zugriffsstatistiken (Sprint 7)

---

## Sprint 5: Generative AI-Integration

In Sprint 5 wurde das bestehende Dokumentenmanagementsystem um **Generative AI support** mittels **Google Gemini** erweitert.  

Ziel war es, dass nach dem Upload eines Dokuments und der OCR-Verarbeitung automatisch:

1. der extrahierte Text an den GenAI-Dienst (Google Gemini) gesendet wird,
2. ein automatisch generiertes **Summary** zurückkommt,
3. dieses Summary in der **Datenbank** gespeichert wird und
4. über **REST-API** und **UI** verfügbar ist.

Zusätzlich wurden in diesem Sprint Logging, Fehlerbehandlung sowie Docker-Integration für alle neuen Komponenten ergänzt.


### Umfang

- Erweiterung von `docker-compose.yml` um den neuen **GenAI-Worker-Service**
- Neues Projekt: **`DocumentManagementSystem.GenAI_Worker`**
- Erweiterung der **Dokumenten-Verarbeitungskette**:
  - Dokument-Upload
  - Speicherung in Garage (S3-kompatibel)
  - OCR-Worker extrahiert Text
  - GenAI-Worker sendet OCR-Text an **Google Gemini**
  - Das generierte Summary wird in der Datenbank gespeichert
- Erweiterung der **REST-API**:
  - Summary wird im Document-DTO zurückgegeben
  - Endpoint zum Aktualisieren von Metadaten (Titel, Tags, Summary)
- Integration von **Serilog** für strukturiertes Logging
- Verbesserte **Fehlerbehandlung** bei:
  - Externen API-Fehlern (Gemini)
  - Datenbankzugriffen
  - S3-Kommunikation
  - Worker-Prozessen

---

### Architektur

```mermaid
flowchart LR
    UI[React UI<br/>DocumentManagementSystem.UI]
    API[REST API<br/>DocumentManagementSystem.API]
    DB[(PostgreSQL)]
    S3[(Garage S3)]
    MQ[(RabbitMQ)]
    OCR[OCR Worker]
    GENAI[GenAI Worker<br/>Google Gemini Integration]
    GEMINI[(Google Gemini API)]

    UI --> API
    API --> DB
    API --> S3
    API --> MQ

    MQ --> OCR
    OCR --> S3
    OCR --> DB

    GENAI --> DB
    GENAI --> GEMINI
    GEMINI --> GENAI

```

```mermaid
sequenceDiagram
    participant UI as Web UI
    participant API as REST API
    participant MQ as RabbitMQ
    participant S3 as Garage S3
    participant OCR as OCR Worker
    participant DB as PostgreSQL
    participant GA as GenAI Worker
    participant Gemini as Google Gemini

    UI->>API: Upload Document
    API->>S3: Store File
    API->>DB: Insert Document Metadata
    API->>MQ: Publish OCR Job

    MQ->>OCR: Deliver Job
    OCR->>S3: Download Document
    OCR->>OCR: Perform OCR
    OCR->>DB: Save Extracted Text (ocr_text)
    OCR->>DB: Mark OCR_Completed = true

    GA->>DB: Query documents WHERE summary is null AND ocr_text is not null
    DB-->>GA: Return next document

    GA->>Gemini: Send OCR Text
    Gemini-->>GA: Return AI Summary

    GA->>DB: Save Summary (summary field)

    UI->>API: Request document details
    API->>DB: Fetch including Summary
    DB-->>API: Return full document DTO
    API-->>UI: Display Summary
```

### Komponenten

#### **DocumentManagementSystem.API**
- ASP.NET Core REST API  
- Funktionen:
  - Dokument-Upload
  - Auflisten von Dokumenten
  - Aktualisieren von Metadaten (Titel, Tags, Summary)
  - Bulk-Löschen
- Summary wird im Document-DTO ausgegeben.

#### **OCR_Worker**
- Konsumiert Nachrichten aus RabbitMQ (`ocr-queue`)
- Lädt Dokumente aus Garage (S3)
- Führt OCR auf PDF/PNG/JPG durch
- Speichert extrahierten Text in der Datenbank
- Markiert Dokumente als *OCR abgeschlossen*

#### **GenAI_Worker (`DocumentManagementSystem.GenAI_Worker`)**
- **Neuer Worker in Sprint 5**
- Periodisches Polling der Datenbank:
  - Dokumente mit OCR-Text  
  - aber ohne Summary
- Sendet den Text an **Google Gemini**
- Speichert die generierte Zusammenfassung in der Datenbank

#### **UI – React / Vite / Tailwind**
- Neues Panel für **„AI Summary“**
- Editierbare Felder für:
  - Titel  
  - Tags  
  - AI-Zusammenfassung  
- Unterstützt Bulk-Aktionen wie Sammellöschen

#### **Infrastruktur**
- PostgreSQL  
- RabbitMQ  
- Garage (S3-kompatibel)  
- Docker Compose für Orchestrierung

---

### GenAI-Integration / Google Gemini

### Konfiguration

Konfiguration erfolgt über `appsettings.json` (ohne Secrets) und Umgebungsvariablen.

#### `appsettings.json` (Auszug)

```json
"Gemini": {
  "ApiKey": "",
  "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
  "Model": "models/gemini-2.5-flash"
}
```
---
# Sprint 6 -  ELK, Use Cases
 

## Architektur / Verarbeitungspipeline 

1. **Upload**
   - `DocumentService.CreateAsync(...)` speichert:
     - Dokument-Metadaten in PostgreSQL
     - PDF nach Garage S3 als `{DocumentId}.pdf`
   - OCR Queue ist in deinem aktuellen Code teilweise auskommentiert (CreateAsync enthält einen auskommentierten MQ-Teil).

2. **OCR**
   - OCR schreibt Ergebnis in `Documents.OcrText` (und optional `OcrCompletedAt`)

3. **GenAI Worker (Gemini)**
   - Worker zieht „fällige“ Dokumente aus der DB:
     - `OcrText != null`
     - Summary/Metadata/Embedding fehlt
     - Retry-Backoff beachtet (`AiNextAttemptAt`, `AiAttempts`)
   - Schritte:
     1) Summary erzeugen (`GenerateSummaryAsync`)
     2) Metadata + Entities extrahieren (`ExtractMetadataAsync`)
     3) Auto-Tags hinzufügen (`AddTagIfMissingAsync`)
     4) Embedding erzeugen (`GenerateEmbeddingAsync`)
   - Wenn alles fertig:
     - `AiProcessedAt` gesetzt

4. **Elasticsearch Indexing (einmalig)**
   - Wenn `AiProcessedAt != null` und `IndexedAt == null`:
     - `doc.ToDocumentIndex()`
     - `IndexDocumentAsync(...)`
     - `IndexedAt` wird gesetzt

---

## Elasticsearch

### Index Model (`DocumentIndex`)
Index: **`documents`**

Felder:
- `Id` (Document.Id als String)
- `Title`
- `Content` = OCR Text (`OcrText`)
- `Summary`
- `Tags` (Tag-Namen)
- `UploadedAt` (CreatedAt)

Mapping passiert hier:
- `DocumentIndexMapper.ToDocumentIndex(Document document)`

Indexing passiert hier:
- `SearchIndexService.IndexDocumentAsync(...)`

---

## Unique Feature 
### 1) Automatische Metadatenextraktion (KI)
Aus `OcrText` wird automatisch extrahiert und in **`DocumentMetadata`** gespeichert:
z.B.:
- Dokumenttyp: `INVOICE | CONTRACT | REMINDER | OTHER`
- IssueDate (DateOnly?)
- InvoiceNumber
- Iban
- RawJson (für Debugging)

**Wozu?**
- Dokumente sind sofort strukturiert (Filter/Anzeige ohne manuelle Eingabe)

### 2) Entities (Personen/Firmen/Orte)
KI extrahiert Listen und speichert sie in **`ExtractedEntities`**:
- `Type`: PERSON / ORG / LOCATION
- `Value`: erkannter Wert

**Wozu?**
- Du kannst später nach Firma/Person/Ort filtern oder im UI anzeigen

### 3) Intelligentes Auto-Tagging
Basierend auf Metadaten/Entities/Keywords werden Tags automatisch ergänzt:
- `type:INVOICE`
- `year:2025`
- `Q1-2025`
- `org:<name>`
- `kw:<keyword>`

**Wozu?**
- Einheitliche Tags ohne manuelles Tippen
- Schnelles Filtern in der UI

### 4) Embeddings
Für jedes Dokument wird ein Embedding erstellt und in **`DocumentEmbedding`** gespeichert:
- `Model`
- `Dims`
- `VectorJson`

**Wozu?**
- Basis für “Ähnliche Dokumente” / semantische Suche (auch wenn Wörter unterschiedlich sind)

---

## Datenbank-Status
### Tabelle `Documents` enthält u.a.:
- `Title`, `Description`, `CreatedAt`, `UpdatedAt`
- `OcrText`, `OcrCompletedAt`
- `Summary`, `AiProcessedAt`
- `IndexedAt`
- Backoff/Retry: `AiNextAttemptAt`, `AiAttempts`, `AiLastError`

### Neue Tabellen:
- `DocumentMetadatas` (1:1)
- `ExtractedEntities` (1:n)
- `DocumentEmbeddings` (1:1)
- `Tags` + Join Tabelle `DocumentTag`

---

## API DTO Output
`DocumentResponseDto` enthält:
- `OcrText`, `Summary`
- `Metadata` (`DocumentMetadataDto`)
- `Entities` (`ExtractedEntityDto`)
- `Embedding` (`DocumentEmbeddingDto`)

Damit ist euer Unique Feature direkt sichtbar in der API Response.

---
## Sprint 7: Integrationstests & Batch-Processing

In der finalen Phase wurde das System um automatisierte Qualitätskontrollen und eine Schnittstelle für externe Statistikdaten erweitert.

### 1. Integration-Tests (End-to-End)
Um die korrekte Funktion der gesamten Kette sicherzustellen, wurde ein Integration-Test für den Use-Case **"Document Upload"** implementiert.

* **Test-Ablauf**: 
    1.  Simulation eines Datei-Uploads über die REST-API.
    2.  Validierung der Persistenz in der **PostgreSQL** Datenbank.
    3.  Prüfung der Speicherung im **Garage S3** Object Store.
    4.  Verifizierung, dass eine Nachricht an die **RabbitMQ** Queue gesendet wurde.
* **Ausführung**:
    ```bash
    dotnet test DocumentManagementSystem.Tests
    ```

### 2. Batch-Processing Service (Daily Access Logs)
Ein neuer, zeitgesteuerter Dienst wurde integriert, um Zugriffszahlen aus externen Systemen via XML-Dateien zu importieren.

* **Funktionsweise**:
    * Der Dienst scannt ein konfigurierbares Eingangsverzeichnis nach neuen XML-Dateien.
    * Extraktion der Zugriffsdaten pro Dokument-ID.
    * Aktualisierung der `DailyAccessCount` in der PostgreSQL Datenbank.
    * Erfolgreich verarbeitete Dateien werden in einen Archiv-Ordner verschoben, um doppelte Verarbeitungen zu verhindern.
* **Zeitsteuerung**: Der Prozess ist standardmäßig so konfiguriert, dass er täglich um **01:00 Uhr** ausgeführt wird.

#### Beispiel für das XML-Format (`access_log.xml`):
```xml
<AccessStatistics date="2026-01-25">
    <Entry>
        <DocumentId>550e8400-e29b-41d4-a716-446655440000</DocumentId>
        <AccessCount>42</AccessCount>
    </Entry>
    <Entry>
        <DocumentId>6ba7b810-9dad-11d1-80b4-00c04fd430c8</DocumentId>
        <AccessCount>15</AccessCount>
    </Entry>
</AccessStatistics>
```
--- 
### Finalisierung & Code-Review Vorbereitung

Das Projekt erfüllt alle Kriterien der Sprints 1 bis 7:
* Unit Tests & Coverage: Abdeckung der Business-Logik und Worker-Dienste (> 70%).
* Logging & Error Handling: Durchgängige Implementierung von Layer-spezifischen Exceptions und strukturiertem Logging.
* Containerisierung: Alle Dienste (API, UI, OCR-Worker, GenAI-Worker, Elasticsearch, RabbitMQ, DB, Garage) sind in der `docker-compose.yml` orchestriert.

### HOWTO: System-Validierung
1. Starte das System via `docker compose up -d`.
2. Lade eine PDF-Datei (`HelloWorld.pdf`) über das Frontend hoch.
3. Prüfe in der UI, ob die KI-Zusammenfassung erscheint.
4. Suche nach dem Begriff "Hello", um die Elasticsearch-Indizierung zu bestätigen.
5. Lege eine XML-Testdatei in den konfigurierten Batch-Ordner, um die Statistik-Aktualisierung zu triggern.



