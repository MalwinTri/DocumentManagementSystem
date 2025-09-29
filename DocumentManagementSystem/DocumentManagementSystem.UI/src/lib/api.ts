// src/lib/api.ts
import axios from 'axios'

export const api = axios.create({ baseURL: '/api' })

export type DocType = 'Pdf' | 'Image' | 'Doc' | 'Zip'
export type Doc = {
    id: string
    title: string
    type: DocType
    tags: string[]
    author: string
    date: string
    summary: string
    snippet: string
    status: { ocr: boolean; indexed: boolean; summarized: boolean }
}

export async function fetchDocuments(params: {
    query?: string
    types?: string[]
    fuzzy?: boolean
    recencyBoost?: number
}) {
    const res = await api.get<Doc[]>('/documents', { params })
    return res.data
}

// (weitere Endpoints optional)
