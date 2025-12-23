import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";

let queryClientSingleton: QueryClient | undefined;

export function getContext() {
	queryClientSingleton ??= new QueryClient();
	return {
		queryClient: queryClientSingleton,
	};
}

interface ProviderProps {
	readonly children: ReactNode;
	readonly queryClient: QueryClient;
}

export function Provider({ children, queryClient }: Readonly<ProviderProps>) {
	return (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
}
