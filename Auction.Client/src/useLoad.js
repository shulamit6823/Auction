import { useCallback, useEffect, useState } from 'react';

// Loads data from the server when the page opens (and when deps change).
// Returns the data, the loading state, the error message, and reload() to load again.
export function useLoad(loader, deps) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // A new reload function only when deps change (e.g. another auction id) - that triggers a new load.
  const reload = useCallback(async () => {
    setError(null);
    try {
      setData(await loader());
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, deps);

  useEffect(() => {
    setLoading(true);
    reload();
  }, [reload]);

  return { data, loading, error, reload };
}
