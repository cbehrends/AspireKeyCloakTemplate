import { createFileRoute } from "@tanstack/react-router";
export const Route = createFileRoute("/")({ component: App });

function App() {
	return <div>Woot woot! This is the root route!</div>;
}
