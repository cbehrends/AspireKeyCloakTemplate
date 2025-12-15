import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { fetchAndStoreXsrfToken } from './csrf';
import { authStore } from '@/features';

describe('fetchAndStoreXsrfToken', () => {
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

  it('should fetch the XSRF token and update the store on success', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => ({ token: 'test-token' }),
    });
    const token = await fetchAndStoreXsrfToken();
    expect(token).toBe('test-token');
    expect(setStateMock).toHaveBeenCalledWith(expect.any(Function));
  });

  it('should return null and not update the store if fetch fails', async () => {
    (global.fetch as any).mockRejectedValue(new Error('fail'));
    const token = await fetchAndStoreXsrfToken();
    expect(token).toBeNull();
    expect(setStateMock).not.toHaveBeenCalled();
  });

  it('should return null and not update the store if response is not ok', async () => {
    (global.fetch as any).mockResolvedValue({ ok: false });
    const token = await fetchAndStoreXsrfToken();
    expect(token).toBeNull();
    expect(setStateMock).not.toHaveBeenCalled();
  });
});
