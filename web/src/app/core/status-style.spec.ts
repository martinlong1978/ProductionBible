import { statusPillClass } from './status-style';

describe('statusPillClass', () => {
  it('maps a done-style status to the done pill class', () => {
    expect(statusPillClass('Shot')).toBe('bg-status-done');
    expect(statusPillClass('Done')).toBe('bg-status-done');
  });

  it('maps an in-progress status to the progress pill class', () => {
    expect(statusPillClass('In Progress')).toBe('bg-status-progress');
  });

  it('falls back to the planned pill class for anything else', () => {
    expect(statusPillClass('Planned')).toBe('bg-status-planned');
    expect(statusPillClass('Something unexpected')).toBe('bg-status-planned');
  });
});
