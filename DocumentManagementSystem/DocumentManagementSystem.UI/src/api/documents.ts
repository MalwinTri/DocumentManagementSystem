import { api } from "./client";

export type DocumentDto = {
  id: string;
  title: string;
  description?: string | null;
  author?: string | null;
  tags?: string[];
  createdAt?: string; 
  ocrText?: string | null;  
  summary?: string | null; 
};

export type PageDto<T> = { items: T[]; total: number; page: number; size: number };

export async function updateDocument(id: string, payload: Partial<DocumentDto>) {
    return api.patch<DocumentDto>(`/api/Documents/${id}`, payload);
}


export async function listDocuments(page = 0, size = 20): Promise<PageDto<DocumentDto>> {
  return api.get<PageDto<DocumentDto>>(`/api/Documents?page=${page}&size=${size}`);
}


export async function getDocument(id: string) {
  return api.get<DocumentDto>(`/api/Documents/${id}`);
}

export async function uploadDocument(file: File, meta?: { title?: string; description?: string; tags?: string[] }) {
  const form = new FormData();
  form.append("file", file, file.name);
  if (meta?.title)       form.append("title", meta.title);
  if (meta?.description) form.append("description", meta.description);
  if (meta?.tags)        meta.tags.forEach(t => form.append("tags", t));
  return api.post<DocumentDto>("/api/Documents", form);
}

export async function deleteDocument(id: string) {
    return api.del<void>(`/api/Documents/${id}`);
}

export async function deleteDocumentsBulk(ids: string[]) {
    try {
        return await api.post<void>("/api/Documents/bulk-delete", ids, undefined);
    } catch {
        await Promise.all(ids.map(id => deleteDocument(id).catch(() => { })));
        return { deleted: ids.length } as any;
    }
}