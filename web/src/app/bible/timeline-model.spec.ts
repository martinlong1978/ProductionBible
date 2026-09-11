import { AssetDto, BeatDto } from '../core/models';
import { buildTimeline, parseTimecodeSeconds } from './timeline-model';

describe('parseTimecodeSeconds', () => {
  it('parses zero-padded MM:SS', () => {
    expect(parseTimecodeSeconds('04:00')).toBe(240);
  });

  it('parses unpadded M:SS', () => {
    expect(parseTimecodeSeconds('4:00')).toBe(240);
  });

  it('parses HH:MM:SS', () => {
    expect(parseTimecodeSeconds('01:04:00')).toBe(3840);
  });

  it('returns null for raw prose', () => {
    expect(parseTimecodeSeconds('Reused in EP1 20:30 · EP3 12:00')).toBeNull();
  });
});

describe('buildTimeline', () => {
  function asset(id: number, assetTypeName: string, code: string, attrs: Record<string, string> = {}): AssetDto {
    return {
      id, episodeId: 1, assetTypeId: 1, assetTypeName, code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: null,
      targetLengthSeconds: null, phaseId: null, completedAtUtc: null, attributes: attrs, beatIds: [],
    };
  }

  function beat(id: number, timecode: string, assetIds: number[]): BeatDto {
    return { id, episodeId: 1, timecode, purpose: 'Beat', ordinal: 0, durationSeconds: 0, startSeconds: 0, endSeconds: 0, assetIds };
  }

  it('builds one row per beat in ascending timecode order', () => {
    const beats = [beat(2, '04:00', []), beat(1, '00:00', [])];
    const result = buildTimeline(beats, []);

    expect(result.rows.map((r) => r.beat.timecode)).toEqual(['00:00', '04:00']);
  });

  it('computes duration as the gap to the next beat, and 60s for the last beat', () => {
    const beats = [beat(1, '00:00', []), beat(2, '00:38', []), beat(3, '00:50', [])];
    const result = buildTimeline(beats, []);

    expect(result.rows[0].durationSeconds).toBe(38);
    expect(result.rows[1].durationSeconds).toBe(12);
    expect(result.rows[2].durationSeconds).toBe(60);
  });

  it('clamps row height and flags clamped rows', () => {
    const beats = [beat(1, '00:00', []), beat(2, '00:05', []), beat(3, '10:00', [])];
    const result = buildTimeline(beats, []);

    expect(result.rows[0].isClamped).toBeTrue();
    expect(result.rows[0].heightPx).toBeGreaterThanOrEqual(56);
    expect(result.rows[1].isClamped).toBeTrue();
    expect(result.rows[1].heightPx).toBeLessThanOrEqual(320);
  });

  it('assigns PieceToCamera to the dialogue track', () => {
    const a = asset(1, 'PieceToCamera', 'E-L');
    const beats = [beat(1, '00:00', [1])];
    const result = buildTimeline(beats, [a]);

    expect(result.rows[0].tracks['dialogue'].map((c) => c.asset.id)).toEqual([1]);
  });

  it('assigns a Setup D shot to the screen track, not b-roll', () => {
    const a = asset(1, 'Shot', 'D-01', { StoryboardSetupSection: 'Setup D — Screen capture' });
    const beats = [beat(1, '00:00', [1])];
    const result = buildTimeline(beats, [a]);

    expect(result.rows[0].tracks['screen'].map((c) => c.asset.id)).toEqual([1]);
    expect(result.rows[0].tracks['broll']).toEqual([]);
  });

  it('assigns a non-Setup-D shot to the b-roll track', () => {
    const a = asset(1, 'Shot', 'B-01', { StoryboardSetupSection: 'Setup B — Bench' });
    const beats = [beat(1, '00:00', [1])];
    const result = buildTimeline(beats, [a]);

    expect(result.rows[0].tracks['broll'].map((c) => c.asset.id)).toEqual([1]);
  });

  it('assigns Animation and Title to the graphics track', () => {
    const anim = asset(1, 'Animation', 'g1_gears');
    const title = asset(2, 'Title', 't1_title');
    const beats = [beat(1, '00:00', [1, 2])];
    const result = buildTimeline(beats, [anim, title]);

    expect(result.rows[0].tracks['graphics'].map((c) => c.asset.id)).toEqual([1, 2]);
  });

  it('preserves beat.assetIds order within a track', () => {
    const a = asset(1, 'Shot', 'B-01');
    const b = asset(2, 'Shot', 'B-02');
    const beats = [beat(1, '00:00', [2, 1])];
    const result = buildTimeline(beats, [a, b]);

    expect(result.rows[0].tracks['broll'].map((c) => c.asset.id)).toEqual([2, 1]);
  });

  it('marks the second and later occurrences of a multi-beat asset as reuses', () => {
    const a = asset(1, 'PieceToCamera', 'E-S');
    const beats = [beat(1, '00:00', [1]), beat(2, '05:00', [1])];
    const result = buildTimeline(beats, [a]);

    expect(result.rows[0].tracks['dialogue'][0].isReuse).toBeFalse();
    expect(result.rows[1].tracks['dialogue'][0].isReuse).toBeTrue();
  });

  it('routes unparseable-timecode beats to the unscheduled bucket, not the main rows', () => {
    const raw = 'Reused in EP1 20:30 · EP3 12:00';
    const beats = [beat(1, '00:00', []), beat(2, raw, [])];
    const result = buildTimeline(beats, []);

    expect(result.rows.map((r) => r.beat.timecode)).toEqual(['00:00']);
    expect(result.unscheduled.map((r) => r.beat.timecode)).toEqual([raw]);
  });

  it('collects assets with no beat link into unassigned', () => {
    const linked = asset(1, 'Shot', 'B-01');
    const unlinked = asset(2, 'Animation', 'g1_gears');
    const beats = [beat(1, '00:00', [1])];
    const result = buildTimeline(beats, [linked, unlinked]);

    expect(result.unassigned.map((a) => a.id)).toEqual([2]);
  });
});
