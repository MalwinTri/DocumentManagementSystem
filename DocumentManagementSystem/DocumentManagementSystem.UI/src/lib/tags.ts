// src/lib/tags.ts
export type ParsedTag = { raw: string; group: string; label: string };

export function parseTag(raw: unknown): ParsedTag {
    const s = String(raw ?? "").trim();
    if (!s) return { raw: "", group: "other", label: "" };

    // Q4-2025 ohne ":" unterstützen
    if (/^q[1-4]-\d{4}$/i.test(s)) return { raw: s, group: "quarter", label: s.toUpperCase() };

    const idx = s.indexOf(":");
    if (idx > 0) {
        const group = s.slice(0, idx).toLowerCase();
        const label = s.slice(idx + 1).trim();
        return { raw: s, group, label: label || s };
    }

    return { raw: s, group: "tag", label: s };
}

// "type:other" ausblenden (case-insensitive)
export function visibleTags(tags: string[]) {
    return (tags ?? []).filter((t) => {
        const s = String(t ?? "").trim().toLowerCase();
        return s !== "type:other";
    });
}

function hashString(s: string) {
    let h = 5381;
    for (let i = 0; i < s.length; i++) h = (h * 33) ^ s.charCodeAt(i);
    return Math.abs(h);
}

const PALETTE_DOT = [
    "bg-teal-500",
    "bg-emerald-500",
    "bg-cyan-500",
    "bg-lime-500",
    "bg-amber-500",
    "bg-orange-500",
    "bg-rose-500",
    "bg-pink-500",
    "bg-fuchsia-500",
    "bg-violet-500",
    "bg-sky-500",
    "bg-indigo-500",
];

const PALETTE_BAR = [
    "bg-teal-500/55",
    "bg-emerald-500/55",
    "bg-cyan-500/55",
    "bg-lime-500/55",
    "bg-amber-500/60",
    "bg-orange-500/60",
    "bg-rose-500/55",
    "bg-pink-500/55",
    "bg-fuchsia-500/55",
    "bg-violet-500/55",
    "bg-sky-500/55",
    "bg-indigo-500/55",
];

const FIXED = {
    year: { dot: "bg-slate-400", bar: "bg-slate-400/40" },
    quarter: { dot: "bg-indigo-500", bar: "bg-indigo-500/55" },
} as const;

export function toneDotClass(tag: string) {
    const { group, raw, label } = parseTag(tag);
    if (group === "year") return FIXED.year.dot;
    if (group === "quarter") return FIXED.quarter.dot;

    const key = String(raw || label || "");
    return PALETTE_DOT[hashString(key) % PALETTE_DOT.length];
}

export function toneBarClass(tag: string) {
    const { group, raw, label } = parseTag(tag);
    if (group === "year") return FIXED.year.bar;
    if (group === "quarter") return FIXED.quarter.bar;

    const key = String(raw || label || "");
    return PALETTE_BAR[hashString(key) % PALETTE_BAR.length];
}

export function normalizeTagToken(token: unknown) {
    const t = String(token ?? "").trim();
    if (!t) return "";

    const idx = t.indexOf(":");
    if (idx > 0) {
        const g = t.slice(0, idx).trim().toLowerCase();
        const v = t.slice(idx + 1).trim();
        return v ? `${g}:${v}` : g;
    }
    return t;
}

export function uniqTags(arr: unknown[]) {
    const out: string[] = [];
    const seen = new Set<string>();

    for (const raw of arr ?? []) {
        const t = normalizeTagToken(raw);
        if (!t) continue;

        const key = t.toLowerCase();
        if (seen.has(key)) continue;

        seen.add(key);
        out.push(t);
    }
    return out;
}