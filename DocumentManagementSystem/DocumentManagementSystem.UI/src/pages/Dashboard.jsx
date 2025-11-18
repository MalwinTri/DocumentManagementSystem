import React from "react";
import {
    Upload,
    FileText,
    Image as ImageIcon,
    FileArchive,
    File,
    Search,
    Filter,
    CheckCircle2,
    HelpCircle,
    Tag,
    Trash2,
    X,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

import {
    uploadDocument,
    listDocuments,
    deleteDocumentsBulk,
    updateDocument,
} from "@/api/documents";
import { useConfirm } from "@/components/ui/confirmDialog";

// ---------- kleine Helfer-Komponenten ----------

function IconButton({ children, className = "", ...props }) {
    return (
        <Button
            variant="ghost"
            size="icon"
            className={`rounded-xl ${className}`}
            {...props}
        >
            {children}
        </Button>
    );
}

function Pill({ icon: Icon, label, active = false }) {
    return (
        <Button
            variant={active ? "default" : "secondary"}
            className={`gap-2 rounded-xl px-4 ${active ? "" : "bg-muted"}`}
            type="button"
        >
            <Icon className="w-4 h-4" />
            {label}
        </Button>
    );
}

function SparklesIcon() {
    return (
        <svg
            viewBox="0 0 24 24"
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
        >
            <path d="M12 3l1.7 3.6L17 8.3l-3.3 1.7L12 13l-1.7-3.1L7 8.3l3.3-1.7L12 3z" />
            <path d="M19 13l.9 1.9L22 16l-2.1 1.1L19 19l-1-1.9L16 16l2-1.1 1-1.9z" />
            <path d="M5 14l.7 1.5L7.5 16l-1.8.9L5 19l-.7-1.9L2.5 16l1.8-.5L5 14z" />
        </svg>
    );
}

function Dropzone({ onUploaded }) {
    const inputRef = React.useRef(null);
    const [busy, setBusy] = React.useState(false);
    const [msg, setMsg] = React.useState("");

    async function onPickFile(e) {
        const file = e.target.files?.[0];
        if (!file) return;
        setBusy(true);
        setMsg("");
        try {
            const saved = await uploadDocument(file, { title: file.name });
            setMsg("Uploaded");
            onUploaded?.(saved);
        } catch (err) {
            console.error(err);
            setMsg(`Upload failed: ${String(err)}`);
        } finally {
            setBusy(false);
            if (inputRef.current) inputRef.current.value = "";
        }
    }

    return (
        <div className="flex flex-col items-center justify-center text-center border-2 border-dashed rounded-2xl py-16 px-6 bg-background">
            <div className="flex items-center gap-3">
                <input
                    ref={inputRef}
                    type="file"
                    className="hidden"
                    onChange={onPickFile}
                />
                <Button
                    className="rounded-xl"
                    disabled={busy}
                    onClick={() => inputRef.current?.click()}
                    type="button"
                >
                    <Upload className="w-4 h-4 mr-2" />
                    {busy ? "Uploading…" : "Select file"}
                </Button>
            </div>
            {msg && <div className="mt-3 text-sm text-muted-foreground">{msg}</div>}
        </div>
    );
}

function ResultCard({ item, onOpen, selected, onToggle }) {
    return (
        <Card
            className={`group rounded-2xl h-full overflow-hidden ${selected ? "ring-2 ring-primary" : ""
                }`}
        >
            <CardContent className="space-y-3">
                {/* klickbarer Bereich */}
                <div
                    className="space-y-3 cursor-pointer"
                    onClick={() => onOpen?.(item)}
                >
                    <div className="flex justify-between items-start">
                        <div className="font-medium text-base truncate">{item.title}</div>
                        <span className="text-xs text-muted-foreground">{item.date}</span>
                    </div>

                    <p className="text-sm text-muted-foreground truncate">
                        {item.preview}
                    </p>

                    <div className="flex flex-wrap gap-2">
                        {(item.tags ?? []).length ? (
                            item.tags.map((t) => (
                                <Badge key={t} variant="secondary" className="rounded-xl">
                                    {t}
                                </Badge>
                            ))
                        ) : (
                            <span className="text-xs text-muted-foreground">No tags</span>
                        )}
                    </div>

                    <div className="rounded-xl bg-muted/50 p-2 text-sm">
                        <div className="flex items-center gap-1 mb-1">
                            <SparklesIcon />
                            <span className="font-medium">AI Summary</span>
                        </div>
                        <p className="line-clamp-2">{item.summary}</p>
                    </div>
                </div>

                {/* Footer (nicht klickbar) */}
                <div className="flex items-center justify-between pt-2">
                    <label
                        className={`flex items-center gap-2 text-sm text-muted-foreground cursor-pointer transition-opacity ${selected ? "opacity-100" : "opacity-0 group-hover:opacity-100"
                            }`}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <input
                            type="checkbox"
                            checked={!!selected}
                            onChange={onToggle}
                            className="cursor-pointer"
                        />
                        Select
                    </label>

                    <span className="text-xs text-muted-foreground">
                        By {item.author}
                    </span>
                </div>
            </CardContent>
        </Card>
    );
}

function RightDetailPanel({ item, onClose, onDelete, onSave }) {
    const [title, setTitle] = React.useState(item?.title ?? "");
    const [summary, setSummary] = React.useState(item?.summary ?? "");
    const [tagsStr, setTagsStr] = React.useState(
        (item?.tags ?? []).join(", ")
    );
    const [saving, setSaving] = React.useState(false);

    React.useEffect(() => {
        if (!item) return;
        setTitle(item.title ?? "");
        setSummary(item.summary ?? "");
        setTagsStr((item.tags ?? []).join(", "));
    }, [item]);

    if (!item) return null;

    const formPayload = {
        title,
        summary,
        tags: tagsStr
            .split(",")
            .map((s) => s.trim())
            .filter(Boolean),
    };

    async function handleSave() {
        try {
            setSaving(true);
            await onSave?.(formPayload);
        } finally {
            setSaving(false);
        }
    }

    return (
        <div className="fixed inset-0 z-50">
            <div
                className="absolute inset-0 bg-black/40 backdrop-blur-sm"
                onClick={onClose}
            />
            <aside
                className="absolute right-0 top-0 h-full w-full sm:w-[440px] md:w-[520px] bg-background border-l shadow-2xl flex flex-col"
                onClick={(e) => e.stopPropagation()}
                role="dialog"
                aria-modal="true"
            >
                <div className="flex items-center justify-between px-4 py-3 border-b">
                    <div className="font-semibold truncate">{item.title}</div>
                    <button
                        className="rounded-xl p-2 hover:bg-muted"
                        onClick={onClose}
                        type="button"
                        aria-label="Close"
                    >
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="p-4 overflow-auto space-y-5">
                    <div className="rounded-2xl border p-4 space-y-3">
                        <div className="font-medium">Summary</div>
                        <textarea
                            value={summary}
                            onChange={(e) => setSummary(e.target.value)}
                            className="w-full h-36 resize-none rounded-xl border bg-background p-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                        <div className="text-right">
                            <Button
                                size="sm"
                                className="rounded-xl"
                                type="button"
                                onClick={handleSave}
                                disabled={saving}
                            >
                                {saving ? "Saving…" : "Save summary"}
                            </Button>
                        </div>
                    </div>

                    <div className="rounded-2xl border p-4 space-y-3">
                        <div className="font-medium">Metadata</div>
                        <div className="space-y-3">
                            <div>
                                <Label className="text-sm">Title</Label>
                                <Input
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    className="rounded-xl"
                                />
                            </div>
                            <div>
                                <Label className="text-sm">Tags</Label>
                                <Input
                                    value={tagsStr}
                                    onChange={(e) => setTagsStr(e.target.value)}
                                    className="rounded-xl"
                                />
                            </div>
                        </div>
                        <div className="flex gap-2 pt-2">
                            <Button
                                variant="destructive"
                                className="rounded-xl gap-2"
                                type="button"
                                onClick={() => onDelete?.(item)}
                            >
                                <Trash2 className="w-4 h-4" />
                                Delete
                            </Button>
                            <Button
                                className="rounded-xl"
                                type="button"
                                onClick={handleSave}
                                disabled={saving}
                            >
                                {saving ? "Saving…" : "Save"}
                            </Button>
                        </div>
                    </div>

                    <div className="rounded-2xl border p-4">
                        <div className="font-medium mb-2">Activity</div>
                        <ul className="text-sm space-y-2">
                            <li className="flex items-center gap-2">
                                <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Uploaded
                                on {item.date || "—"}
                            </li>
                            <li className="flex items-center gap-2">
                                <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Indexed in
                                ElasticSearch
                            </li>
                            <li className="flex items-center gap-2">
                                <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Summary
                                generated
                            </li>
                        </ul>
                    </div>
                </div>
            </aside>
        </div>
    );
}

// -------- Mapping + Mockdaten --------

function mapToCardItem(dto) {
    return {
        id: dto.id,
        title: dto.title ?? "Untitled",
        date: dto.createdAt?.slice(0, 10) ?? "",
        preview:
            dto.summary ??
            dto.description ??
            (dto.ocrText ? dto.ocrText.slice(0, 140) + "…" : "—"),
        tags: Array.isArray(dto.tags) ? dto.tags : [],
        summary: dto.summary ?? "—",
        author: dto.author ?? "System",
    };
}

const mockResults = [
    {
        id: "q2",
        title: "Quarterly Report Q2",
        date: "2025-08-14",
        preview: "…revenue up 18%…",
        tags: ["Finance", "Q2"],
        summary: "Revenue grew 18% YoY…",
        author: "Alex Kim",
    },
    {
        id: "brand",
        title: "Brand Photography Set",
        date: "2025-07-03",
        preview: "…warm palette…",
        tags: ["Marketing", "Assets"],
        summary: "High-res lifestyle images…",
        author: "Jamie Lee",
    },
    {
        id: "contract",
        title: "Contract - Vendor Nova LLC",
        date: "2025-09-02",
        preview: "…force majeure…",
        tags: ["Legal"],
        summary: "12-month term, NET30…",
        author: "Priya Patel",
    },
    {
        id: "legacy",
        title: "Legacy ZIP Archive",
        date: "2024-12-11",
        preview: "…pending OCR jobs…",
        tags: ["Archive", "Legacy"],
        summary: "Contains migrated PDFs…",
        author: "System",
    },
];

// -------- Dashboard --------

export default function Dashboard() {
    const confirm = useConfirm();

    const [items, setItems] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState(null);
    const [tab, setTab] = React.useState("results");
    const [selected, setSelected] = React.useState(new Set());
    const [busy, setBusy] = React.useState(false);
    const [err, setErr] = React.useState(null);
    const [openItem, setOpenItem] = React.useState(null);

    React.useEffect(() => {
        let cancelled = false;

        (async () => {
            setLoading(true);
            setError(null);
            try {
                const page = await listDocuments(0, 20);
                if (!cancelled) setItems(page.items.map(mapToCardItem));
            } catch (e) {
                console.warn("List endpoint missing, using mock data. Error:", e);
                if (!cancelled) setItems(mockResults.map(mapToCardItem));
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, []);

    function handleOpen(item) {
        setOpenItem(item);
    }

    async function handleDeleteFromPanel(doc) {
        if (!doc?.id) return;

        const ok = await confirm({
            title: <>Delete “{doc.title}”?</>,
            confirmText: "Delete",
            destructive: true,
        });
        if (!ok) return;

        try {
            await deleteDocumentsBulk([doc.id]);
            setItems((prev) => prev.filter((x) => x.id !== doc.id));
            setSelected((prev) => {
                const n = new Set(prev);
                n.delete(doc.id);
                return n;
            });
            setOpenItem(null);
        } catch (e) {
            console.error(e);
            alert("Delete failed");
        }
    }

    async function handleSaveFromPanel(payload) {
        if (!openItem?.id) return;
        try {
            const updated = await updateDocument(openItem.id, payload);
            setItems((prev) =>
                prev.map((x) =>
                    x.id === updated.id ? { ...x, ...mapToCardItem(updated) } : x
                )
            );
            setOpenItem((prev) =>
                prev ? { ...prev, ...mapToCardItem(updated) } : prev
            );
        } catch (e) {
            console.error(e);
            alert(e.message || "Save failed");
        }
    }

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
        setSelected(new Set(items.map((x) => x.id)));
    }

    async function deleteSelected() {
        if (selected.size === 0) return;

        const ok = await confirm({
            title: `Delete ${selected.size} item${selected.size > 1 ? "s" : ""}?`,
            confirmText: "Delete",
            destructive: true,
        });
        if (!ok) return;

        setBusy(true);
        setErr(null);

        const ids = Array.from(selected);
        const prevItems = items;
        setItems((prev) => prev.filter((x) => !selected.has(x.id)));
        clearSelection();

        try {
            await deleteDocumentsBulk(ids);
        } catch (e) {
            setItems(prevItems);
            setErr(e?.message ?? "Delete failed");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="min-h-screen bg-background text-foreground">
            {/* Top bar */}
            <header className="sticky top-0 z-30 border-b bg-background/80 backdrop-blur supports-[backdrop-filter]:bg-background/60">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-3">
                    <div className="ml-auto flex items-center gap-2 w-full max-w-2xl">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                            <Input
                                placeholder="Search documents (full-text & fuzzy)…"
                                className="pl-9 rounded-xl"
                            />
                        </div>
                        <Button variant="outline" className="rounded-xl gap-2" type="button">
                            <Filter className="w-4 h-4" />
                            Filters
                        </Button>
                        <IconButton>
                            <HelpCircle className="w-5 h-5" />
                        </IconButton>
                    </div>
                </div>
            </header>

            {/* Main grid */}
            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 grid grid-cols-1 lg:grid-cols-12 gap-6">
                {/* Sidebar */}
                <aside className="lg:col-span-3">
                    <Card className="rounded-2xl">
                        <CardHeader className="pb-4">
                            <CardTitle className="text-lg">Filters</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <Label className="text-base">Fuzzy search</Label>
                                    <Switch id="fuzzy" />
                                </div>
                                <Input
                                    placeholder="Typo tolerance, synonyms"
                                    className="rounded-xl"
                                />
                            </div>

                            <div className="space-y-3">
                                <Label className="text-base">File types</Label>
                                <div className="grid grid-cols-2 gap-3">
                                    <Pill icon={FileText} label="Pdf" />
                                    <Pill icon={File} label="Doc" />
                                    <Pill icon={ImageIcon} label="Image" />
                                    <Pill icon={FileArchive} label="Zip" />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </aside>

                {/* Main column */}
                <section className="lg:col-span-9 space-y-6">
                    <Tabs value={tab} onValueChange={setTab} className="w-full">
                        <Card className="rounded-2xl">
                            <CardHeader className="pb-2">
                                <TabsList className="rounded-xl">
                                    <TabsTrigger value="upload" className="rounded-xl">
                                        Upload
                                    </TabsTrigger>
                                    <TabsTrigger value="results" className="rounded-xl">
                                        Results
                                    </TabsTrigger>
                                    <TabsTrigger value="manage" className="rounded-xl">
                                        Manage
                                    </TabsTrigger>
                                </TabsList>
                            </CardHeader>

                            <CardContent>
                                <TabsContent value="upload" className="mt-2">
                                    <Card>
                                        <CardContent className="pt-6">
                                            <Dropzone
                                                onUploaded={async (dto) => {
                                                    const card = mapToCardItem(dto);
                                                    setItems((prev) => [card, ...prev]);
                                                    setTab("results");
                                                }}
                                            />
                                        </CardContent>
                                    </Card>
                                </TabsContent>

                                <TabsContent value="results" className="mt-2">
                                    {loading && <p>Loading…</p>}
                                    {error && (
                                        <p className="text-destructive">
                                            {String(error) || "Error loading documents"}
                                        </p>
                                    )}

                                    <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
                                        {items.map((it) => (
                                            <ResultCard
                                                key={it.id ?? it.title}
                                                item={it}
                                                onOpen={handleOpen}
                                                selected={selected.has(it.id)}
                                                onToggle={() => toggleSelect(it.id)}
                                            />
                                        ))}
                                    </div>

                                    <RightDetailPanel
                                        item={openItem}
                                        onClose={() => setOpenItem(null)}
                                        onDelete={handleDeleteFromPanel}
                                        onSave={handleSaveFromPanel}
                                    />
                                </TabsContent>

                                <TabsContent value="manage" className="mt-2 space-y-6">
                                    <Card className="rounded-2xl">
                                        <CardHeader className="pb-2">
                                            <CardTitle className="text-lg">Bulk actions</CardTitle>
                                        </CardHeader>
                                        <CardContent>
                                            <p className="text-sm text-muted-foreground">
                                                {selected.size} selected
                                                {err && (
                                                    <span className="text-destructive ml-2">
                                                        {String(err)}
                                                    </span>
                                                )}
                                            </p>
                                            <div className="flex flex-wrap gap-3 mt-4">
                                                <Button
                                                    variant="secondary"
                                                    className="rounded-xl gap-2"
                                                    type="button"
                                                    onClick={() => { }}
                                                    disabled={busy || selected.size === 0}
                                                >
                                                    <Tag className="w-4 h-4" /> Update tags
                                                </Button>
                                                <Button
                                                    variant="destructive"
                                                    className="rounded-xl gap-2"
                                                    type="button"
                                                    onClick={deleteSelected}
                                                    disabled={busy || selected.size === 0}
                                                >
                                                    <Trash2 className="w-4 h-4" />{" "}
                                                    {busy ? "Deleting…" : "Delete"}
                                                </Button>
                                                <Button
                                                    variant="outline"
                                                    className="rounded-xl"
                                                    type="button"
                                                    onClick={clearSelection}
                                                    disabled={busy || selected.size === 0}
                                                >
                                                    Clear
                                                </Button>
                                                <Button
                                                    variant="outline"
                                                    className="rounded-xl"
                                                    type="button"
                                                    onClick={selectAll}
                                                    disabled={busy || items.length === 0}
                                                >
                                                    Select all
                                                </Button>
                                            </div>
                                        </CardContent>
                                    </Card>
                                </TabsContent>
                            </CardContent>
                        </Card>
                    </Tabs>
                </section>
            </main>
        </div>
    );
}
