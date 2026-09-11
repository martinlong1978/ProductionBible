import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { EpisodeDto } from '../core/models';

@Component({
  selector: 'app-episodes-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './episodes-tab.component.html',
})
export class EpisodesTabComponent {
  episodes: EpisodeDto[] = [];

  newName = '';
  newOrderIndex = 0;

  editingId: number | null = null;
  editName = '';
  editOrderIndex = 0;

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      if (projectId !== null) this.load(projectId);
      else this.episodes = [];
    });
  }

  private load(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      this.episodes = episodes;
    });
  }

  create(): void {
    if (!this.newName.trim() || this.projectId === null) return;
    this.api.createEpisode(this.projectId, { name: this.newName, orderIndex: this.newOrderIndex }).subscribe(() => {
      this.newName = '';
      this.newOrderIndex = 0;
      this.load(this.projectId!);
    });
  }

  startEdit(episode: EpisodeDto): void {
    this.editingId = episode.id;
    this.editName = episode.name;
    this.editOrderIndex = episode.orderIndex;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(episode: EpisodeDto): void {
    this.api.updateEpisode(episode.id, { name: this.editName, orderIndex: this.editOrderIndex }).subscribe(() => {
      this.editingId = null;
      this.load(this.projectId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteEpisode(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.load(this.projectId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
