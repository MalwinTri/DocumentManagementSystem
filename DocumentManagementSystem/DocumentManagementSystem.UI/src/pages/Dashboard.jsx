// src/pages/Dashboard.jsx
import React from "react";
import { Search, Tag, Trash2, Filter, ExternalLink } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";

import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { ScrollArea } from "@/components/ui/scroll-area";

import { api } from "@/api/client";
import {
    uploadDocument,
    listDocuments,
    deleteDocumentsBulk,
    updateDocument,
    searchDocuments,
    getDocument,
} from "@/api/documents";

import { parseTag, toneDotClass, toneBarClass, visibleTags, uniqTags } from "@/lib/tags";
import { TagChipsCompact, TagGroups } from "@/components/tags/TagUI";
import { TagEditor } from "@/components/tags/TagEditor";

import { useConfirm } from "@/components/ui/confirmDialog";

// ---------- Mapping ----------
function mapToCardItem(dto) {
    return {
        id: dto.id,
        title: dto.title ?? "Untitled",
        date: dto.createdAt?.slice(0, 10) ?? "",
        tags: Array.isArray(dto.tags) ? dto.tags : [],
        summary: dto.summary ?? "-",
        preview: dto.summary ?? dto.description ?? (dto.ocrText ? dto.ocrText.slice(0, 140) + "..." : ""),
    };
}

// ---------- Farben für linke Farbleiste ----------
function barClassesFromTags(tags) {
    const out = [];
    const seen = new Set();
    const cleaned = visibleTags(tags ?? []);

    for (const t of cleaned) {
        const c = toneBarClass(t);
        if (seen.has(c)) continue;
        seen.add(c);
        out.push(c);
        if (out.length >= 10) break;
    }

    return out.length ? out : ["bg-slate-400/40"];
}

// ---------- Polling helpers (Variante A) ----------
function isDocReady(dto) {
    //  Stop-Kriterium: Summary vorhanden + mindestens ein kw:-Tag
    // (Wenn du kw: nicht hast, läuft es bis maxTries und stoppt dann automatisch.)
    const summary = String(dto?.summary ?? "").trim();
    const hasSummary = summary.length > 0 && summary !== "-";
    const hasKw = Array.isArray(dto?.tags) && dto.tags.some((t) => String(t).toLowerCase().startsWith("kw:"));
    return hasSummary && hasKw;
}

// ---------- Dropzone (Click + Drag & Drop) ----------
function Dropzone({ onUploaded, onStartPolling }) {
    const inputRef = React.useRef(null);
    const [busy, setBusy] = React.useState(false);
    const [msg, setMsg] = React.useState("");
    const [dragging, setDragging] = React.useState(false);
    const dragCounter = React.useRef(0);

    async function uploadOne(file) {
        if (!file) return;
        setBusy(true);
        setMsg(""); try {
            const saved = await uploadDocument(file, { title: file.name });
            setMsg("Uploaded");
            //  wichtig: Polling direkt hier starten (damit Drag&Drop und Click identisch funktionieren)
            onStartPolling?.(saved?.id);
            onUploaded?.(saved);
        } catch (err) {
            console.error(err);
            setMsg(`Upload failed: ${String(err)}`);
        } finally {
            setBusy(false);
            if (inputRef.current) inputRef.current.value = "";
        }
    }

    async function onPickFile(e) {
        const file = e.target.files?.[0];
        await uploadOne(file);
    }

    function onDragEnter(e) {
        e.preventDefault();
        e.stopPropagation();
        dragCounter.current += 1;
        setDragging(true);
    }

    function onDragLeave(e) {
        e.preventDefault();
        e.stopPropagation();
        dragCounter.current -= 1;
        if (dragCounter.current <= 0) {
            dragCounter.current = 0;
            setDragging(false);
        }
    }

    function onDragOver(e) {
        e.preventDefault();
        e.stopPropagation();
        // wichtig: damit Drop erlaubt ist
        e.dataTransfer.dropEffect = "copy";
    }

    async function onDrop(e) {
        e.preventDefault();
        e.stopPropagation();
        dragCounter.current = 0;
        setDragging(false);

        if (busy) return;

        const file = e.dataTransfer?.files?.[0];
        await uploadOne(file);
    }

    return (
        <div
            className={
                "flex flex-col items-center justify-center text-center border-2 border-dashed rounded-2xl py-16 px-6 bg-background transition " +
                (dragging ? "border-indigo-500/60 bg-indigo-500/5" : "")
            }
            onDragEnter={onDragEnter}
            onDragLeave={onDragLeave}
            onDragOver={onDragOver}
            onDrop={onDrop}
            role="button"
            tabIndex={0}
            onClick={() => !busy && inputRef.current?.click()}
            onKeyDown={(e) => {
                if ((e.key === "Enter" || e.key === " ") && !busy) inputRef.current?.click();
            }}
            aria-label="Upload document"
        >
            <div className="flex flex-col items-center gap-3">
                <input
                    ref={inputRef}
                    type="file"
                    className="hidden"
                    onChange={onPickFile}
                    // wenn du wirklich NUR PDFs willst, lass das so. Sonst einfach weg.
                    accept=".pdf,application/pdf"
                />

                <div className="text-sm text-muted-foreground">
                    {dragging ? "Drop file to upload" : "Drag & drop a PDF here or click to select"}
                </div>

                <Button
                    className="rounded-xl"
                    disabled={busy}
                    onClick={(e) => {
                        e.stopPropagation();
                        inputRef.current?.click();
                    }}
                    type="button"
                >
                    {busy ? "Uploading..." : "Select file"}
                </Button>
            </div>

            {msg && <div className="mt-3 text-sm text-muted-foreground">{msg}</div>}
        </div>
    );
}

// ---------- Tags Panel helpers ----------
function buildTagCounts(items) {
    const counts = new Map();
    for (const it of items ?? []) {
        for (const t of it.tags ?? []) {
            counts.set(t, (counts.get(t) ?? 0) + 1);
        }
    }
    return Array.from(counts.entries())
        .map(([tag, count]) => ({ tag, count }))
        .sort((a, b) => b.count - a.count || a.tag.localeCompare(b.tag));
}

function toTagSet(tagCounts) {
    return new Set((tagCounts ?? []).map((x) => x.tag));
}

function TagPanel({ tags, selected, disabledSet, onToggle, onClear, showAllToggle, showAll, onShowAllChange }) {
    const [tagQuery, setTagQuery] = React.useState("");

    const visible = React.useMemo(() => {
        const q = tagQuery.trim().toLowerCase();

        const base = visibleTags(tags.map((x) => x.tag)).map((t) => {
            const original = tags.find((z) => z.tag === t);
            return original ?? { tag: t, count: 0 };
        });

        if (!q) return base;

        return base.filter((x) => {
            const label = parseTag(x.tag).label?.toLowerCase?.() ?? "";
            return x.tag.toLowerCase().includes(q) || label.includes(q);
        });
    }, [tags, tagQuery]);

    return (
        <div className="space-y-4">
            <div className="space-y-2">
                <Label className="text-sm font-medium">Tags</Label>
                <Input
                    value={tagQuery}
                    onChange={(e) => setTagQuery(e.target.value)}
                    placeholder="Search tags..."
                    className="rounded-xl"
                />
            </div>

            {showAllToggle && (
                <div className="flex items-center gap-2">
                    <Checkbox checked={showAll} onCheckedChange={(v) => onShowAllChange?.(!!v)} />
                    <div className="text-sm text-muted-foreground">Show unavailable tags</div>
                </div>
            )}

            <div className="flex flex-wrap gap-2">
                {visible.length === 0 ? (
                    <div className="text-sm text-muted-foreground">No tags found.</div>
                ) : (
                    visible.map((x) => {
                        const active = selected.has(x.tag);
                        const disabled = !active && disabledSet?.has?.(x.tag);

                        return (
                            <button
                                key={x.tag}
                                type="button"
                                onClick={() => !disabled && onToggle(x.tag)}
                                disabled={disabled}
                                className={
                                    "inline-flex items-center gap-2 rounded-xl border px-3 py-1.5 text-sm transition " +
                                    (active
                                        ? "bg-indigo-500/10 border-indigo-500/25 ring-1 ring-indigo-500/15"
                                        : disabled
                                            ? "opacity-40 cursor-not-allowed bg-background"
                                            : "bg-background hover:bg-muted")
                                }
                                title={disabled ? "No results with current selection" : undefined}
                            >
                                <span className={"h-2 w-2 rounded-full " + toneDotClass(x.tag)} />
                                <span className="font-medium">{parseTag(x.tag).label}</span>
                                <span className="text-xs text-muted-foreground">{x.count}</span>
                            </button>
                        );
                    })
                )}
            </div>

            <div className="flex items-center justify-between pt-2">
                <Button variant="outline" size="sm" className="rounded-xl" onClick={onClear} disabled={selected.size === 0} type="button">
                    Clear
                </Button>
            </div>
        </div>
    );
}

function InlineFiltersPanel({
    open,
    onClose,
    tags,
    selectedTags,
    disabledSet,
    toggleTag,
    clearTags,
    showAllTags,
    setShowAllTags,
}) {
    if (!open) return null;

    return (
        <Card className="rounded-2xl">
            <CardHeader className="pb-3">
                <div className="flex items-center justify-between gap-3">
                    <CardTitle className="text-lg flex items-center gap-2">
                        <Filter className="w-5 h-5" /> Filters
                    </CardTitle>
                    <div className="flex items-center gap-2">
                        {selectedTags.size > 0 && (
                            <Button size="sm" variant="outline" className="rounded-xl" onClick={clearTags} type="button">
                                Clear tags
                            </Button>
                        )}
                        <Button
                            size="sm"
                            variant="ghost"
                            className="rounded-xl"
                            onClick={onClose}
                            type="button"
                            aria-label="Close filters"
                        >Close</Button>
                    </div>
                </div>
            </CardHeader>

            <CardContent>
                <div className="rounded-2xl border p-4">
                    <div className="flex items-center gap-2 mb-3 text-sm font-medium">
                        <Tag className="w-4 h-4" /> Tag filter
                    </div>

                    <TagPanel
                        tags={tags}
                        selected={selectedTags}
                        disabledSet={disabledSet}
                        onToggle={toggleTag}
                        onClear={clearTags}
                        showAllToggle={true}
                        showAll={showAllTags}
                        onShowAllChange={setShowAllTags}
                    />
                </div>
            </CardContent>
        </Card>
    );
}

// ---------- Cards ----------
function ResultCard({ item, selected, onToggle, onOpen }) {
    const bars = barClassesFromTags(item?.tags);

    return (
        <Card
            className={
                "relative rounded-2xl transition " +
                (selected ? "ring-2 ring-primary/50" : "hover:shadow-md hover:-translate-y-0.5")
            }
        >
            <div className="absolute left-0 top-0 h-full w-1.5 overflow-hidden">
                <div className="h-full w-full flex flex-col">
                    {bars.map((c) => (
                        <div key={c} className={"flex-1 " + c} />
                    ))}
                </div>
            </div>

            <CardContent className="space-y-3 pt-6 pl-7">
                <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                        <div className="font-medium text-base truncate">{item.title}</div>
                        <div className="text-xs text-muted-foreground">{item.date}</div>
                    </div>
                    <div onClick={(e) => e.stopPropagation()}>
                        <Checkbox checked={selected} onCheckedChange={onToggle} />
                    </div>
                </div>

                <div
                    role="button"
                    tabIndex={0}
                    className="text-left w-full space-y-3 cursor-pointer"
                    onClick={() => onOpen(item)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") onOpen(item);
                    }}
                >
                    <TagChipsCompact tags={item.tags ?? []} max={6} onMore={() => onOpen(item)} />

                    <div className="rounded-xl bg-muted/40 p-3 text-sm border">
                        <div className="flex items-center gap-2 mb-1 text-muted-foreground">
                            <span className="text-foreground font-bold flex items-center gap-2">
                                AI Summary
                            </span>
                        </div>
                        <div className="max-h-48 overflow-y-auto pr-2">
                            <p className="whitespace-pre-wrap break-words">{item.summary}</p>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

// ---------- Similar ----------
function normalizeSimilarResponse(arr) {
    return (arr ?? [])
        .map((x) => {
            const doc = x?.document ?? x;
            const score = typeof x?.score === "number" ? x.score : typeof doc?.score === "number" ? doc.score : 0;
            return { doc, score };
        })
        .filter((x) => x.score > 0);
}

function SimilarRow({ dto, score, onOpen }) {
    const item = mapToCardItem(dto);
    const bars = barClassesFromTags(item?.tags);
    const pct = Math.round(score * 100);
    if (pct <= 0) return null;

    const showTags = visibleTags(item.tags ?? []).slice(0, 3);

    return (
        <button
            type="button"
            onClick={() => onOpen?.(dto)}
            className="w-full text-left rounded-xl border hover:bg-muted/50 transition px-3 py-2"
        >
            <div className="flex items-start gap-3">
                <div className="mt-1 h-8 w-1.5 overflow-hidden rounded-full border border-border/60">
                    <div className="h-full w-full flex flex-col">
                        {bars.slice(0, 4).map((c) => (
                            <div key={c} className={"flex-1 " + c} />
                        ))}
                    </div>
                </div>

                <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                            <div className="font-medium truncate">{item.title}</div>
                            <div className="text-xs text-muted-foreground mt-0.5">{item.date}</div>
                        </div>

                        <div className="flex items-center gap-2">
                            <Badge className="rounded-xl" variant="secondary">
                                {pct}%
                            </Badge>
                            <ExternalLink className="w-4 h-4 text-muted-foreground" />
                        </div>
                    </div>

                    <div className="mt-1 text-xs text-muted-foreground whitespace-pre-wrap break-words">
                        {item.preview || item.summary || ""}
                    </div>

                    <div className="mt-2 flex flex-wrap gap-2">
                        {showTags.map((t) => (
                            <Badge key={t} variant="secondary" className="rounded-xl inline-flex items-center gap-2">
                                <span className={"h-2 w-2 rounded-full " + toneDotClass(t)} />
                                {parseTag(t).label}
                            </Badge>
                        ))}
                        {visibleTags(item.tags ?? []).length > 3 && (
                            <span className="text-xs text-muted-foreground">+{visibleTags(item.tags ?? []).length - 3}</span>
                        )}
                    </div>
                </div>
            </div>

            <div className="mt-2 h-1.5 w-full rounded-full bg-muted">
                <div className="h-1.5 rounded-full bg-indigo-500/40" style={{ width: `${pct}%` }} />
            </div>
        </button>
    );
}

// ---------- Detail Sheet ----------
function RightDetailSheet({ openItem, open, onOpenChange, onDeleted, onUpdated, onOpenDoc, ensureLive }) {
    const confirm = useConfirm();

    const [detail, setDetail] = React.useState(null);

    const [title, setTitle] = React.useState("");
    const [summary, setSummary] = React.useState("");
    const [tagsArr, setTagsArr] = React.useState([]);

    const [similar, setSimilar] = React.useState([]);
    const [similarLoading, setSimilarLoading] = React.useState(false);

    // Autosave status (kein Save-Button)
    const [dirty, setDirty] = React.useState(false);
    const [saving, setSaving] = React.useState(false);
    const [saveError, setSaveError] = React.useState("");

    const MAX_TAGS = 10; // Backend-Validation: max. 10 Tags

    // Tag suggestions API (bleibt in Dashboard, damit du nix extra anlegen musst)
    const loadTagSuggestions = React.useCallback(async (q) => {
        const params = new URLSearchParams();
        if (q != null) params.set("q", String(q));
        params.set("take", "20");
        return api.get(`/api/Tags/suggest?${params.toString()}`);
    }, []);

    React.useEffect(() => {
        if (!openItem?.id) return;

        ensureLive?.(openItem.id); // wenn offen: automatisch aktuell halten

        (async () => {
            try {
                const fresh = await getDocument(openItem.id);
                setDetail(fresh);
                setTitle(fresh.title ?? "");
                setSummary(fresh.summary ?? "");
                setTagsArr(uniqTags(fresh.tags ?? []).slice(0, MAX_TAGS));
                setDirty(false);
                setSaveError("");
            } catch (e) {
                console.error(e);
                setDetail(null);
                setTitle(openItem.title ?? "");
                setSummary(openItem.summary ?? "");
                setTagsArr(uniqTags(openItem.tags ?? []).slice(0, MAX_TAGS));
                setDirty(false);
                setSaveError("");
            }
        })();
    }, [openItem?.id, ensureLive]);

    React.useEffect(() => {
        if (!openItem?.id) return;

        setSimilar([]);
        setSimilarLoading(true);

        (async () => {
            try {
                const res = await api.get(`/api/Documents/${openItem.id}/similar?take=6`);
                setSimilar(normalizeSimilarResponse(res));
            } catch (e) {
                setSimilar([]);
            } finally {
                setSimilarLoading(false);
            }
        })();
    }, [openItem?.id]);

    // Autosave (debounced) — speichert Tags/Title/Summary automatisch
    React.useEffect(() => {
        if (!openItem?.id) return;
        if (!dirty) return;

        const id = openItem.id;
        const h = window.setTimeout(async () => {
            try {
                setSaving(true);
                setSaveError("");

                const tags = uniqTags(tagsArr ?? []).slice(0, MAX_TAGS);
                const updated = await updateDocument(id, { title, summary, tags });

                // Sync local state with server response
                setTitle(updated?.title ?? "");
                setSummary(updated?.summary ?? "");
                setTagsArr(uniqTags(updated?.tags ?? []));
                setDetail(updated);
                setDirty(false);

                onUpdated?.(updated);
            } catch (e) {
                console.error(e);
                setSaveError("Save failed");
                // dirty bleibt true
            } finally {
                setSaving(false);
            }
        }, 700);

        return () => window.clearTimeout(h);
    }, [dirty, title, summary, tagsArr, openItem?.id, onUpdated]);

    async function handleDelete() {
        if (!openItem?.id) return;
        const ok = await confirm({
            title: <>Delete “{title || openItem.title}”?</>,
            confirmText: "Delete",
            cancelText: "Cancel",
            destructive: true,
        });
        if (!ok) return;

        await deleteDocumentsBulk([openItem.id]);
        onDeleted?.(openItem.id);
        onOpenChange(false);
    }

    // Show the *edited* tags immediately (and keep them stable while polling)
    const tagsForUI = uniqTags(tagsArr ?? []);

    return (
        <Sheet open={open} onOpenChange={onOpenChange}>
            <SheetContent className="p-0 flex flex-col">
                <SheetHeader className="px-4 py-3 border-b">
                    <div className="flex items-center justify-between gap-3">
                        <SheetTitle className="truncate flex-1">{title || openItem?.title || "Document"}</SheetTitle>
                        <div className="text-xs text-muted-foreground">
                            {saving ? "Saving..." : saveError ? saveError : dirty ? "Unsaved" : "Saved"}
                        </div>
                    </div>
                </SheetHeader>

                <ScrollArea className="flex-1">
                    <div className="p-4 space-y-5">
                        <div className="rounded-2xl border p-4 space-y-3">
                            <div className="font-medium">Summary</div>
                            <textarea
                                value={summary}
                                onChange={(e) => {
                                    setSummary(e.target.value);
                                    setDirty(true);
                                }}
                                className="w-full h-36 resize-none rounded-xl border bg-background p-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                            />
                        </div>

                        <div className="rounded-2xl border p-4 space-y-3">
                            <div className="font-medium">Metadata</div>
                            <div className="space-y-3">
                                <div>
                                    <div className="text-sm font-medium">Title</div>
                                    <Input
                                        value={title}
                                        onChange={(e) => {
                                            setTitle(e.target.value);
                                            setDirty(true);
                                        }}
                                        className="rounded-xl mt-1"
                                    />
                                </div>

                                <div>
                                    <div className="text-sm font-medium">Tags</div>

                                    <div className="mt-2">
                                        <TagChipsCompact
                                            tags={tagsForUI}
                                            max={10}
                                            onMore={() =>
                                                document.getElementById("full-tags")?.scrollIntoView({ behavior: "smooth", block: "start" })
                                            }
                                        />
                                    </div>

                                    <div className="mt-3">
                                        <TagEditor
                                            value={tagsArr}
                                            onChange={(next) => {
                                                const clean = uniqTags(next ?? []);
                                                if (clean.length > MAX_TAGS) setSaveError("No more than 10 tags allowed.");
                                                setTagsArr(clean.slice(0, MAX_TAGS));
                                                setDirty(true);
                                            }}
                                            loadSuggestions={loadTagSuggestions}
                                        />
                                    </div>

                                    <div id="full-tags" className="mt-4">
                                        <TagGroups tags={tagsForUI} />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="rounded-2xl border p-4 space-y-3">
                            <div className="flex items-center justify-between gap-2">
                                <div className="font-medium">Ähnliche Dokumente</div>
                            </div>

                            {similarLoading ? (
                                <div className="text-sm text-muted-foreground">Loading...</div>
                            ) : similar.length === 0 ? (
                                <div className="text-sm text-muted-foreground">Keine ähnlichen Dokumente gefunden.</div>
                            ) : (
                                <div className="space-y-2">
                                    {similar.map((x) => (
                                        <SimilarRow
                                            key={x.doc.id}
                                            dto={x.doc}
                                            score={x.score}
                                            onOpen={(docDto) => onOpenDoc?.(mapToCardItem(docDto))}
                                        />
                                    ))}
                                </div>
                            )}
                        </div>

                        <div className="rounded-2xl border p-4">
                            <div className="font-medium mb-2">Activity</div>
                            <ul className="text-sm space-y-2 text-muted-foreground">
                                <li>Uploaded on {openItem?.date || "-"}</li>
                                <li>Indexed</li>
                                <li>Summary generated</li>
                            </ul>
                        </div>
                    </div>
                </ScrollArea>

                <div className="border-t bg-background p-3 flex items-center gap-2">
                    <Button
                        variant="destructive"
                        className="rounded-xl"
                        type="button"
                        onClick={handleDelete}
                        disabled={saving}
                    >
                        Delete
                    </Button>

                    <div className="flex-1" />

                    <div className="text-xs text-muted-foreground">
                        {saving ? "Saving..." : saveError ? saveError : dirty ? "Unsaved" : "Saved"}
                    </div>
                </div>
            </SheetContent>
        </Sheet>
    );
}

// ---------- Dashboard ----------
export default function Dashboard() {
    const confirm = useConfirm();

    const [tab, setTab] = React.useState("results");
    const [query, setQuery] = React.useState("");
    const [loading, setLoading] = React.useState(true);

    const [items, setItems] = React.useState([]);
    const [selected, setSelected] = React.useState(new Set());

    const [filtersOpen, setFiltersOpen] = React.useState(false);
    const [selectedTags, setSelectedTags] = React.useState(new Set());

    const [showAllTags, setShowAllTags] = React.useState(false);

    const [openItem, setOpenItem] = React.useState(null);

    // Polling state
    const pollTimersRef = React.useRef(new Map()); // id -> intervalId

    const applyFresh = React.useCallback((dto) => {
        const card = mapToCardItem(dto);

        // items updaten (oder hinzufügen, falls z.B. Suche aktuell ist)
        setItems((prev) => {
            const exists = prev.some((x) => x.id === card.id);
            if (!exists) return [card, ...prev];
            return prev.map((x) => (x.id === card.id ? { ...x, ...card } : x));
        });

        // wenn Detail offen: auch updaten
        setOpenItem((prev) => (prev?.id === card.id ? { ...prev, ...card } : prev));
    }, []);

    const stopPolling = React.useCallback((id) => {
        const intervalId = pollTimersRef.current.get(id);
        if (intervalId) {
            window.clearInterval(intervalId);
            pollTimersRef.current.delete(id);
        }
    }, []);

    const startPolling = React.useCallback(
        (id) => {
            if (!id) return;
            if (pollTimersRef.current.has(id)) return;

            let tries = 0;
            const maxTries = 80; // ~120 Sekunden bei 1500ms

            const intervalId = window.setInterval(async () => {
                tries++;

                try {
                    const fresh = await getDocument(id);
                    applyFresh(fresh);

                    if (isDocReady(fresh) || tries >= maxTries) {
                        stopPolling(id);
                    }
                } catch (e) {
                    // Wenn es dauerhaft crasht, nach maxTries stoppen.
                    if (tries >= maxTries) stopPolling(id);
                }
            }, 1500);

            pollTimersRef.current.set(id, intervalId);
        },
        [applyFresh, stopPolling]
    );
    // cleanup
    React.useEffect(() => {
        return () => {
            for (const [, intervalId] of pollTimersRef.current.entries()) window.clearInterval(intervalId);
            pollTimersRef.current.clear();
        };
    }, []);

    //  Safety-Net: falls ein Poll-Start mal nicht feuert (z.B. Drop-Edgecases),
    // starte Polling automatisch für die neuesten Docs ohne Summary.
    React.useEffect(() => {
        const t = window.setInterval(() => {
            const need = (items ?? []).filter((x) => {
                const s = String(x?.summary ?? "").trim();
                return !s || s === "-";
            });

            // nur ein paar gleichzeitig
            for (const it of need.slice(0, 4)) {
                startPolling(it.id);
            }
        }, 2500);

        return () => window.clearInterval(t);
    }, [items, startPolling]);

    // initial load
    React.useEffect(() => {
        (async () => {
            try {
                setLoading(true);
                const page = await listDocuments(0, 50);
                setItems((page.items ?? []).map(mapToCardItem));
            } finally {
                setLoading(false);
            }
        })();
    }, []);

    // search
    React.useEffect(() => {
        const handle = setTimeout(async () => {
            const q = query.trim();
            try {
                setLoading(true);
                if (!q) {
                    const page = await listDocuments(0, 50);
                    setItems((page.items ?? []).map(mapToCardItem));
                } else {
                    const results = await searchDocuments(q);
                    setItems((results ?? []).map(mapToCardItem));
                }
            } catch (e) {
                console.error(e);
            } finally {
                setLoading(false);
            }
        }, 300);

        return () => clearTimeout(handle);
    }, [query]);

    const filteredDocs = React.useMemo(() => {
        const activeTags = Array.from(selectedTags);
        if (activeTags.length === 0) return items;

        return items.filter((x) => {
            const set = new Set(x.tags ?? []);
            for (const t of activeTags) if (!set.has(t)) return false;
            return true;
        });
    }, [items, selectedTags]);

    const allTagCounts = React.useMemo(() => buildTagCounts(items), [items]);

    const availableTagCounts = React.useMemo(() => {
        const baseDocs = selectedTags.size === 0 ? items : filteredDocs;
        return buildTagCounts(baseDocs);
    }, [items, filteredDocs, selectedTags]);

    const tagCountsForPanel = showAllTags ? allTagCounts : availableTagCounts;

    const disabledSet = React.useMemo(() => {
        if (!showAllTags) return new Set();
        const allSet = toTagSet(allTagCounts);
        const availSet = toTagSet(availableTagCounts);
        const d = new Set();
        for (const t of allSet) if (!availSet.has(t)) d.add(t);
        return d;
    }, [showAllTags, allTagCounts, availableTagCounts]);

    function toggleSelect(id) {
        setSelected((prev) => {
            const next = new Set(prev);
            next.has(id) ? next.delete(id) : next.add(id);
            return next;
        });
    }

    function clearSelection() {
        setSelected(new Set());
    }

    function selectAll() {
        setSelected(new Set(filteredDocs.map((x) => x.id)));
    }

    async function deleteSelected() {
        const ids = Array.from(selected);
        if (!ids.length) return;

        const ok = await confirm({
            title: <>Delete {ids.length} document{ids.length === 1 ? "" : "s"}?</>,
            confirmText: "Delete",
            cancelText: "Cancel",
            destructive: true,
        });
        if (!ok) return;

        try {
            await deleteDocumentsBulk(ids);
            setItems((prev) => prev.filter((x) => !selected.has(x.id)));
            clearSelection();
            if (openItem && selected.has(openItem.id)) setOpenItem(null);

            // falls gerade polling läuft: stoppen
            for (const id of ids) stopPolling(id);
        } catch (e) {
            console.error(e);
            alert("Delete failed");
        }
    }

    function toggleTag(tag) {
        setSelectedTags((prev) => {
            const next = new Set(prev);
            next.has(tag) ? next.delete(tag) : next.add(tag);
            return next;
        });
    }

    function clearTags() {
        setSelectedTags(new Set());
    }

    function handleUpdated(updatedDto) {
        applyFresh(updatedDto);
    }

    function handleDeleted(id) {
        setItems((prev) => prev.filter((x) => x.id !== id));
        setSelected((prev) => {
            const n = new Set(prev);
            n.delete(id);
            return n;
        });
        setOpenItem(null);
        stopPolling(id);
    }

    async function runSearch(currentQuery) {
        const q = currentQuery.trim();

        // Wenn leer -> normale Liste laden
        if (!q) {
            setLoading(true);
            setError(null);
            try {
                const page = await listDocuments(0, 20);
                setItems(page.items.map(mapToCardItem));
            } catch (e) {
                console.warn("List endpoint failed in search reset:", e);
                setError(e);
            } finally {
                setLoading(false);
            }
            return;
        }

        // Suche in Backend
        setLoading(true);
        setError(null);
        try {
            const results = await searchDocuments(q);
            setItems(results.map(mapToCardItem));
        } catch (e) {
            console.error("Search failed", e);
            setError(e);
        } finally {
            setLoading(false);
        }
    }



    return (
        <div className="min-h-screen bg-background text-foreground">
            <header className="sticky top-0 z-30 border-b bg-background/80 backdrop-blur supports-[backdrop-filter]:bg-background/60">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-3">
                    <div className="ml-auto flex items-center gap-2 flex-1">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                            <Input
                                placeholder="Search documents..."
                                className="pl-9 rounded-xl"
                                value={query}
                                onChange={(e) => setQuery(e.target.value)}
                            />
                        </div>

                        <Button
                            variant="outline"
                            className="rounded-xl gap-2"
                            type="button"
                            onClick={() => setFiltersOpen((v) => !v)}
                        >
                            <Filter className="w-4 h-4" />
                            Filters
                            {selectedTags.size > 0 && (
                                <span className="ml-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-indigo-500/10 text-indigo-700 border border-indigo-500/25 px-1.5 text-xs">
                                    {selectedTags.size}
                                </span>
                            )}
                        </Button>
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
                <Tabs value={tab} onValueChange={setTab} className="w-full">
                    <Card className="rounded-2xl">
                        <CardHeader className="pb-2">
                            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                                <TabsList className="rounded-xl w-fit">
                                    <TabsTrigger value="upload" className="rounded-xl">
                                        Upload
                                    </TabsTrigger>
                                    <TabsTrigger value="results" className="rounded-xl">
                                        Results
                                    </TabsTrigger>
                                </TabsList>

                                <div className="flex flex-wrap gap-2">
                                    {Array.from(selectedTags).map((t) => (
                                        <button
                                            key={t}
                                            type="button"
                                            onClick={() => toggleTag(t)}
                                            className="inline-flex items-center gap-2 rounded-xl border border-indigo-500/25 bg-indigo-500/10 px-3 py-1 text-sm hover:bg-indigo-500/15"
                                            title="Remove tag"
                                        >
                                            <span className={"h-2 w-2 rounded-full " + toneDotClass(t)} />
                                            <span className="text-foreground">{parseTag(t).label}</span>
                                            <span className="text-muted-foreground">x</span>
                                        </button>
                                    ))}
                                    {selectedTags.size > 0 && (
                                        <Button size="sm" variant="outline" className="rounded-xl" onClick={clearTags} type="button">
                                            Clear tags
                                        </Button>
                                    )}
                                </div>
                            </div>
                        </CardHeader>

                        <CardContent>
                            <TabsContent value="upload" className="mt-2">
                                <Dropzone
                                    onStartPolling={(id) => startPolling(id)}
                                    onUploaded={(dto) => {
                                        const card = mapToCardItem(dto);
                                        setItems((prev) => [card, ...prev]);
                                        setTab("results");
                                    }}
                                />
                            </TabsContent>

                            <TabsContent value="results" className="mt-2 space-y-6">
                                <InlineFiltersPanel
                                    open={filtersOpen}
                                    onClose={() => setFiltersOpen(false)}
                                    tags={tagCountsForPanel}
                                    selectedTags={selectedTags}
                                    disabledSet={disabledSet}
                                    toggleTag={toggleTag}
                                    clearTags={clearTags}
                                    showAllTags={showAllTags}
                                    setShowAllTags={setShowAllTags}
                                />

                                <div className="rounded-2xl border bg-background p-3 flex flex-wrap items-center justify-between gap-3">
                                    <div className="text-sm text-muted-foreground">
                                        {loading ? "" : `${filteredDocs.length} document${filteredDocs.length === 1 ? "" : "s"}`}
                                    </div>
                                    <div className="flex flex-wrap gap-2">
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            className="rounded-xl"
                                            type="button"
                                            onClick={selectAll}
                                            disabled={loading || filteredDocs.length === 0}
                                        >
                                            Select all
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            className="rounded-xl"
                                            type="button"
                                            onClick={clearSelection}
                                            disabled={selected.size === 0}
                                        >
                                            Clear selection
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="destructive"
                                            className="rounded-xl gap-2"
                                            type="button"
                                            onClick={deleteSelected}
                                            disabled={selected.size === 0}
                                        >
                                            <Trash2 className="w-4 h-4" /> Delete
                                        </Button>
                                    </div>
                                </div>

                                {loading ? (
                                    <div className="text-sm text-muted-foreground">Loading...</div>
                                ) : filteredDocs.length === 0 ? (
                                    <Card className="rounded-2xl">
                                        <CardContent className="py-16 text-center text-muted-foreground">No documents found.</CardContent>
                                    </Card>
                                ) : (
                                    <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
                                        {filteredDocs.map((it) => (
                                            <ResultCard
                                                key={it.id}
                                                item={it}
                                                selected={selected.has(it.id)}
                                                onToggle={() => toggleSelect(it.id)}
                                                onOpen={(x) => {
                                                    setOpenItem(x);
                                                    startPolling(x.id); //  auch beim Öffnen live halten
                                                }}
                                            />
                                        ))}
                                    </div>
                                )}

                                <RightDetailSheet
                                    openItem={openItem}
                                    open={!!openItem}
                                    onOpenChange={(v) => !v && setOpenItem(null)}
                                    onDeleted={handleDeleted}
                                    onUpdated={handleUpdated}
                                    onOpenDoc={(docDto) => setOpenItem(mapToCardItem(docDto))}
                                    ensureLive={(id) => startPolling(id)}
                                />
                            </TabsContent>
                        </CardContent>
                    </Card>
                </Tabs>
            </main>
        </div>
    );
}
