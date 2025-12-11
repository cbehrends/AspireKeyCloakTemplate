export interface AuthState {
	xsrfToken: string | null;
	isAuthenticated: boolean;
}

export interface BffUser {
	isAuthenticated: boolean;
	name?: string;
	// Backend might return PascalCase
	IsAuthenticated?: boolean;
	Name?: string;
}
