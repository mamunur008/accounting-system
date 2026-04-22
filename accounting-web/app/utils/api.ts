export async function apiFetch<T>(path: string, opts: any = {}): Promise<T> {
  // Always ensure path starts with "/"
  const p = path.startsWith("/") ? path : `/${path}`

  // If you configured /api proxy in nuxt.config.ts, KEEP IT RELATIVE
  // so requests go to: http://localhost:3000/api/... and Nuxt proxies to backend.
  if (p.startsWith("/api/") || p === "/api") {
    return await $fetch<T>(p, opts)
  }

  // Optional: if later you want non-proxied calls, use runtimeConfig base
  const cfg = useRuntimeConfig()
  const base = (cfg.public?.apiBase as string | undefined) ?? "" // NEVER undefined

  const url =
    p.startsWith("http://") || p.startsWith("https://")
      ? p
      : `${base}${p}`

  return await $fetch<T>(url, opts)
}