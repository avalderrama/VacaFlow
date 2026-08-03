'use client';

// S-03 shell navigation (US-030). Real routes via next/link, not the
// prototype's single-page state — usePathname derives the active tab
// instead of duplicating it in component state.
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import type { EmployeeRole } from '@/lib/types';

export function NavTabs({ role }: { role: EmployeeRole }) {
  const pathname = usePathname();
  const isQueueActive = pathname.startsWith('/queue');

  return (
    <nav style={{ display: 'flex', gap: '6px' }}>
      <Link
        href="/requests"
        aria-current={!isQueueActive ? 'page' : undefined}
        className={`nav-tab ${!isQueueActive ? 'nav-tab-active' : ''}`}
      >
        My Requests
      </Link>
      {role === 'Manager' && (
        <Link
          href="/queue"
          aria-current={isQueueActive ? 'page' : undefined}
          className={`nav-tab ${isQueueActive ? 'nav-tab-active' : ''}`}
        >
          Approval Queue
        </Link>
      )}
    </nav>
  );
}
