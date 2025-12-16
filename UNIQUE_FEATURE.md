# Unique Feature – Dokumentenmanagementsystem (Vorschlag)

Für unser Dokumentenmanagementsystem planen wir ein **Unique Feature**, das über die Basisfunktionalität eines klassischen DMS hinausgeht. Wir möchten OCR-Texterkennung mit generativer KI (Google Gemini) und semantischer Vektorsuche kombinieren, um die Dokumentenverwaltung intelligent zu automatisieren.

---

## Unser geplantes Unique Feature

Unser System soll drei zentrale Funktionen bieten, die über Standard-DMS hinausgehen:

### 1. **Automatische Metadatenextraktion mit KI**
Nach dem Upload und der OCR-Verarbeitung soll ein GenAI-Worker den Text automatisch mit **Google Gemini** analysieren und strukturiert extrahieren:
- Personen, Unternehmen, Orte
- Datums- und ID-Angaben (Rechnungs-Nr., IBAN, etc.)
- Dokumenttyp (Rechnung, Vertrag, Mahnung, etc.)
- Relevante Keywords

**Vorteil:** Keine manuelle Metadateneingabe nötig, konsistente Datenstruktur, sofort durchsuchbar.

### 2. **Intelligentes, automatisches Tagging**
Basierend auf den KI-extrahierten Metadaten soll das System automatisch strukturierte Tags generieren:
- Dokumenttyp: „Rechnung", „Vertrag"
- Zeitkontext: „2025", „Q1-2025"
- Organisationen: „Firma XY"
- Thematik: „Finanzwesen", „Personal"

**Vorteil:** Sofortige Kategorisierung, einheitliche Taxonomie, flexible Filter- und Suchfunktionen.

### 3. **Semantische Ähnlichkeitserkennung**
Das System soll für jedes Dokument **Embedding-Vektoren** (numerische Repräsentation des Inhalts) erzeugen und damit semantischen Vergleich statt nur Keyword-Suche ermöglichen.

Das System würde automatisch erkennen:
- Thematisch ähnliche Dokumente
- Inhaltliche Zusammenhänge (z. B. Angebot → Auftrag → Rechnung)
- Semantische Ähnlichkeit trotz unterschiedlicher Formulierungen

Im UI soll ein **„Ähnliche Dokumente"**-Bereich angezeigt werden mit Vorschau, Matching-Score und Direktlinks.

**Vorteil:** Kontexterkennung, Duplikaterkennung, intelligente Empfehlungen, bessere Navigation.

---

## Geplante technische Umsetzung

### Architektur
```
Upload → OCR (Tesseract) → GenAI Worker (Google Gemini) → 
  → Metadaten + Tags + Embeddings → 
  → Indexing Worker (Elasticsearch) → 
  → UI (React)
```


### Geplante Verarbeitungspipeline
1. Upload → Speicherung in S3
2. OCR-Worker extrahiert Text (RabbitMQ Queue)
3. **GenAI-Worker analysiert Text mit Gemini** (Metadaten + Tags + Embeddings) → **NEU**
4. **Indexing-Worker überträgt zu Elasticsearch** → **NEU**
5. **UI zeigt Metadaten, Tags und ähnliche Dokumente an** → **NEU**

---

## Abgrenzung zu Standard-DMS

**Standard-DMS:**
- Manuelles Tagging
- Nur Keyword-Suche
- Statische Metadaten
- Keine inhaltliche Analyse

**Unser geplantes System:**
- Vollautomatische Metadatenextraktion mit KI
- Intelligentes, kontextbezogenes Tagging
- Semantische Ähnlichkeitssuche
- Inhaltsbasierte Dokumentenverknüpfung
- Skalierbare, asynchrone Verarbeitung

---
