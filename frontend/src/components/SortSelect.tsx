import { SortDirection } from "../types/api";

interface SortSelectProps {
  value: SortDirection;
  onChange: (value: SortDirection) => void;
}

export function SortSelect({ value, onChange }: SortSelectProps) {
  return (
    <label className="sort-select">
      <span className="sort-select__label">Sort</span>
      <select value={value} onChange={(e) => onChange(Number(e.target.value) as SortDirection)}>
        <option value={SortDirection.desc}>Newest First</option>
        <option value={SortDirection.asc}>Oldest First</option>
      </select>
    </label>
  );
}