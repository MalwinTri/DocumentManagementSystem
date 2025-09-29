import React from "react";
import {
    Upload, FileText, Image as ImageIcon, FileArchive, File,
    Search, Filter, CheckCircle2, Settings2, HelpCircle
} from "lucide-react";

const StatusChip = ({ label }) => (
    <span className="inline-flex items-center gap-1 text-sm text-gray-500">
        <CheckCircle2 className="w-4 h-4" /> {label}
    </span>
);

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
                    <div className="text-2xl font-bold tracking-tight">
                        Docs <span className="text-indigo-600">AI</span>
                    </div>
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
                    <div className="rounded-2xl border">
                        <div className="p-5 pb-0">
                            <div className="inline-flex rounded-xl border p-1 gap-1 text-sm">
                                <button className="px-3 py-1.5 rounded-lg bg-indigo-600 text-white">Upload</button>
                                <button className="px-3 py-1.5 rounded-lg hover:bg-gray-100">Results</button>
                                <button className="px-3 py-1.5 rounded-lg hover:bg-gray-100">Manage</button>
                            </div>
                        </div>
                        <div className="p-5">
                            {/* Dropzone */}
                            <div className="flex flex-col items-center justify-center text-center border-2 border-dashed rounded-2xl py-16 px-6 bg-white">
                                <div className="rounded-2xl bg-gray-100 w-16 h-16 flex items-center justify-center mb-4">
                                    <Upload className="w-8 h-8" />
                                </div>
                                <h2 className="text-2xl font-semibold mb-2">Upload document</h2>
                                <p className="text-gray-500 mb-6">Drag & drop files here, or click to browse</p>
                                <div className="flex items-center gap-3">
                                    <button className="rounded-xl px-4 h-10 bg-indigo-600 text-white inline-flex items-center gap-2">
                                        <Upload className="w-4 h-4" /> Select file
                                    </button>
                                    <button className="rounded-xl px-4 h-10 bg-gray-100 inline-flex items-center gap-2 hover:bg-gray-200">
                                        <Settings2 className="w-4 h-4" /> Try sample
                                    </button>
                                </div>
                                <div className="flex items-center gap-6 mt-6 text-gray-600">
                                    <StatusChip label="OCR" />
                                    <StatusChip label="Indexed" />
                                    <StatusChip label="Summary" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="grid md:grid-cols-2 gap-6">
                        {/* Pipeline status */}
                        <div className="rounded-2xl border">
                            <div className="p-5 pb-2">
                                <h3 className="text-lg font-semibold">Pipeline status</h3>
                            </div>
                            <div className="p-5 space-y-3 text-sm">
                                <div className="flex items-center justify-between">
                                    <span>OCR</span>
                                    <span className="rounded-xl bg-gray-100 px-2 py-1">Automatic</span>
                                </div>
                                <div className="flex items-center justify-between">
                                    <span>Indexing (ElasticSearch)</span>
                                    <span className="rounded-xl bg-gray-100 px-2 py-1">Enabled</span>
                                </div>
                                <div className="flex items-center justify-between">
                                    <span>Summarization</span>
                                    <span className="rounded-xl bg-gray-100 px-2 py-1">Enabled</span>
                                </div>
                            </div>
                        </div>

                        {/* Tips */}
                        <div className="rounded-2xl border">
                            <div className="p-5 pb-2">
                                <h3 className="text-lg font-semibold">Tips</h3>
                            </div>
                            <div className="p-5 text-sm text-gray-600">
                                <p>Use PDFs for best OCR fidelity. Add tags to improve recall. You can edit summaries after upload.</p>
                            </div>
                        </div>
                    </div>
                </section>
            </main>
        </div>
    );
}
