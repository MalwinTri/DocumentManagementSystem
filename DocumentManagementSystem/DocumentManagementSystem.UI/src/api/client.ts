export const BASE_URL =
    (typeof import.meta !== "undefined" && import.meta.env?.VITE_API_BASE_URL) ||
    "http://localhost:7244";

type RequestOpts = {
    headers?: Record<string, string>;
};

async function request<T>(
    method: string,
    url: string,
    body?: any,                 // <-- body wirklich optional
    opts: RequestOpts = {}      // <-- Default für opts
): Promise<T> {
    const headers: Record<string, string> = { ...(opts.headers || {}) };
    let fetchBody: BodyInit | undefined = body;

    if (!(body instanceof FormData) && body !== undefined) {
        headers["Content-Type"] = "application/json";
        fetchBody = JSON.stringify(body);
    }

    const res = await fetch(`${BASE_URL}${url}`, {
        method,
        headers,
        body: fetchBody,
        credentials: "include",
    });

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`${res.status} ${res.statusText}: ${text}`);
    }

    const ct = res.headers.get("content-type") || "";
    return ct.includes("application/json") ? (res.json() as Promise<T>) : (null as T);
}

export const api = {
    get<T>(url: string, opts?: RequestOpts) {
        return request<T>("GET", url, undefined, opts);     // <-- body = undefined
    },
    post<T>(url: string, body?: any, opts?: RequestOpts) {
        return request<T>("POST", url, body, opts);
    },
    put<T>(url: string, body?: any, opts?: RequestOpts) {
        return request<T>("PUT", url, body, opts);
    },
    del<T>(url: string, opts?: RequestOpts) {
        return request<T>("DELETE", url, undefined, opts);  // <-- body = undefined
    },
};
