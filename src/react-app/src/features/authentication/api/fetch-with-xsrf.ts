// Wrapper for fetch that includes credentials and XSRF token from auth store

import { authStore } from "@/features";
import { fetchAndStoreXsrfToken } from "./csrf";

export async function fetchWithXsrf(
	input: RequestInfo | URL,
	init: RequestInit = {},
) {
	let token = authStore.state.xsrfToken;

	// If no token in store, try to fetch it
	if (!token) {
		token = await fetchAndStoreXsrfToken();
	}

	const headers = new Headers(init.headers || {});
	if (token) {
		headers.set("X-XSRF-TOKEN", token);
	}

	return fetch(input, {
		...init,
		credentials: "include",
		headers,
	});
}
