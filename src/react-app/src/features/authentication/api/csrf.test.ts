import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { authStore } from "@/features";
import { fetchAndStoreXsrfToken } from "./csrf";

describe("fetchAndStoreXsrfToken", () => {
	let setStateMock: ReturnType<typeof vi.fn>;
	const originalSetState = authStore.setState;
	const originalFetch = global.fetch;

	beforeEach(() => {
		setStateMock = vi.fn();
		authStore.setState = setStateMock;
		global.fetch = vi.fn();
	});

	afterEach(() => {
		authStore.setState = originalSetState;
		global.fetch = originalFetch;
		vi.resetAllMocks();
	});

	it("should fetch the XSRF token and update the store on success", async () => {
		vi.mocked(global.fetch).mockResolvedValue({
			ok: true,
			json: async () => ({ token: "test-token" }),
		} as Response);
		const token = await fetchAndStoreXsrfToken();
		expect(token).toBe("test-token");
		expect(setStateMock).toHaveBeenCalledWith(expect.any(Function));
	});

	it("should return null and not update the store if fetch fails", async () => {
		vi.mocked(global.fetch).mockRejectedValue(new Error("fail"));
		const token = await fetchAndStoreXsrfToken();
		expect(token).toBeNull();
		expect(setStateMock).not.toHaveBeenCalled();
	});

	it("should return null and not update the store if response is not ok", async () => {
		vi.mocked(global.fetch).mockResolvedValue({ ok: false } as Response);
		const token = await fetchAndStoreXsrfToken();
		expect(token).toBeNull();
		expect(setStateMock).not.toHaveBeenCalled();
	});
});
