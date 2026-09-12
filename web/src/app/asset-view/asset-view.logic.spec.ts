import { AssetDto, AssetTypeDto, BeatDto } from '../core/models';
import { filterAndSortAssets } from './asset-view.logic';

function asset(overrides: Partial<AssetDto>): AssetDto {
  return {
    id: 1, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01', title: 'Alpha',
    scriptText: null, status: 'Planned', notes: null, sequenceNumber: null, targetLengthSeconds: null,
    phaseId: null, completedAtUtc: null, attributes: {}, beatIds: [], orderInPhase: null,
    ...overrides,
  };
}

const shotType: AssetTypeDto = { id: 1, name: 'Shot' };
const graphicType: AssetTypeDto = { id: 2, name: 'Graphic' };
const assetTypesById = new Map([[1, shotType], [2, graphicType]]);

describe('filterAndSortAssets', () => {
  it('sorts by type name', () => {
    const a = asset({ id: 1, assetTypeId: 2 });
    const b = asset({ id: 2, assetTypeId: 1 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'type', 'asc');
    expect(result.map((x) => x.id)).toEqual([1, 2]); // 'Graphic' < 'Shot', so Graphic (a) sorts first ascending
  });

  it('sorts by name (title)', () => {
    const a = asset({ id: 1, title: 'Zebra' });
    const b = asset({ id: 2, title: 'Apple' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('sorts by length, nulls last ascending', () => {
    const a = asset({ id: 1, targetLengthSeconds: null });
    const b = asset({ id: 2, targetLengthSeconds: 5 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'length', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('sorts by timeline order using the minimum linked beat ordinal, unlinked last', () => {
    const beatsById = new Map<number, BeatDto>([
      [100, { id: 100, episodeId: 10, timecode: '00:00', purpose: 'a', ordinal: 5, durationSeconds: 1, startSeconds: 0, endSeconds: 1, assetIds: [] }],
      [101, { id: 101, episodeId: 10, timecode: '00:01', purpose: 'b', ordinal: 1, durationSeconds: 1, startSeconds: 1, endSeconds: 2, assetIds: [] }],
    ]);
    const a = asset({ id: 1, beatIds: [100] }); // ordinal 5
    const b = asset({ id: 2, beatIds: [101] }); // ordinal 1
    const c = asset({ id: 3, beatIds: [] });     // unlinked
    const result = filterAndSortAssets([a, b, c], assetTypesById, beatsById, { status: null, assetTypeId: null, toShootOnly: false }, 'timeline', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1, 3]);
  });

  it('sorts by shooting order using sequenceNumber, nulls last ascending', () => {
    const a = asset({ id: 1, sequenceNumber: null });
    const b = asset({ id: 2, sequenceNumber: 3 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'shooting', 'asc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('reverses order when sortDir is desc', () => {
    const a = asset({ id: 1, title: 'Apple' });
    const b = asset({ id: 2, title: 'Zebra' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: false }, 'name', 'desc');
    expect(result.map((x) => x.id)).toEqual([2, 1]);
  });

  it('filters by status', () => {
    const a = asset({ id: 1, status: 'Planned' });
    const b = asset({ id: 2, status: 'Shot' });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: 'Shot', assetTypeId: null, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });

  it('filters by assetTypeId', () => {
    const a = asset({ id: 1, assetTypeId: 1 });
    const b = asset({ id: 2, assetTypeId: 2 });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: 2, toShootOnly: false }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });

  it('filters to only assets with no completedAtUtc when toShootOnly is true', () => {
    const a = asset({ id: 1, completedAtUtc: '2026-09-01T00:00:00Z' });
    const b = asset({ id: 2, completedAtUtc: null });
    const result = filterAndSortAssets([a, b], assetTypesById, new Map(), { status: null, assetTypeId: null, toShootOnly: true }, 'name', 'asc');
    expect(result.map((x) => x.id)).toEqual([2]);
  });
});
