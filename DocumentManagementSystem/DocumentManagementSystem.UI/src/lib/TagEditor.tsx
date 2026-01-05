// src/components/tags/TagEditor.tsx
import React from "react";
import { parseTag, toneDotClass, uniqTags, normalizeTagToken } from "@/lib/tags";

export function TagEditor({
    value,
    onChange,
    loadSuggestions, // async suggestions from API
}: {
    value: string[];
    onChange: (next: string[]) => void;
    loadSuggestions?: (q: string) => Promise<string[]>;
}) {
    const [q, setQ] = React.useState("");
    const [open, setOpen] = React.useState(false);
    const [suggestions, setSuggestions] = React.useState<string[]>([]);
    const boxRef = React.useRef<HTMLDivElement | null>(null);
    const inputRef = React.useRef<HTMLInputElement | null>(null);

    const normalizedValue = React.useMemo(() => uniqTags(value ?? []), [value]);

    function commitTokens(tokens: string[]) {
        onChange(uniqTags([...(normalizedValue ?? []), ...tokens]));
    }

    function removeTag(tag: string) {
        const key = tag.toLowerCase();
        onChange((normalizedValue ?? []).filter((t) => t.toLowerCase() !== key));
    }

    function addFromInput() {
        const parts = q
            .split(/[,\n\t]+/g)
            .map((x) => normalizeTagToken(x))
            .filter(Boolean);

        if (parts.length) commitTokens(parts);
        setQ("");
        setOpen(false);
    }

    // outside click closes dropdown
    React.useEffect(() => {
        function onDoc(e: MouseEvent) {
            if (!boxRef.current) return;
            if (boxRef.current.contains(e.target as Node)) return;
            setOpen(false);
        }
        document.addEventListener("mousedown", onDoc);
        return () => document.removeEventListener("mousedown", onDoc);
    }, []);

    // fetch suggestions
    React.useEffect(() => {
        let alive = true;
        const run = async () => {
            if (!loadSuggestions) return;
            const qq = q.trim();
            const items = await loadSuggestions(qq);
            if (!alive) return;

            // remove already selected
            const selected = new Set(normalizedValue.map((t) => t.toLowerCase()));
            const clean = uniqTags(items).filter((t) => !selected.has(t.toLowerCase()));
            setSuggestions(clean.slice(0, 12));
        };
        const h = window.setTimeout(run, 180);
        return () => {
            alive = false;
            window.clearTimeout(h);
        };
    }, [q, loadSuggestions, normalizedValue]);

    function quickPrefix(prefix: string) {
        const cur = q.trim();
        if (!cur) setQ(prefix);
        else if (!cur.includes(":")) setQ(prefix + cur);
        inputRef.current?.focus();
        setOpen(true);
    }

    return (
        <div className="space-y-2" ref={boxRef}>
            <div className="flex flex-wrap gap-2">
                {[
                    { k: "type:", label: "type" },
                    { k: "org:", label: "org" },
                    { k: "year:", label: "year" },
                    { k: "kw:", label: "keyword" },
                ].map((x) => (
                    <button
                        key={x.k}
                        type="button"
                        onClick={() => quickPrefix(x.k)}
                        className="rounded-xl border px-2.5 py-1 text-xs hover:bg-muted"
                    >
                        {x.label}
                    </button>
                ))}
                <span className="text-xs text-muted-foreground self-center"></span>
            </div>

            <div className="relative">
                <div
                    className="rounded-2xl border bg-background px-3 py-2 flex flex-wrap gap-2 items-center"
                    onClick={() => inputRef.current?.focus()}
                >
                    {normalizedValue.length === 0 ? (
                        <span className="text-sm text-muted-foreground py-1">
                            Keine Tags. Tippe z.B. <span className="font-medium">year:2025</span> oder{" "}
                            <span className="font-medium">Q4-2025</span>.
                        </span>
                    ) : (
                        normalizedValue.map((t) => {
                            const p = parseTag(t);
                            return (
                                <span
                                    key={t}
                                    className="inline-flex items-center gap-2 rounded-xl border bg-muted/40 px-2.5 py-1 text-xs max-w-[260px]"
                                >
                                    <span className={`h-2 w-2 rounded-full ${toneDotClass(t)}`} />
                                    <span className="truncate" title={p.label}>{p.label}</span>
                                    <button
                                        type="button"
                                        className="ml-1 rounded-full px-1 hover:bg-muted"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            removeTag(t);
                                        }}
                                        aria-label={`Remove ${p.label}`}
                                        title="Remove"
                                    >
                                        ×
                                    </button>
                                </span>
                            );
                        })
                    )}

                    <input
                        ref={inputRef}
                        value={q}
                        onChange={(e) => {
                            setQ(e.target.value);
                            setOpen(true);
                        }}
                        onFocus={() => setOpen(true)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") { e.preventDefault(); addFromInput(); }
                            if (e.key === ",") { e.preventDefault(); addFromInput(); }
                            if (e.key === "Backspace" && !q) {
                                const last = normalizedValue[normalizedValue.length - 1];
                                if (last) removeTag(last);
                            }
                            if (e.key === "Escape") setOpen(false);
                        }}
                        onPaste={(e) => {
                            const text = e.clipboardData?.getData("text") ?? "";
                            if (text.includes(",") || text.includes("\n") || text.includes("\t")) {
                                e.preventDefault();
                                const parts = text
                                    .split(/[,\n\t]+/g)
                                    .map((x) => normalizeTagToken(x))
                                    .filter(Boolean);
                                if (parts.length) commitTokens(parts);
                                setQ("");
                                setOpen(false);
                            }
                        }}
                        placeholder="Tag hinzufügen…"
                        className="flex-1 min-w-[200px] bg-transparent outline-none text-sm py-1"
                    />
                </div>

                {open && suggestions.length > 0 && (
                    <div className="absolute z-20 mt-2 w-full rounded-2xl border bg-background shadow-lg overflow-hidden">
                        <div className="max-h-64 overflow-auto p-2">
                            {suggestions.map((t) => {
                                const p = parseTag(t);
                                return (
                                    <button
                                        key={t}
                                        type="button"
                                        className="w-full text-left rounded-xl px-3 py-2 hover:bg-muted flex items-center gap-2"
                                        onClick={() => {
                                            commitTokens([t]);
                                            setQ("");
                                            setOpen(false);
                                            inputRef.current?.focus();
                                        }}
                                    >
                                        <span className={`h-2 w-2 rounded-full ${toneDotClass(t)}`} />
                                        <span className="font-medium truncate flex-1">{p.label}</span>
                                        <span className="text-xs text-muted-foreground">{p.group}</span>
                                    </button>
                                );
                            })}
                        </div>
                    </div>
                )}
            </div>

            <div className="text-xs text-muted-foreground">
                <span className="font-medium">org:ÖBB</span>,{" "}
                <span className="font-medium">kw:Netzwerktechnik</span>,{" "}
                <span className="font-medium">Q4-2025</span>
            </div>
        </div>
    );
}
