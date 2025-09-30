import React from "react";
import { X, CheckCircle2, RefreshCw, Trash2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function DocumentDetailDialog({ item, open, onClose }) {
    if (!open || !item) return null;

    React.useEffect(() => {
        const onKey = (e) => e.key === "Escape" && onClose?.();
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [onClose]);

    return (
        <div className="fixed inset-0 z-50">
            {/* Backdrop */}
            <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />

            {/* Dialog */}
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
                                <Button size="sm" className="rounded-xl">Save summary</Button>
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
                                    <Input defaultValue={item.tags?.join(", ")} className="rounded-xl" />
                                </div>
                                <div className="flex gap-2 pt-2">
                                    <Button variant="destructive" className="rounded-xl gap-2">
                                        <Trash2 className="w-4 h-4" /> Delete
                                    </Button>
                                    {/*<Button variant="secondary" className="rounded-xl gap-2">*/}
                                    {/*    <RefreshCw className="w-4 h-4" /> Reprocess OCR*/}
                                    {/*</Button>*/}
                                    <Button className="rounded-xl">Save</Button>
                                </div>
                            </div>
                        </div>

                        {/* OCR Text */}
                        {/*<div className="rounded-2xl border p-4">*/}
                        {/*    <div className="font-medium mb-3">OCR Text</div>*/}
                        {/*    <div className="text-sm text-muted-foreground space-y-2 max-h-48 overflow-auto">*/}
                        {/*        <p>…preview of OCR text… In the real app this area renders the extracted text and supports find-in-text.</p>*/}
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