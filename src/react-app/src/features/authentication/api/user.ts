import type { BffUser } from "../types";

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
