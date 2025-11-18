import React from "react";
import { X, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { updateDocument, deleteDocument, getDocument } from "@/api/documents";

export default function DocumentDetailDialog({ item, open, onClose }) {
    const [summary, setSummary] = React.useState("");
    const [title, setTitle] = React.useState("");
    const [tags, setTags] = React.useState("");
    const [loading, setLoading] = React.useState(false);

    // WICHTIG: frisches Dokument laden, sobald Dialog geöffnet wird
    React.useEffect(() => {
        if (!open || !item) return;

        setLoading(true);

        (async () => {
            try {
                const fresh = await getDocument(item.id);   // GET /api/Documents/{id}

                setSummary(fresh.summary || "");
                setTitle(fresh.title || "");
                setTags((fresh.tags || []).join(", "));
            } catch (e) {
                console.error("Failed to load document detail", e);

                // Fallback: Daten aus der Liste
                setSummary(item.summary || "");
                setTitle(item.title || "");
                setTags((item.tags || []).join(", "));
            } finally {
                setLoading(false);
            }
        })();
    }, [open, item && item.id]);

    if (!open || !item) return null;

    // ESC schließt Dialog
    React.useEffect(() => {
        const onKey = (e) => {
            if (e.key === "Escape") onClose?.();
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [onClose]);

    const handleSaveSummary = async () => {
        try {
            await updateDocument(item.id, { summary });
        } catch (e) {
            console.error("Failed to save summary", e);
        }
    };

    const handleSaveMetadata = async () => {
        try {
            const tagArray = tags
                .split(",")
                .map((t) => t.trim())
                .filter(Boolean);

            await updateDocument(item.id, { title, tags: tagArray });
        } catch (e) {
            console.error("Failed to save metadata", e);
        }
    };

    const handleDelete = async () => {
        try {
            await deleteDocument(item.id);
            onClose();
        } catch (e) {
            console.error("Failed to delete document", e);
        }
    };

    return (
        <div className="fixed inset-0 z-50">
            {/* Backdrop */}
            <div
                className="absolute inset-0 bg-black/40 backdrop-blur-sm"
                onClick={onClose}
            />

            {/* Dialog */}
            <div className="absolute inset-0 flex items-start justify-center p-4 sm:p-6">
                <div
                    className="w-full max-w-5xl rounded-2xl bg-background text-foreground shadow-2xl border overflow-hidden"
                    onClick={(e) => e.stopPropagation()}
                >
                    {/* Header */}
                    <div className="flex items-center justify-between px-6 py-4 border-b">
                        <div className="text-lg font-semibold truncate">
                            {title || item.title}
                        </div>
                        <button
                            onClick={onClose}
                            className="rounded-xl p-2 hover:bg-muted"
                            aria-label="Close"
                        >
                            <X className="w-5 h-5" />
                        </button>
                    </div>

                    <div className="grid lg:grid-cols-2 gap-6 p-6">
                        {/* Summary */}
                        <div className="rounded-2xl border p-4">
                            <div className="font-medium mb-3">Summary</div>
                            <textarea
                                value={summary}
                                onChange={(e) => setSummary(e.target.value)}
                                className="w-full h-40 resize-none rounded-xl border bg-background p-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                                placeholder={loading ? "Loading summary..." : ""}
                            />
                            <div className="mt-3 text-right">
                                <Button
                                    size="sm"
                                    className="rounded-xl"
                                    type="button"
                                    onClick={handleSaveSummary}
                                    disabled={loading}
                                >
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
                                    <Input
                                        value={title}
                                        onChange={(e) => setTitle(e.target.value)}
                                        className="rounded-xl"
                                    />
                                </div>
                                <div>
                                    <Label className="text-sm">Tags</Label>
                                    <Input
                                        value={tags}
                                        onChange={(e) => setTags(e.target.value)}
                                        placeholder="tag1, tag2, tag3"
                                        className="rounded-xl"
                                    />
                                </div>
                                <div className="flex gap-2 pt-2">
                                    <Button
                                        variant="destructive"
                                        className="rounded-xl gap-2"
                                        type="button"
                                        onClick={handleDelete}
                                    >
                                        <Trash2 className="w-4 h-4" /> Delete
                                    </Button>
                                    <Button
                                        className="rounded-xl"
                                        type="button"
                                        onClick={handleSaveMetadata}
                                        disabled={loading}
                                    >
                                        Save
                                    </Button>
                                </div>
                            </div>
                        </div>

                        {/* Activity-Box kannst du wie gehabt darunter lassen */}
                    </div>
                </div>
            </div>
        </div>
    );
}
