// Loading placeholder for a list fetch (Backlog.md §3.3, US-032 AC1).
// Presentational only — the page decides count/blockHeight per screen
// (3x64px on S-04, 2x96px on S-07); no fetch, no state, same pattern as
// RequestRow/QueueCard.
interface ListSkeletonProps {
  count: number;
  blockHeight: number;
}

export function ListSkeleton({ count, blockHeight }: ListSkeletonProps) {
  return (
    <div
      role="status"
      aria-label="Loading"
      style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}
    >
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="skeleton-block" style={{ height: `${blockHeight}px` }} />
      ))}
    </div>
  );
}
