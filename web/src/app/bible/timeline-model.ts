import { AssetDto, BeatDto } from '../core/models';

export type TrackId = 'dialogue' | 'broll' | 'screen' | 'graphics';

export const TRACK_ORDER: TrackId[] = ['dialogue', 'broll', 'screen', 'graphics'];

export interface TimelineClip {
  asset: AssetDto;
  isReuse: boolean;
}

export interface TimelineRow {
  beat: BeatDto;
  startSeconds: number;
  durationSeconds: number;
  heightPx: number;
  isClamped: boolean;
  tracks: Record<TrackId, TimelineClip[]>;
}

export interface TimelineResult {
  rows: TimelineRow[];
  unscheduled: TimelineRow[];
  unassigned: AssetDto[];
}

const PX_PER_SECOND = 2;
const MIN_HEIGHT_PX = 56;
const MAX_HEIGHT_PX = 320;
const LAST_BEAT_FALLBACK_SECONDS = 60;

export function parseTimecodeSeconds(timecode: string): number | null {
  const match = /^(?:(\d+):)?(\d{1,2}):(\d{2})$/.exec(timecode);
  if (!match) return null;
  const hours = match[1] ? parseInt(match[1], 10) : 0;
  const minutes = parseInt(match[2], 10);
  const seconds = parseInt(match[3], 10);
  return hours * 3600 + minutes * 60 + seconds;
}

function assignTrack(asset: AssetDto): TrackId {
  if (asset.assetTypeName === 'PieceToCamera') return 'dialogue';
  if (asset.assetTypeName === 'Animation' || asset.assetTypeName === 'Title') return 'graphics';
  if (asset.assetTypeName === 'Shot') {
    const setupSection = asset.attributes['StoryboardSetupSection'] ?? '';
    return setupSection.startsWith('Setup D') ? 'screen' : 'broll';
  }
  return 'broll';
}

function emptyTracks(): Record<TrackId, TimelineClip[]> {
  return { dialogue: [], broll: [], screen: [], graphics: [] };
}

export function buildTimeline(beats: BeatDto[], assets: AssetDto[]): TimelineResult {
  const assetsById = new Map(assets.map((asset) => [asset.id, asset]));
  const seenAssetIds = new Set<number>();
  const linkedAssetIds = new Set(beats.flatMap((beat) => beat.assetIds));

  const scheduled: Array<{ beat: BeatDto; seconds: number }> = [];
  const unscheduledBeats: BeatDto[] = [];
  for (const beat of beats) {
    const seconds = parseTimecodeSeconds(beat.timecode);
    if (seconds === null) {
      unscheduledBeats.push(beat);
    } else {
      scheduled.push({ beat, seconds });
    }
  }
  scheduled.sort((a, b) => a.seconds - b.seconds);

  function buildRow(beat: BeatDto, startSeconds: number, durationSeconds: number): TimelineRow {
    const tracks = emptyTracks();
    for (const assetId of beat.assetIds) {
      const asset = assetsById.get(assetId);
      if (!asset) continue;
      const isReuse = seenAssetIds.has(asset.id);
      seenAssetIds.add(asset.id);
      tracks[assignTrack(asset)].push({ asset, isReuse });
    }

    const rawHeight = durationSeconds * PX_PER_SECOND;
    const heightPx = Math.min(MAX_HEIGHT_PX, Math.max(MIN_HEIGHT_PX, rawHeight));
    return {
      beat,
      startSeconds,
      durationSeconds,
      heightPx,
      isClamped: rawHeight < MIN_HEIGHT_PX || rawHeight > MAX_HEIGHT_PX,
      tracks,
    };
  }

  const rows: TimelineRow[] = scheduled.map((entry, index) => {
    const next = scheduled[index + 1];
    const duration = next ? next.seconds - entry.seconds : LAST_BEAT_FALLBACK_SECONDS;
    return buildRow(entry.beat, entry.seconds, duration);
  });

  const unscheduled: TimelineRow[] = unscheduledBeats.map((beat) => buildRow(beat, 0, 0));

  const unassigned = assets.filter((asset) => !linkedAssetIds.has(asset.id));

  return { rows, unscheduled, unassigned };
}
