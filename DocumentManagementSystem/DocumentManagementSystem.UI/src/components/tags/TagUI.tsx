// src/components/tags/TagUI.tsx
import * as React from "react";
import { Badge } from "@/components/ui/badge";
import { parseTag, toneDotClass, visibleTags } from "@/lib/tags";
import { cn } from "@/lib/utils";

const TAG_GROUP_ORDER = ["type", "org", "year", "quarter", "kw", "tag", "other"] as const;

function tagPriority(raw: string) {
    const p = parseTag(raw);
    const gi = TAG_GROUP_ORDER.indexOf(p.group as any);
    const groupScore = gi === -1 ? 99 : gi;

    const keywordPenalty = p.group === "kw" ? 20 : 0;
    const lengthPenalty = Math.min(20, Math.floor((p.label.length || 0) / 8));

    return groupScore + keywordPenalty + lengthPenalty;
}

function groupTags(tags: string[]) {
    const groups = new Map<string, ReturnType<typeof parseTag>[]>();

    for (const t of tags ?? []) {
        const p = parseTag(t);
        const key = p.group || "other";
        if (!groups.has(key)) groups.set(key, []);
        groups.get(key)!.push(p);
    }

    const keys = Array.from(groups.keys()).sort((a, b) => {
        const ai = TAG_GROUP_ORDER.indexOf(a as any);
        const bi = TAG_GROUP_ORDER.indexOf(b as any);
        return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi) || a.localeCompare(b);
    });

    const labelFor = (k: string) =>
        k === "type"
            ? "Type"
            : k === "org"
                ? "Organisation"
                : k === "kw"
                    ? "Keywords"
                    : k === "year"
                        ? "Year"
                        : k === "quarter"
                            ? "Quarter"
                            : k === "tag"
                                ? "Tags"
                                : "Other";

    return keys.map((k) => ({
        key: k,
        label: labelFor(k),
        items: (groups.get(k) ?? []).sort((a, b) => a.label.localeCompare(b.label)),
    }));
}

function Accordion({
    title,
    right,
    defaultOpen = false,
    children,
}: {
    title: string;
    right?: React.ReactNode;
    defaultOpen?: boolean;
    children: React.ReactNode;
}) {
    const [open, setOpen] = React.useState(defaultOpen);

    return (
        <div className="rounded-2xl border overflow-hidden">
            <button
                type="button"
                className="w-full flex items-center justify-between gap-3 px-4 py-3 hover:bg-muted/40"
                onClick={() => setOpen((v) => !v)}
            >
                <div className="min-w-0 text-left">
                    <div className="font-medium truncate">{title}</div>
                </div>
                <div className="flex items-center gap-2">
                    {right}
                    <span className="text-muted-foreground">{open ? "▾" : "▸"}</span>
                </div>
            </button>

            {open && <div className="px-4 pb-4">{children}</div>}
        </div>
    );
}

export function TagChipsCompact({
    tags,
    max = 6,
    onMore,
}: {
    tags: string[];
    max?: number;
    onMore?: () => void;
}) {
    const [expanded, setExpanded] = React.useState(false);

    const sorted = React.useMemo(() => {
        const arr = visibleTags((tags ?? []).filter(Boolean));
        return [...arr].sort((a, b) => tagPriority(a) - tagPriority(b) || a.localeCompare(b));
    }, [tags, max]);

    React.useEffect(() => setExpanded(false), [sorted.length, max]);

    const head = sorted.slice(0, max);
    const rest = Math.max(0, sorted.length - head.length);
    const visible = onMore ? head : expanded ? sorted : head;

    return (
        <div className="flex flex-wrap gap-2">
            {visible.length ? (
                visible.map((t) => {
                    const p = parseTag(t);
                    return (
                        <Badge
                            key={t}
                            variant="secondary"
                            className="rounded-xl inline-flex items-center gap-2 max-w-[220px]"
                            title={p.label}
                        >
                            <span className={cn("h-2 w-2 rounded-full", toneDotClass(t))} />
                            <span className="truncate">{p.label}</span>
                        </Badge>
                    );
                })
            ) : (
                <span className="text-xs text-muted-foreground">No tags</span>
            )}

            {rest > 0 && !expanded && (
                <button
                    type="button"
                    className="inline-flex items-center rounded-xl border px-2.5 py-1 text-xs hover:bg-muted"
                    onClick={(e) => {
                        e.stopPropagation();
                        if (onMore) onMore();
                        else setExpanded(true);
                    }}
                >
                    +{rest} more
                </button>
            )}

            {!onMore && expanded && sorted.length > max && (
                <button
                    type="button"
                    className="inline-flex items-center rounded-xl border px-2.5 py-1 text-xs hover:bg-muted"
                    onClick={(e) => {
                        e.stopPropagation();
                        setExpanded(false);
                    }}
                >
                    Show less
                </button>
            )}
        </div>
    );
}

export function TagGroups({ tags }: { tags: string[] }) {
    const filtered = React.useMemo(() => visibleTags(tags ?? []), [tags]);
    const groups = React.useMemo(() => groupTags(filtered), [filtered]);

    if (!filtered.length) return <div className="text-sm text-muted-foreground">No tags.</div>;

    return (
        <div className="space-y-3">
            {groups.map((g) => (
                <Accordion
                    key={g.key}
                    title={g.label}
                    defaultOpen={false}
                    right={<span className="text-xs text-muted-foreground">{g.items.length}</span>}
                >
                    <div className="flex flex-wrap gap-2 pt-3">
                        {g.items.map((p) => (
                            <Badge
                                key={p.raw}
                                variant="secondary"
                                className="rounded-xl inline-flex items-center gap-2 max-w-[260px]"
                                title={p.label}
                            >
                                <span className={cn("h-2 w-2 rounded-full", toneDotClass(p.raw))} />
                                <span className="truncate">{p.label}</span>
                            </Badge>
                        ))}
                    </div>
                </Accordion>
            ))}
        </div>
    );
}
