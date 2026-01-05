import { api } from "@/api/client";

export async function suggestTags(q: string, take = 20): Promise<string[]> {
    const params = new URLSearchParams();
    if (q != null) params.set("q", String(q));
    params.set("take", String(take));
    return api.get(`/api/Tags/suggest?${params.toString()}`);
}
