import type { BffUser } from "@/features/authentication/types";

export async function fetchBffUser(): Promise<BffUser> {
	try {
		const res = await fetch("/bff/user", { credentials: "include" });
		if (res.ok) {
			return await res.json();
		}
		return { isAuthenticated: false };
	} catch {
		return { isAuthenticated: false };
	}
}

export async function logout(redirectUrl?: string): Promise<void> {
	const url = new URL("/bff/logout", window.location.origin);
	if (redirectUrl) {
		url.searchParams.append("redirectUrl", redirectUrl);
	}

	try {
		const res = await fetch(url, {
			method: "POST",
			credentials: "include",
		});
		if (res.ok) {
			// Redirect to the logout redirect URL if provided
			const finalUrl = res.url || redirectUrl || "/";
			window.location.href = finalUrl;
		}
	} catch (error) {
		console.error("Logout failed:", error);
		// Fallback redirect
		window.location.href = "/";
	}
}

