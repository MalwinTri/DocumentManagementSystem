// src/pages/Dashboard.jsx
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
    Settings2,
    HelpCircle,
    Tag,
    RefreshCw,
    Trash2,
    X,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Slider } from "@/components/ui/slider";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

function IconButton({ children, className = "", ...props }) {
    return (
        <Button variant="ghost" size="icon" className={`rounded-xl ${className}`} {...props}>
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

function StatusChip({ label }) {
    return (
        <span className="inline-flex items-center gap-1 text-sm text-muted-foreground">
            <CheckCircle2 className="w-4 h-4" /> {label}
        </span>
    );
}

function Dropzone() {
    return (
        <div className="flex flex-col items-center justify-center text-center border-2 border-dashed rounded-2xl py-16 px-6 bg-background">
            <div className="rounded-2xl bg-muted w-16 h-16 flex items-center justify-center mb-4">
                <Upload className="w-8 h-8" />
            </div>
            <h2 className="text-2xl font-semibold mb-2">Upload document</h2>
            <p className="text-muted-foreground mb-6">Drag & drop files here, or click to browse</p>
            <div className="flex items-center gap-3">
                <Button className="rounded-xl" type="button">
                    <Upload className="w-4 h-4 mr-2" />
                    Select file
                </Button>
            </div>
        </div>
    );
}

function ResultCard({ item, onOpen }) {
    return (
        <Card className="rounded-2xl h-full cursor-pointer" onClick={onOpen}>
            <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-3">
                    <CardTitle className="text-base leading-tight">{item.title}</CardTitle>
                    <span className="text-xs text-muted-foreground whitespace-nowrap">{item.date}</span>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-2">{item.preview}</p>
            </CardHeader>
            <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2">
                    {item.tags.map((t) => (
                        <Badge key={t} variant="secondary" className="rounded-xl">
                            {t}
                        </Badge>
                    ))}
                </div>
                <div className="rounded-2xl bg-muted/50 p-3">
                    <div className="font-medium mb-1 flex items-center gap-2">
                        <span className="inline-flex items-center justify-center w-5 h-5 rounded-md bg-muted">
                            <SparklesIcon />
                        </span>
                        AI Summary
                    </div>
                    <p className="text-sm text-muted-foreground line-clamp-3">{item.summary}</p>
                </div>
                <div className="flex items-end justify-between">
                    <div className="flex flex-wrap gap-2">
                        <Badge className="rounded-xl bg-emerald-100 text-emerald-800 hover:bg-emerald-100">OCR</Badge>
                        <Badge className="rounded-xl bg-emerald-100 text-emerald-800 hover:bg-emerald-100">Indexed</Badge>
                        <Badge className="rounded-xl bg-emerald-100 text-emerald-800 hover:bg-emerald-100">Summarized</Badge>
                    </div>
                    <span className="text-xs text-muted-foreground">By {item.author}</span>
                </div>
            </CardContent>
        </Card>
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

function DocumentDetailDialog({ item, open, onClose }) {
    if (!open || !item) return null;

    React.useEffect(() => {
        const onKey = (e) => e.key === "Escape" && onClose?.();
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [onClose]);

    return (
        <div className="fixed inset-0 z-50">
            <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
            <div className="absolute inset-0 flex items-start justify-center p-4 sm:p-6">
                <div className="w-full max-w-5xl rounded-2xl bg-background text-foreground shadow-2xl border overflow-hidden">
                    {/* Title bar */}
                    <div className="flex items-center justify-between px-6 py-4 border-b">
                        <div className="text-lg font-semibold truncate">{item.title}</div>
                        <button onClick={onClose} className="rounded-xl p-2 hover:bg-muted" aria-label="Close">
                            <X className="w-5 h-5" />
                        </button>
                    </div>

                    <div className="grid lg:grid-cols-2 gap-6 p-6">
                        {/* Summary */}
                        <div className="rounded-2xl border p-4">
                            <div className="font-medium mb-3">Summary</div>
                            <textarea
                                defaultValue={item.summary}
                                className="w-full h-40 resize-none rounded-xl border bg-background p-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                            />
                            <div className="mt-3 text-right">
                                <Button size="sm" className="rounded-xl" type="button">
                                    Save summary
                                </Button>
                            </div>
                        </div>

                        {/* Metadata */}
                        <div className="rounded-2xl border p-4">
                            <div className="font-medium mb-3">Metadata</div>
                            <div className="space-y-3">
                                <div>
                                    <Label className="text-sm">Title</Label>
                                    <Input defaultValue={item.title} className="rounded-xl" />
                                </div>
                                <div>
                                    <Label className="text-sm">Author</Label>
                                    <Input defaultValue={item.author} className="rounded-xl" />
                                </div>
                                <div>
                                    <Label className="text-sm">Tags</Label>
                                    <Input defaultValue={item.tags.join(", ")} className="rounded-xl" />
                                </div>
                                <div className="flex gap-2 pt-2">
                                    <Button variant="destructive" className="rounded-xl gap-2" type="button">
                                        <Trash2 className="w-4 h-4" />
                                        Delete
                                    </Button>
                                    <Button variant="secondary" className="rounded-xl gap-2" type="button">
                                        <RefreshCw className="w-4 h-4" />
                                        Reprocess OCR
                                    </Button>
                                    <Button className="rounded-xl" type="button">
                                        Save
                                    </Button>
                                </div>
                            </div>
                        </div>

                        {/* OCR Text */}
                        {/*<div className="rounded-2xl border p-4">*/}
                        {/*    <div className="font-medium mb-3">OCR Text</div>*/}
                        {/*    <div className="text-sm text-muted-foreground space-y-2 max-h-48 overflow-auto">*/}
                        {/*        <p>*/}
                        {/*            …preview of OCR text… In the real app this area renders the extracted text and supports*/}
                        {/*            find-in-text.*/}
                        {/*        </p>*/}
                        {/*    </div>*/}
                        {/*</div>*/}

                        {/* Activity */}
                        <div className="rounded-2xl border p-4">
                            <div className="font-medium mb-3">Activity</div>
                            <ul className="text-sm space-y-2">
                                <li className="flex items-center gap-2">
                                    <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Uploaded on {item.date}
                                </li>
                                <li className="flex items-center gap-2">
                                    <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Indexed in ElasticSearch
                                </li>
                                <li className="flex items-center gap-2">
                                    <CheckCircle2 className="w-4 h-4 text-emerald-600" /> Summary generated
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}


export default function Dashboard() {
    const [openItem, setOpenItem] = React.useState(null);

    const resultsData = [
        {
            id: "q2",
            title: "Quarterly Report Q2",
            date: "2025-08-14",
            preview: "…revenue up 18% YoY… retention improved by 4.2% … OCR extracted 12…",
            tags: ["Finance", "Q2"],
            summary:
                "Revenue grew 18% YoY. Key drivers include EMEA expansion and pricing updates. Risks: FX volatility.",
            author: "Alex Kim",
        },
        {
            id: "brand",
            title: "Brand Photography Set",
            date: "2025-07-03",
            preview: "…SKU 2211 featured prominently… warm palette …",
            tags: ["Marketing", "Assets"],
            summary:
                "High-res lifestyle images for autumn campaign. Includes product close-ups and set notes.",
            author: "Jamie Lee",
        },
        {
            id: "contract",
            title: "Contract - Vendor Nova LLC",
            date: "2025-09-02",
            preview: "…governing law: AT… force majeure …",
            tags: ["Legal"],
            summary:
                "12-month term, NET30, termination with 30-day notice, liability capped.",
            author: "Priya Patel",
        },
        {
            id: "legacy",
            title: "Legacy ZIP Archive",
            date: "2024-12-11",
            preview: "…pending OCR jobs: 3/27…",
            tags: ["Archive", "Legacy"],
            summary: "Contains migrated PDFs and images. OCR pending for 3 items.",
            author: "System",
        },
    ];

    return (
        <div className="min-h-screen bg-background text-foreground">
            {/* Top bar */}
            <header className="sticky top-0 z-30 border-b bg-background/80 backdrop-blur supports-[backdrop-filter]:bg-background/60">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-3">
                    <div className="ml-auto flex items-center gap-2 w-full max-w-2xl">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                            <Input placeholder="Search documents (full-text & fuzzy)…" className="pl-9 rounded-xl" />
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
                            {/* Fuzzy search */}
                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <Label className="text-base">Fuzzy search</Label>
                                    <Switch id="fuzzy" />
                                </div>
                                <Input placeholder="Typo tolerance, synonyms" className="rounded-xl" />
                            </div>

                            {/* File types */}
                            <div className="space-y-3">
                                <Label className="text-base">File types</Label>
                                <div className="grid grid-cols-2 gap-3">
                                    <Pill icon={FileText} label="Pdf" />
                                    <Pill icon={File} label="Doc" />
                                    <Pill icon={ImageIcon} label="Image" />
                                    <Pill icon={FileArchive} label="Zip" />
                                </div>
                            </div>

                            {/* Recency boost */}
                            {/*<div className="space-y-3">*/}
                            {/*    <Label className="text-base">Recency boost</Label>*/}
                            {/*    <Slider defaultValue={[25]} step={1} max={100} className="px-2" />*/}
                            {/*    <p className="text-sm text-muted-foreground">Bias results toward newer content</p>*/}
                            {/*</div>*/}
                        </CardContent>
                    </Card>
                </aside>

                {/* Main column */}
                <section className="lg:col-span-9 space-y-6">
                    <Tabs defaultValue="results" className="w-full">
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
                                            <Dropzone />
                                        </CardContent>
                                    </Card>
                                </TabsContent>

                                <TabsContent value="results" className="mt-2">
                                    <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
                                        {resultsData.map((it) => (
                                            <ResultCard key={it.id} item={it} onOpen={() => setOpenItem(it)} />
                                        ))}
                                    </div>

                                    {/* Detail Dialog */}
                                    <DocumentDetailDialog
                                        item={openItem}
                                        open={!!openItem}
                                        onClose={() => setOpenItem(null)}
                                    />
                                </TabsContent>

                                <TabsContent value="manage" className="mt-2 space-y-6">
                                    {/* Bulk actions */}
                                    <Card className="rounded-2xl">
                                        <CardHeader className="pb-2">
                                            <CardTitle className="text-lg">Bulk actions</CardTitle>
                                        </CardHeader>
                                        <CardContent>
                                            <p className="text-sm text-muted-foreground">
                                                Select multiple documents from Results to run actions.
                                            </p>
                                            <div className="flex flex-wrap gap-3 mt-4">
                                                <Button variant="secondary" className="rounded-xl gap-2" type="button">
                                                    <Tag className="w-4 h-4" /> Update tags
                                                </Button>
                                                {/*<Button variant="secondary" className="rounded-xl gap-2" type="button">*/}
                                                {/*    <RefreshCw className="w-4 h-4" /> Reprocess OCR*/}
                                                {/*</Button>*/}
                                                <Button variant="destructive" className="rounded-xl gap-2" type="button">
                                                    <Trash2 className="w-4 h-4" /> Delete
                                                </Button>
                                            </div>
                                        </CardContent>
                                    </Card>

                                    {/* Index & Pipeline */}
                                    {/*<Card className="rounded-2xl">*/}
                                    {/*    <CardHeader className="pb-2">*/}
                                    {/*        <CardTitle className="text-lg">Index & Pipeline</CardTitle>*/}
                                    {/*    </CardHeader>*/}
                                    {/*    <CardContent className="space-y-4">*/}
                                    {/*        <div className="flex items-center justify-between">*/}
                                    {/*            <span>ElasticSearch index</span>*/}
                                    {/*            <Badge className="rounded-xl bg-foreground text-background">docs-ai-001</Badge>*/}
                                    {/*        </div>*/}
                                    {/*        <div className="flex items-center justify-between">*/}
                                    {/*            <span>Fuzzy matching</span>*/}
                                    {/*            <Switch defaultChecked />*/}
                                    {/*        </div>*/}
                                    {/*        <div className="flex items-center justify-between">*/}
                                    {/*            <span>Synonyms</span>*/}
                                    {/*            <Badge variant="secondary" className="rounded-xl">*/}
                                    {/*                Enabled*/}
                                    {/*            </Badge>*/}
                                    {/*        </div>*/}
                                    {/*    </CardContent>*/}
                                    {/*</Card>*/}
                                </TabsContent>
                            </CardContent>
                        </Card>
                    </Tabs>

                    {/* Untere Cards */}
                    {/*<div className="grid md:grid-cols-2 gap-6">*/}
                    {/*    <Card className="rounded-2xl">*/}
                    {/*        <CardHeader className="pb-2">*/}
                    {/*            <CardTitle className="text-lg">Pipeline status</CardTitle>*/}
                    {/*        </CardHeader>*/}
                    {/*        <CardContent className="space-y-3">*/}
                    {/*            <div className="flex items-center justify-between">*/}
                    {/*                <span>OCR</span>*/}
                    {/*                <Badge variant="secondary" className="rounded-xl">*/}
                    {/*                    Automatic*/}
                    {/*                </Badge>*/}
                    {/*            </div>*/}
                    {/*            <div className="flex items-center justify-between">*/}
                    {/*                <span>Indexing (ElasticSearch)</span>*/}
                    {/*                <Badge variant="secondary" className="rounded-xl">*/}
                    {/*                    Enabled*/}
                    {/*                </Badge>*/}
                    {/*            </div>*/}
                    {/*            <div className="flex items-center justify-between">*/}
                    {/*                <span>Summarization</span>*/}
                    {/*                <Badge variant="secondary" className="rounded-xl">*/}
                    {/*                    Enabled*/}
                    {/*                </Badge>*/}
                    {/*            </div>*/}
                    {/*        </CardContent>*/}
                    {/*    </Card>*/}

                    {/*    <Card className="rounded-2xl">*/}
                    {/*        <CardHeader className="pb-2">*/}
                    {/*            <CardTitle className="text-lg">Tips</CardTitle>*/}
                    {/*        </CardHeader>*/}
                    {/*        <CardContent className="space-y-2 text-sm text-muted-foreground">*/}
                    {/*            <p>*/}
                    {/*                Use PDFs for best OCR fidelity. Add tags to improve recall. You can edit summaries after upload.*/}
                    {/*            </p>*/}
                    {/*        </CardContent>*/}
                    {/*    </Card>*/}
                    {/*</div>*/}
                </section>
            </main>
        </div>
    );
}
