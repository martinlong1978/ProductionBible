const DONE_STATUSES = new Set(['Shot', 'Done', 'Complete', 'Completed']);
const PROGRESS_STATUSES = new Set(['In Progress', 'Editing', 'Reviewing']);

export function statusPillClass(status: string): string {
  if (DONE_STATUSES.has(status)) return 'bg-status-done';
  if (PROGRESS_STATUSES.has(status)) return 'bg-status-progress';
  return 'bg-status-planned';
}
