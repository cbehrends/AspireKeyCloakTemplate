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
	const url = new URL("/bff/logout", globalThis.location.origin);
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
			globalThis.location.href = res.url || redirectUrl || "/";
		}
	} catch (error) {
		console.error("Logout failed:", error);
		// Fallback redirect
		globalThis.location.href = "/";
	}
}

export async function login(
	returnUrl?: string,
	claimsChallenge?: string,
): Promise<void> {
	const url = new URL("/bff/login", globalThis.location.origin);
	if (returnUrl) {
		url.searchParams.append("returnUrl", returnUrl);
	}
	if (claimsChallenge) {
		url.searchParams.append("claimsChallenge", claimsChallenge);
	}

	try {
		const res = await fetch(url, {
			method: "POST",
			credentials: "include",
		});
		if (res.ok) {
			// Redirect to the login redirect URL if provided
			globalThis.location.href = res.url || returnUrl || "/";
		}
	} catch (error) {
		console.error("Login failed:", error);
		// Fallback redirect
		globalThis.location.href = "/";
	}
}
