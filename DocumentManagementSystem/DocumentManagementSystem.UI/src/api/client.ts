 export const API_BASE = import.meta.env.VITE_API_BASE_URL?.replace(/\/+$/, "") ?? "";

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { ...(init?.body instanceof FormData ? {} : { "Content-Type": "application/json" }) },
    ...init,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`${res.status} ${res.statusText} - ${text}`);
  }
  return res.json() as Promise<T>;
}

export const api = {
  get  : <T>(p: string) => http<T>(p),
  post : <T>(p: string, body: any, init?: RequestInit) =>
    http<T>(p, { method: "POST", body: body instanceof FormData ? body : JSON.stringify(body), ...init }),
};
