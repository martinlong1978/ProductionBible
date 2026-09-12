import { AssetDto, AssetTypeDto, BeatDto } from '../core/models';

export type SortKey = 'type' | 'name' | 'length' | 'timeline' | 'shooting';
export type SortDir = 'asc' | 'desc';

export interface AssetViewFilters {
  status: string | null;
  assetTypeId: number | null;
  toShootOnly: boolean;
}

export function timelineOrderOf(asset: AssetDto, beatsById: Map<number, BeatDto>): number {
  const ordinals = asset.beatIds
    .map((id) => beatsById.get(id)?.ordinal)
    .filter((o): o is number => o !== undefined);
  return ordinals.length > 0 ? Math.min(...ordinals) : Number.MAX_SAFE_INTEGER;
}

function sortKeyValue(
  asset: AssetDto,
  assetTypesById: Map<number, AssetTypeDto>,
  beatsById: Map<number, BeatDto>,
  key: SortKey,
): string | number {
  switch (key) {
    case 'type':
      return assetTypesById.get(asset.assetTypeId)?.name ?? '';
    case 'name':
      return asset.title;
    case 'length':
      return asset.targetLengthSeconds ?? Number.MAX_SAFE_INTEGER;
    case 'timeline':
      return timelineOrderOf(asset, beatsById);
    case 'shooting':
      return asset.sequenceNumber ?? Number.MAX_SAFE_INTEGER;
  }
}

export function filterAndSortAssets(
  assets: AssetDto[],
  assetTypesById: Map<number, AssetTypeDto>,
  beatsById: Map<number, BeatDto>,
  filters: AssetViewFilters,
  sortKey: SortKey,
  sortDir: SortDir,
): AssetDto[] {
  const filtered = assets.filter((asset) => {
    if (filters.status !== null && asset.status !== filters.status) return false;
    if (filters.assetTypeId !== null && asset.assetTypeId !== filters.assetTypeId) return false;
    if (filters.toShootOnly && asset.completedAtUtc !== null) return false;
    return true;
  });

  const sorted = [...filtered].sort((a, b) => {
    const av = sortKeyValue(a, assetTypesById, beatsById, sortKey);
    const bv = sortKeyValue(b, assetTypesById, beatsById, sortKey);
    if (av < bv) return -1;
    if (av > bv) return 1;
    return 0;
  });

  return sortDir === 'asc' ? sorted : sorted.reverse();
}
