// fetchWithXsrf.ts
// Wrapper for fetch that always includes credentials and XSRF token header if present

let xsrfToken: string | undefined;

async function ensureXsrfToken() {
  if (!xsrfToken) {
    const res = await fetch('/bff/csrf', { credentials: 'include' });
    if (res.ok) {
      const data = await res.json();
      xsrfToken = data.token;
    }
  }
  return xsrfToken;
}

export async function fetchWithXsrf(input: RequestInfo | URL, init: RequestInit = {}) {
  await ensureXsrfToken();
  const headers = new Headers(init.headers || {});
  if (xsrfToken) {
    headers.set('X-XSRF-TOKEN', xsrfToken);
  }
  return fetch(input, {
    ...init,
    credentials: 'include',
    headers,
  });
}
