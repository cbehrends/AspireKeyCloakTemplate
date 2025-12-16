import { authStore } from "@/features";

export async function fetchAndStoreXsrfToken() {
	try {
		const res = await fetch("/bff/csrf", { credentials: "include" });
		if (res.ok) {
			const data = await res.json();
			authStore.setState((state) => ({ ...state, xsrfToken: data.token }));
			return data.token;
		}
	} catch (error) {
		console.error("Failed to fetch XSRF token:", error);
	}
	return null;
}
