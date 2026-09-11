import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { BeatDto, EpisodeDto } from '../core/models';

@Component({
  selector: 'app-beats-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './beats-tab.component.html',
})
export class BeatsTabComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];

  newTimecode = '';
  newPurpose = '';
  newOrdinal = 0;
  newDurationSeconds = 0;

  editingId: number | null = null;
  editTimecode = '';
  editPurpose = '';
  editOrdinal = 0;
  editDurationSeconds = 0;

  confirmingDeleteId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.editingId = null;
      this.confirmingDeleteId = null;
      if (projectId !== null) this.loadEpisodes(projectId);
      else {
        this.episodes = [];
        this.selectedEpisodeId = null;
        this.beats = [];
      }
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      this.episodes = episodes;
      if (episodes.length > 0) this.selectEpisode(episodes[0].id);
      else {
        this.selectedEpisodeId = null;
        this.beats = [];
      }
      this.cdr.markForCheck();
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.editingId = null;
    this.confirmingDeleteId = null;
    this.loadBeats(episodeId);
  }

  private loadBeats(episodeId: number): void {
    this.api.getBeats(episodeId).subscribe((beats) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.beats = beats;
      this.cdr.markForCheck();
    });
  }

  create(): void {
    if (this.selectedEpisodeId === null || !this.newTimecode.trim()) return;
    this.api.createBeat(this.selectedEpisodeId, {
      timecode: this.newTimecode, purpose: this.newPurpose,
      ordinal: this.newOrdinal, durationSeconds: this.newDurationSeconds,
    }).subscribe(() => {
      this.newTimecode = '';
      this.newPurpose = '';
      this.newOrdinal = 0;
      this.newDurationSeconds = 0;
      this.loadBeats(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  startEdit(beat: BeatDto): void {
    this.editingId = beat.id;
    this.editTimecode = beat.timecode;
    this.editPurpose = beat.purpose;
    this.editOrdinal = beat.ordinal;
    this.editDurationSeconds = beat.durationSeconds;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(beat: BeatDto): void {
    this.api.updateBeat(beat.id, {
      timecode: this.editTimecode, purpose: this.editPurpose,
      ordinal: this.editOrdinal, durationSeconds: this.editDurationSeconds,
    }).subscribe(() => {
      this.editingId = null;
      this.loadBeats(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteBeat(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.loadBeats(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
