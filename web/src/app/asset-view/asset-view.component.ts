import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, map } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ASSET_STATUSES, statusPillClass } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, PhaseDto } from '../core/models';
import { AssetEditorComponent } from '../manage/asset-editor.component';
import { AssetViewFilters, filterAndSortAssets, SortDir, SortKey } from './asset-view.logic';

@Component({
  selector: 'app-asset-view',
  standalone: true,
  imports: [CommonModule, FormsModule, AssetEditorComponent],
  templateUrl: './asset-view.component.html',
})
export class AssetViewComponent {
  assets: AssetDto[] = [];
  assetTypes: AssetTypeDto[] = [];
  phases: PhaseDto[] = [];
  assetTypesById = new Map<number, AssetTypeDto>();
  beatsById = new Map<number, BeatDto>();
  beatsByEpisode = new Map<number, BeatDto[]>();

  sortKey: SortKey = 'shooting';
  sortDir: SortDir = 'asc';
  filters: AssetViewFilters = { status: null, assetTypeId: null, toShootOnly: false };

  selectedAssetForEdit: AssetDto | null = null;

  protected readonly assetStatuses = ASSET_STATUSES;
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    this.api.getAssetTypes().subscribe((types) => {
      this.assetTypes = types;
      this.assetTypesById = new Map(types.map((t) => [t.id, t]));
      this.cdr.markForCheck();
    });

    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadData(projectId);
      }
    });
  }

  get visibleAssets(): AssetDto[] {
    return filterAndSortAssets(this.assets, this.assetTypesById, this.beatsById, this.filters, this.sortKey, this.sortDir);
  }

  setSortKey(key: SortKey): void {
    this.sortDir = this.sortKey === key && this.sortDir === 'asc' ? 'desc' : 'asc';
    this.sortKey = key;
  }

  openEditor(asset: AssetDto): void {
    this.selectedAssetForEdit = asset;
  }

  closeEditor(): void {
    this.selectedAssetForEdit = null;
  }

  timelineOrderDisplay(asset: AssetDto): string {
    const ordinals = asset.beatIds.map((id) => this.beatsById.get(id)?.ordinal).filter((o): o is number => o !== undefined);
    return ordinals.length > 0 ? String(Math.min(...ordinals)) : '—';
  }

  onEditorSaved(updated: AssetDto): void {
    const index = this.assets.findIndex((a) => a.id === updated.id);
    if (index !== -1) {
      this.assets[index] = updated;
    }
    this.selectedAssetForEdit = null;
    this.cdr.markForCheck();
  }

  private loadData(projectId: number): void {
    forkJoin({
      episodes: this.api.getEpisodes(projectId),
      phases: this.api.getPhases(projectId),
    }).subscribe(({ episodes, phases }) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      this.phases = phases;
      if (episodes.length === 0) {
        this.assets = [];
        this.beatsById = new Map();
        this.beatsByEpisode = new Map();
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) =>
        forkJoin({ assets: this.api.getAssets(episode.id), beats: this.api.getBeats(episode.id) })
          .pipe(map((result) => ({ episodeId: episode.id, ...result }))),
      )).subscribe((perEpisode) => {
        if (this.projectContext.selectedProjectId() !== projectId) return;
        this.assets = perEpisode.flatMap((e) => e.assets);
        this.beatsByEpisode = new Map(perEpisode.map((e) => [e.episodeId, e.beats]));
        this.beatsById = new Map(perEpisode.flatMap((e) => e.beats).map((b) => [b.id, b]));
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
