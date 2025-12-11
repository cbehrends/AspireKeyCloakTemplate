import { Store } from "@tanstack/store";
import type { AuthState } from "../types";

export const authStore = new Store<AuthState>({
	xsrfToken: null,
	isAuthenticated: false,
});
