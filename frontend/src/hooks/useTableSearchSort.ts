import { useMemo, useState } from 'react';

export type SortDirection = 'asc' | 'desc';

interface UseTableSearchSortOptions<T> {
  searchText: (item: T) => string;
  sorters: Record<string, (a: T, b: T) => number>;
  defaultSortKey?: string;
  defaultDirection?: SortDirection;
}

export function useTableSearchSort<T>(items: T[], options: UseTableSearchSortOptions<T>) {
  const [query, setQuery] = useState('');
  const [sortKey, setSortKey] = useState(options.defaultSortKey ?? '');
  const [direction, setDirection] = useState<SortDirection>(options.defaultDirection ?? 'asc');

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => options.searchText(item).toLowerCase().includes(q));
  }, [items, query, options]);

  const rows = useMemo(() => {
    const sorter = options.sorters[sortKey];
    if (!sorter) return filtered;
    const copy = [...filtered].sort(sorter);
    return direction === 'asc' ? copy : copy.reverse();
  }, [filtered, sortKey, direction, options.sorters]);

  const toggleSort = (key: string) => {
    if (sortKey === key) {
      setDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setDirection('asc');
    }
  };

  return { query, setQuery, sortKey, direction, toggleSort, rows };
}
