import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { statusPillClass } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto } from '../core/models';
import { AssetEditorComponent } from './asset-editor.component';

@Component({
  selector: 'app-assets-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, AssetEditorComponent],
  templateUrl: './assets-tab.component.html',
})
export class AssetsTabComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  assets: AssetDto[] = [];
  assetTypes: AssetTypeDto[] = [];
  phases: PhaseDto[] = [];
  beats: BeatDto[] = [];
  protected readonly statusPillClass = statusPillClass;

  newCode = '';
  newTitle = '';
  newAssetTypeId: number | null = null;

  editingId: number | null = null;

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    this.api.getAssetTypes().subscribe((types) => {
      this.assetTypes = types;
      this.cdr.markForCheck();
    });

    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      this.editingId = null;
      this.confirmingDeleteId = null;
      if (projectId === null) {
        this.episodes = [];
        this.selectedEpisodeId = null;
        this.assets = [];
        this.phases = [];
        return;
      }
      this.api.getPhases(projectId).subscribe((phases) => {
        if (this.projectId !== projectId) return;
        this.phases = phases;
        this.cdr.markForCheck();
      });
      this.loadEpisodes(projectId);
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (this.projectId !== projectId) return;
      this.episodes = episodes;
      if (episodes.length > 0) this.selectEpisode(episodes[0].id);
      else {
        this.selectedEpisodeId = null;
        this.assets = [];
        this.beats = [];
      }
      this.cdr.markForCheck();
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.editingId = null;
    this.confirmingDeleteId = null;
    this.api.getBeats(episodeId).subscribe((beats) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.beats = beats;
      this.cdr.markForCheck();
    });
    this.loadAssets(episodeId);
  }

  private loadAssets(episodeId: number): void {
    this.api.getAssets(episodeId).subscribe((assets) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.assets = assets;
      this.cdr.markForCheck();
    });
  }

  create(): void {
    if (this.selectedEpisodeId === null || this.newAssetTypeId === null || !this.newCode.trim()) return;
    this.api.createAsset(this.selectedEpisodeId, {
      assetTypeId: this.newAssetTypeId,
      code: this.newCode,
      title: this.newTitle,
      scriptText: null,
      status: 'Planned',
      notes: null,
      sequenceNumber: null,
      targetLengthSeconds: null,
      phaseId: null,
      attributes: null,
      beatIds: null,
    }).subscribe(() => {
      this.newCode = '';
      this.newTitle = '';
      this.newAssetTypeId = null;
      this.loadAssets(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  startEdit(asset: AssetDto): void {
    this.editingId = asset.id;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  onEditorSaved(updated: AssetDto): void {
    const index = this.assets.findIndex((a) => a.id === updated.id);
    if (index !== -1) {
      this.assets[index] = updated;
    }
    this.editingId = null;
    this.cdr.markForCheck();
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteAsset(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.loadAssets(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
