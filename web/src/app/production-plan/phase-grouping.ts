import { AssetDto } from '../core/models';

export interface PhaseGroup {
  phase: string;
  assets: AssetDto[];
}

export function buildPhaseGroups(assets: AssetDto[]): PhaseGroup[] {
  const sorted = [...assets].sort(
    (a, b) => (a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) - (b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

  const map = new Map<string, AssetDto[]>();
  for (const asset of sorted) {
    const phase = asset.attributes['PhaseGroup'] ?? 'Unphased';
    if (!map.has(phase)) map.set(phase, []);
    map.get(phase)!.push(asset);
  }

  return Array.from(map.entries()).map(([phase, assets]) => ({ phase, assets }));
}
