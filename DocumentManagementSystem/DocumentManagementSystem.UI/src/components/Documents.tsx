// src/components/Documents.tsx
import { useEffect, useMemo, useState } from 'react'
import { fetchDocuments } from '../lib/api'
import type { Doc, DocType } from '../lib/api'  // <-- type-only import!

// sauber typisierte Map statt "as any"
const typeMap: Record<'pdf' | 'doc' | 'image' | 'zip', DocType> = {
    pdf: 'Pdf',
    doc: 'Doc',
    image: 'Image',
    zip: 'Zip',
}

export default function Documents() {
    const [query, setQuery] = useState('')
    const [fuzzy, setFuzzy] = useState(true)
    const [types, setTypes] = useState<Array<'pdf' | 'doc' | 'image' | 'zip'>>(['pdf', 'doc', 'image', 'zip'])
    const [recencyBoost, setRecencyBoost] = useState(70)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [items, setItems] = useState<Doc[]>([])

    const apiTypes = useMemo<DocType[]>(
        () => types.map(t => typeMap[t]),
        [types]
    )

    useEffect(() => {
        let ignore = false
        setLoading(true)
        setError(null)

        fetchDocuments({ query, types: apiTypes, fuzzy, recencyBoost })
            .then(data => { if (!ignore) setItems(data) })
            .catch(e => { if (!ignore) setError(e?.message ?? 'Fehler beim Laden') })
            .finally(() => { if (!ignore) setLoading(false) })

        return () => { ignore = true }
    }, [query, apiTypes, fuzzy, recencyBoost])

    return (
        <div style={{ maxWidth: 1000, margin: '40px auto', padding: 16 }}>
            <h1>Docs AI</h1>

            <div style={{ display: 'flex', gap: 8, alignItems: 'center', margin: '16px 0' }}>
                <input
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    placeholder="Suche (full-text & fuzzy)…"
                    style={{ flex: 1, padding: 8 }}
                />
                <label style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                    <input type="checkbox" checked={fuzzy} onChange={e => setFuzzy(e.target.checked)} />
                    Fuzzy
                </label>
                <label style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                    Recency: {recencyBoost}
                    <input
                        type="range" min={0} max={100} step={10}
                        value={recencyBoost}
                        onChange={e => setRecencyBoost(parseInt(e.target.value))}
                    />
                </label>
            </div>

            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 12 }}>
                {(['pdf', 'doc', 'image', 'zip'] as const).map(t => {
                    const active = types.includes(t)
                    return (
                        <button
                            key={t}
                            onClick={() =>
                                setTypes(prev => prev.includes(t) ? prev.filter(x => x !== t) : [...prev, t])
                            }
                            style={{
                                padding: '6px 10px',
                                borderRadius: 8,
                                border: '1px solid #ccc',
                                background: active ? '#111' : '#f6f6f6',
                                color: active ? '#fff' : '#111'
                            }}
                        >
                            {t}
                        </button>
                    )
                })}
            </div>

            {loading && <div>Lade…</div>}
            {error && <div style={{ color: 'crimson' }}>{error}</div>}
            {!loading && !error && items.length === 0 && <div>Keine Ergebnisse</div>}

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 12 }}>
                {items.map(d => (
                    <div key={d.id} style={{ border: '1px solid #e5e5e5', borderRadius: 12, padding: 12 }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                            <strong>{d.title}</strong>
                            <span style={{ fontSize: 12, opacity: 0.6 }}>{d.date}</span>
                        </div>
                        <div style={{ fontSize: 12, opacity: 0.7, marginBottom: 8 }}>{d.snippet}</div>
                        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 8 }}>
                            {d.tags.map(t => (
                                <span key={t} style={{ fontSize: 11, background: '#f2f2f2', borderRadius: 12, padding: '2px 8px' }}>{t}</span>
                            ))}
                        </div>
                        <div style={{ fontSize: 12, background: '#fafafa', borderRadius: 8, padding: 8 }}>
                            <div style={{ fontWeight: 600, marginBottom: 4 }}>AI Summary</div>
                            <div>{d.summary}</div>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontSize: 12, opacity: 0.7 }}>
                            <div>
                                {d.status.ocr ? 'OCR ✔ ' : 'OCR ☐ '}
                                {d.status.indexed ? 'Indexed ✔ ' : 'Indexed ☐ '}
                                {d.status.summarized ? 'Summarized ✔' : 'Summarized ☐'}
                            </div>
                            <div>By {d.author}</div>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    )
}