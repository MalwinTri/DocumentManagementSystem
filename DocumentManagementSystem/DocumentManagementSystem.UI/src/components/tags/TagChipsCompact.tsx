import React from "react";
import { parseTag, toneDotClass, visibleTags } from "./tagUtils";

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
        return [...arr].sort((a, b) => parseTag(a).label.length - parseTag(b).label.length);
    }, [tags]);

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
                        <span
                            key={t}
                            className="inline-flex items-center gap-2 rounded-xl border bg-muted/40 px-2.5 py-1 text-xs max-w-[220px]"
                            title={p.label}
                        >
                            <span className={`h-2 w-2 rounded-full ${toneDotClass(t)}`} />
                            <span className="truncate">{p.label}</span>
                        </span>
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
