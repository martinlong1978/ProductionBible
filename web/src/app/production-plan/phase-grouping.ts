import { AssetDto, PhaseDto } from '../core/models';

export interface PhaseGroup {
  phaseId: number | null;
  phase: string;
  assets: AssetDto[];
}

export function buildPhaseGroups(
  assets: AssetDto[],
  phases: PhaseDto[],
): { phaseGroups: PhaseGroup[]; unphasedGroup: PhaseGroup | null } {
  const byPhase = new Map<number, AssetDto[]>();
  const unphased: AssetDto[] = [];
  for (const asset of assets) {
    if (asset.phaseId === null) {
      unphased.push(asset);
      continue;
    }
    if (!byPhase.has(asset.phaseId)) byPhase.set(asset.phaseId, []);
    byPhase.get(asset.phaseId)!.push(asset);
  }

  const sortAssets = (list: AssetDto[]) =>
    [...list].sort((a, b) =>
      (a.orderInPhase ?? a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) -
      (b.orderInPhase ?? b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

  const phaseGroups = [...phases]
    .sort((a, b) => a.orderIndex - b.orderIndex)
    .map((phase) => ({
      phaseId: phase.id,
      phase: phase.name,
      assets: sortAssets(byPhase.get(phase.id) ?? []),
    }));

  const unphasedGroup = unphased.length > 0
    ? { phaseId: null, phase: 'Unphased', assets: sortAssets(unphased) }
    : null;

  return { phaseGroups, unphasedGroup };
}
