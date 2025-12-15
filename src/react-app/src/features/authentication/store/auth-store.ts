import { Store } from "@tanstack/store";
import type { AuthState } from "@/features/authentication/types";

export const authStore = new Store<AuthState>({
	xsrfToken: null,
	isAuthenticated: false,
});
