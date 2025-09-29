import React from "react";
import {
    Upload, FileText, Image as ImageIcon, FileArchive, File,
    Search, Filter, CheckCircle2, Settings2, HelpCircle,
    Tag, RefreshCw, Trash2
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Slider } from "@/components/ui/slider";
import { Checkbox } from "@/components/ui/checkbox";

const StatusChip = ({ label }) => (
    <span className="inline-flex items-center gap-1 text-sm text-gray-500">
        <CheckCircle2 className="w-4 h-4" /> {label}
    </span>
);

function Dropzone() {
    return (
        <div className="flex flex-col items-center justify-center text-center border-2 border-dashed rounded-2xl py-16 px-6 bg-background">
            <div className="rounded-2xl bg-muted w-16 h-16 flex items-center justify-center mb-4">
                <Upload className="w-8 h-8" />
            </div>
            <h2 className="text-2xl font-semibold mb-2">Upload document</h2>
            <p className="text-muted-foreground mb-6">Drag & drop files here, or click to browse</p>
            <div className="flex items-center gap-3">
                <Button className="rounded-xl">
                    <Upload className="w-4 h-4 mr-2" />
                    Select file
                </Button>
                <Button variant="secondary" className="rounded-xl">
                    <Settings2 className="w-4 h-4 mr-2" />
                    Try sample
                </Button>
            </div>
            <div className="flex items-center gap-6 mt-6 text-muted-foreground">
                <StatusChip label="OCR" />
                <StatusChip label="Indexed" />
                <StatusChip label="Summary" />
            </div>
        </div>
    );
}

function ResultCard({ item }) {
    return (
        <Card className="rounded-2xl h-full">
            <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-3">
                    <CardTitle className="text-base leading-tight">{item.title}</CardTitle>
                    <span className="text-xs text-muted-foreground whitespace-nowrap">{item.date}</span>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-2">{item.preview}</p>
            </CardHeader>
            <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2">
                    {item.tags.map(t => <Badge key={t} variant="secondary" className="rounded-xl">{t}</Badge>)}
                </div>
                <div className="rounded-2xl bg-muted/50 p-3">
                    <div className="font-medium mb-1">AI Summary</div>
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

// Demo-Daten bis deine API dran ist
const resultsData = [
    { id: 'q2', title: 'Quarterly Report Q2', date: '2025-08-14', preview: '…revenue up 18%…', tags: ['Finance', 'Q2'], summary: 'Revenue grew 18% YoY…', author: 'Alex Kim' },
    { id: 'brand', title: 'Brand Photography Set', date: '2025-07-03', preview: '…warm palette…', tags: ['Marketing', 'Assets'], summary: 'High-res lifestyle images…', author: 'Jamie Lee' },
    { id: 'contract', title: 'Contract - Vendor Nova LLC', date: '2025-09-02', preview: '…force majeure…', tags: ['Legal'], summary: '12-month term, NET30…', author: 'Priya Patel' },
    { id: 'legacy', title: 'Legacy ZIP Archive', date: '2024-12-11', preview: '…pending OCR jobs…', tags: ['Archive', 'Legacy'], summary: 'Contains migrated PDFs…', author: 'System' },
];


const Pill = ({ icon: Icon, label }) => (
    <button className="flex items-center gap-2 rounded-xl px-4 py-2 bg-gray-100 hover:bg-gray-200 transition">
        <Icon className="w-4 h-4" />
        {label}
    </button>
);

export default function Dashboard() {
    return (
        <div className="min-h-screen bg-white text-gray-900">
            {/* Top bar */}
            <header className="sticky top-0 z-30 border-b bg-white/80 backdrop-blur">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-3">
                    <div className="ml-auto flex items-center gap-2 w-full max-w-2xl">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                            <input
                                className="w-full pl-9 pr-3 h-10 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200 outline-none"
                                placeholder="Search documents (full-text & fuzzy)…"
                            />
                        </div>
                        <button className="rounded-xl border px-3 h-10 inline-flex items-center gap-2 hover:bg-gray-50">
                            <Filter className="w-4 h-4" /> Filters
                        </button>
                        <button className="rounded-xl p-2 hover:bg-gray-100">
                            <HelpCircle className="w-5 h-5" />
                        </button>
                    </div>
                </div>
            </header>

            {/* Main grid */}
            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 grid grid-cols-1 lg:grid-cols-12 gap-6">
                {/* Sidebar */}
                <aside className="lg:col-span-3">
                    <div className="rounded-2xl border">
                        <div className="p-5 pb-2">
                            <h3 className="text-lg font-semibold">Filters</h3>
                        </div>
                        <div className="p-5 space-y-6 pt-3">
                            {/* Fuzzy search */}
                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <span className="text-base">Fuzzy search</span>
                                    <label className="relative inline-flex items-center cursor-pointer">
                                        <input type="checkbox" className="sr-only peer" />
                                        <div className="w-11 h-6 bg-gray-200 rounded-full peer peer-checked:bg-indigo-600 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:h-5 after:w-5 after:rounded-full after:transition-all peer-checked:after:translate-x-5"></div>
                                    </label>
                                </div>
                                <input
                                    placeholder="Typo tolerance, synonyms"
                                    className="w-full h-10 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200 outline-none px-3"
                                />
                            </div>

                            {/* File types */}
                            <div className="space-y-3">
                                <span className="text-base">File types</span>
                                <div className="grid grid-cols-2 gap-3">
                                    <Pill icon={FileText} label="Pdf" />
                                    <Pill icon={File} label="Doc" />
                                    <Pill icon={ImageIcon} label="Image" />
                                    <Pill icon={FileArchive} label="Zip" />
                                </div>
                            </div>

                            {/* Recency */}
                            <div className="space-y-3">
                                <span className="text-base">Recency boost</span>
                                <input
                                    id="recency"
                                    type="range"
                                    defaultValue={30}
                                    className="w-full"
                                    onInput={(e) =>
                                        e.currentTarget.style.setProperty("--p", `${e.currentTarget.value}%`)
                                    }
                                />
                                <p className="text-sm text-gray-500">Bias results toward newer content</p>
                            </div>
                        </div>
                    </div>
                </aside>

                {/* Main column */}
                <section className="lg:col-span-9 space-y-6">
                    <Tabs defaultValue="results" className="w-full">
                        <Card className="rounded-2xl">
                            <CardHeader className="pb-2">
                                <TabsList className="rounded-xl">
                                    <TabsTrigger value="upload" className="rounded-xl">Upload</TabsTrigger>
                                    <TabsTrigger value="results" className="rounded-xl">Results</TabsTrigger>
                                    <TabsTrigger value="manage" className="rounded-xl">Manage</TabsTrigger>
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
                                        {resultsData.map(it => <ResultCard key={it.id} item={it} />)}
                                    </div>
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
                                                <Button variant="secondary" className="rounded-xl gap-2">
                                                    <Tag className="w-4 h-4" /> Update tags
                                                </Button>
                                                <Button variant="secondary" className="rounded-xl gap-2">
                                                    <RefreshCw className="w-4 h-4" /> Reprocess OCR
                                                </Button>
                                                <Button variant="destructive" className="rounded-xl gap-2">
                                                    <Trash2 className="w-4 h-4" /> Delete
                                                </Button>
                                            </div>
                                        </CardContent>
                                    </Card>

                                    {/* Index & Pipeline */}
                                    <Card className="rounded-2xl">
                                        <CardHeader className="pb-2">
                                            <CardTitle className="text-lg">Index & Pipeline</CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-4">
                                            <div className="flex items-center justify-between">
                                                <span>ElasticSearch index</span>
                                                <Badge className="rounded-xl bg-foreground text-background">docs-ai-001</Badge>
                                            </div>
                                            <div className="flex items-center justify-between">
                                                <span>Fuzzy matching</span>
                                                <Switch defaultChecked />
                                            </div>
                                            <div className="flex items-center justify-between">
                                                <span>Synonyms</span>
                                                <Badge variant="secondary" className="rounded-xl">Enabled</Badge>
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
