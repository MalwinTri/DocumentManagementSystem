import React from "react";

export default function DocumentDetails({ doc, onClose }) {
    if (!doc) return null;

    return (
        <div className="fixed top-0 right-0 h-full w-96 bg-white shadow-lg border-l z-50 flex flex-col">
            {/* Header */}
            <div className="p-4 border-b flex items-center justify-between">
                <h2 className="text-lg font-semibold">{doc.title}</h2>
                <button
                    onClick={onClose}
                    className="text-slate-500 hover:text-slate-800"
                >
                    ✕
                </button>
            </div>

            {/* Body */}
            <div className="p-4 flex-1 overflow-y-auto space-y-4 text-slate-700">
                <div className="text-sm text-slate-500">
                    Typ: {doc.type} • Datum: {doc.date}
                </div>

                <div>
                    <h3 className="font-medium mb-1">Tags</h3>
                    <div className="flex flex-wrap gap-2">
                        {doc.tags.map((tag) => (
                            <span
                                key={tag}
                                className="bg-slate-100 px-2 py-1 rounded-full text-xs"
                            >
                                {tag}
                            </span>
                        ))}
                    </div>
                </div>

                <div>
                    <h3 className="font-medium mb-1">OCR-Text</h3>
                    <pre className="bg-slate-50 p-2 rounded text-xs max-h-40 overflow-auto">
                        {doc.ocr}
                    </pre>
                </div>

                <div>
                    <h3 className="font-medium mb-1">GenAI-Zusammenfassung</h3>
                    <p className="text-sm">{doc.summary}</p>
                </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t flex justify-end">
                <button
                    onClick={onClose}
                    className="px-4 py-2 rounded bg-indigo-600 text-white text-sm hover:bg-indigo-700"
                >
                    Schließen
                </button>
            </div>
        </div>
    );
}