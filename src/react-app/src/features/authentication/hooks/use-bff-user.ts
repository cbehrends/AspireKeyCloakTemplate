import { useEffect, useState } from "react";
import { authStore, fetchAndStoreXsrfToken, fetchBffUser } from "@/features";
import type { BffUser } from "../types";

export function useBffUser() {
	const [user, setUser] = useState<BffUser | null>(null);
	const [loading, setLoading] = useState(true);

	useEffect(() => {
		fetchBffUser()
			.then(async (data) => {
				// Normalize the response to handle both camelCase and PascalCase
				const normalizedData: BffUser = {
					isAuthenticated:
						data.isAuthenticated ?? data.IsAuthenticated ?? false,
					name: data.name ?? data.Name,
				};

				// Fetch XSRF token after successful authentication
				if (normalizedData.isAuthenticated) {
					await fetchAndStoreXsrfToken();
				}

				console.log("BFF User Data (raw):", data);
				console.log("BFF User Data (normalized):", normalizedData);

				setUser(normalizedData);
				authStore.setState((state) => ({
					...state,
					isAuthenticated: normalizedData.isAuthenticated,
				}));

				setLoading(false);
			})
			.catch((error) => {
				console.error("Error fetching BFF user:", error);
				setUser({ isAuthenticated: false });
				authStore.setState((state) => ({ ...state, isAuthenticated: false }));
				setLoading(false);
			});
	}, []);

	return { user, loading };
}
