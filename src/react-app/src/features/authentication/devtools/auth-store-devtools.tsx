import { EventClient } from "@tanstack/devtools-event-client";
import { useEffect, useState } from "react";

import { authStore } from "@/features";
import type { AuthState } from "../types";

type EventMap = {
	"auth-store-devtools:state": AuthState;
};

class AuthStoreDevtoolsEventClient extends EventClient<EventMap> {
	constructor() {
		super({
			pluginId: "auth-store-devtools",
		});
	}
}

const asdec = new AuthStoreDevtoolsEventClient();

authStore.subscribe(() => {
	asdec.emit("state", authStore.state);
});

function DevtoolPanel() {
	const [state, setState] = useState<EventMap["auth-store-devtools:state"]>(
		() => authStore.state,
	);

	useEffect(() => {
		return asdec.on("state", (e) => setState(e.payload));
	}, []);

	return (
		<div className="p-4 grid gap-4 grid-cols-[1fr_10fr]">
			<div className="text-sm font-bold text-gray-500 whitespace-nowrap">
				Is Authenticated
			</div>
			<div className="text-sm">
				{state?.isAuthenticated ? "✅ Yes" : "❌ No"}
			</div>
			<div className="text-sm font-bold text-gray-500 whitespace-nowrap">
				XSRF Token
			</div>
			<div className="text-sm font-mono break-all">
				{state?.xsrfToken || "(none)"}
			</div>
		</div>
	);
}

export default {
	name: "Auth Store",
	render: <DevtoolPanel />,
};
