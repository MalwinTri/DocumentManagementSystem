import React, { useState, useMemo } from "react";

const MOCK_DOCS = [
    { id: "1", title: "Quarterly Report Q2", type: "pdf", summary: "Revenue grew 18% YoY." },
    { id: "2", title: "Contract - Vendor Nova LLC", type: "doc", summary: "12-month term, NET30." },
];

export default function DocumentManagementDashboard() {
    const [query, setQuery] = useState("");
    const filtered = useMemo(() => {
        const q = query.toLowerCase();
        return MOCK_DOCS.filter(
            (d) =>
                d.title.toLowerCase().includes(q) ||
                d.summary.toLowerCase().includes(q)
        );
    }, [query]);

    return (
        <div>
            <h1 className="text-2xl font-bold mb-4">📂 Document Management</h1>
            <input
                type="text"
                placeholder="Search..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="border rounded px-3 py-2 w-full mb-4"
            />
            {filtered.length === 0 ? (
                <p>No results found.</p>
            ) : (
                <ul className="space-y-2">
                    {filtered.map((doc) => (
                        <li key={doc.id} className="p-3 border rounded shadow-sm">
                            <h2 className="font-semibold">{doc.title}</h2>
                            <p className="text-sm text-gray-600">{doc.summary}</p>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
