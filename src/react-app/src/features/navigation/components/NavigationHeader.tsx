import { Link } from "@tanstack/react-router";
import { Home, Menu, X } from "lucide-react";
import { useState } from "react";
import { useBffUser } from "@/features";
import { logout } from "@/features/authentication/api/user";

export default function NavigationHeader() {
	const [isOpen, setIsOpen] = useState(false);
	const { user, loading } = useBffUser();

	console.log("NavigationHeader - loading:", loading, "user:", user);

	const handleLogout = (e: React.MouseEvent<HTMLButtonElement>) => {
		e.preventDefault();
		logout();
	};

	return (
		<>
			<header className="p-4 flex items-center justify-between bg-gray-800 text-white shadow-lg">
				<div className="flex items-center">
					<button
						type="button"
						onClick={() => setIsOpen(true)}
						className="p-2 hover:bg-gray-700 rounded-lg transition-colors"
						aria-label="Open menu"
					>
						<Menu size={24} />
					</button>
					<h1 className="ml-4 text-xl font-semibold">
						<Link to="/">
							<img
								src="/tanstack-word-logo-white.svg"
								alt="TanStack Logo"
								className="h-10"
							/>
						</Link>
					</h1>
				</div>
				<div>
					{loading ? (
						<span className="text-gray-400">Loading...</span>
					) : user?.isAuthenticated ? (
						<>
							<span className="mr-4">Hello, {user.name || "User"}</span>
							<button
								onClick={handleLogout}
								className="px-3 py-1 bg-red-600 rounded hover:bg-red-700 inline-block cursor-pointer"
							>
								Logout
							</button>
						</>
					) : (
						<a
							href="/bff/login"
							className="px-3 py-1 bg-green-600 rounded hover:bg-green-700 inline-block"
						>
							Login
						</a>
					)}
				</div>
			</header>

			<aside
				className={`fixed top-0 left-0 h-full w-80 bg-gray-900 text-white shadow-2xl z-50 transform transition-transform duration-300 ease-in-out flex flex-col ${
					isOpen ? "translate-x-0" : "-translate-x-full"
				}`}
			>
				<div className="flex items-center justify-between p-4 border-b border-gray-700">
					<h2 className="text-xl font-bold">Navigation</h2>
					<button
						type="button"
						onClick={() => setIsOpen(false)}
						className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
						aria-label="Close menu"
					>
						<X size={24} />
					</button>
				</div>

				<nav className="flex-1 p-4 overflow-y-auto">
					<Link
						to="/"
						onClick={() => setIsOpen(false)}
						className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-800 transition-colors mb-2"
						activeProps={{
							className:
								"flex items-center gap-3 p-3 rounded-lg bg-cyan-600 hover:bg-cyan-700 transition-colors mb-2",
						}}
					>
						<Home size={20} />
						<span className="font-medium">Home</span>
					</Link>
				</nav>
			</aside>
		</>
	);
}
