import { useState, useEffect, useCallback } from 'react';

interface ExampleRow {
  id: number | string;
  [key: string]: unknown;
}

export default function ExampleGrid() {
  const [rows, setRows] = useState<ExampleRow[]>([]);
  const [columns, setColumns] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch('/api/example');
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}: ${res.statusText}`);
      }
      const data = await res.json();
      const items: ExampleRow[] = Array.isArray(data) ? data : [data];
      setRows(items);
      if (items.length > 0) {
        setColumns(Object.keys(items[0]));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data');
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    const handleEnvChanged = () => fetchData();
    window.addEventListener('env-changed', handleEnvChanged);
    return () => window.removeEventListener('env-changed', handleEnvChanged);
  }, [fetchData]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16 text-gray-500">
        <svg className="animate-spin h-5 w-5 mr-2" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
        </svg>
        Loading…
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-md bg-red-50 border border-red-200 p-4 text-red-700 text-sm">
        <strong>Error:</strong> {error}
        <button
          onClick={fetchData}
          className="ml-4 underline hover:no-underline"
        >
          Retry
        </button>
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <div className="text-center py-16 text-gray-400 text-sm">No data returned.</div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200 shadow-sm">
      {/*
       * TODO: Replace with commercial data grid (e.g. AG Grid, Telerik KendoReact).
       *
       * For AG Grid:    npm install ag-grid-react ag-grid-community
       * For KendoReact: npm install @progress/kendo-react-grid @progress/kendo-data-query
       *
       * Pass `rows` as rowData / data and `columns` to derive column definitions.
       */}
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50">
          <tr>
            {columns.map((col) => (
              <th
                key={col}
                className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider"
              >
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-100">
          {rows.map((row, i) => (
            <tr key={row.id ?? i} className="hover:bg-gray-50 transition-colors">
              {columns.map((col) => (
                <td key={col} className="px-4 py-3 text-gray-700 whitespace-nowrap">
                  {String(row[col] ?? '')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
