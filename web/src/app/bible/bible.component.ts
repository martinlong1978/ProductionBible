import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, BeatDto, EpisodeDto, UpdateAssetRequest } from '../core/models';
import { ASSET_STATUSES, statusPillClass } from '../core/status-style';
import { TimelineViewComponent } from './timeline-view.component';

@Component({
  selector: 'app-bible',
  standalone: true,
  imports: [CommonModule, FormsModule, TimelineViewComponent],
  templateUrl: './bible.component.html',
})
export class BibleComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];
  assets: AssetDto[] = [];
  viewMode: 'list' | 'timeline' = 'list';
  protected readonly statusPillClass = statusPillClass;
  protected readonly assetStatuses = ASSET_STATUSES;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadEpisodes(projectId);
      }
    });
  }

  setViewMode(mode: 'list' | 'timeline'): void {
    this.viewMode = mode;
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.api.getBeats(episodeId).subscribe((beats) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.beats = beats;
      this.cdr.markForCheck();
    });
    this.api.getAssets(episodeId).subscribe((assets) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.assets = assets;
      this.cdr.markForCheck();
    });
  }

  assetsForBeat(beat: BeatDto): AssetDto[] {
    const byId = new Map(this.assets.map((asset) => [asset.id, asset]));
    return beat.assetIds
      .map((id) => byId.get(id))
      .filter((asset): asset is AssetDto => asset !== undefined);
  }

  get unassignedAssets(): AssetDto[] {
    const linkedIds = new Set(this.beats.flatMap((beat) => beat.assetIds));
    return this.assets.filter((asset) => !linkedIds.has(asset.id));
  }

  editingAssetId: number | null = null;
  editStatus = '';
  editNotes = '';

  startEdit(asset: AssetDto): void {
    this.editingAssetId = asset.id;
    this.editStatus = asset.status;
    this.editNotes = asset.notes ?? '';
  }

  cancelEdit(): void {
    this.editingAssetId = null;
  }

  saveEdit(asset: AssetDto): void {
    const request: UpdateAssetRequest = {
      assetTypeId: asset.assetTypeId,
      code: asset.code,
      title: asset.title,
      scriptText: asset.scriptText,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: asset.sequenceNumber,
      targetLengthSeconds: asset.targetLengthSeconds,
      phaseId: asset.phaseId,
      attributes: asset.attributes,
      beatIds: asset.beatIds,
    };
    this.api.updateAsset(asset.id, request).subscribe((updated) => {
      const index = this.assets.findIndex((a) => a.id === asset.id);
      if (index !== -1) {
        this.assets[index] = updated;
      }
      this.editingAssetId = null;
      this.cdr.markForCheck();
    });
  }

  private loadEpisodes(projectId: number): void {
    this.editingAssetId = null;
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      this.episodes = episodes;
      if (episodes.length > 0) {
        this.selectEpisode(episodes[0].id);
      } else {
        this.selectedEpisodeId = null;
        this.beats = [];
        this.assets = [];
      }
      this.cdr.markForCheck();
    });
  }
}
