import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { PhaseDto } from '../core/models';

@Component({
  selector: 'app-phases-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phases-tab.component.html',
})
export class PhasesTabComponent {
  phases: PhaseDto[] = [];

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
      else this.phases = [];
    });
  }

  private load(projectId: number): void {
    this.api.getPhases(projectId).subscribe((phases) => {
      this.phases = phases;
    });
  }

  create(): void {
    if (!this.newName.trim() || this.projectId === null) return;
    this.api.createPhase(this.projectId, { name: this.newName, orderIndex: this.newOrderIndex }).subscribe(() => {
      this.newName = '';
      this.newOrderIndex = 0;
      this.load(this.projectId!);
    });
  }

  startEdit(phase: PhaseDto): void {
    this.editingId = phase.id;
    this.editName = phase.name;
    this.editOrderIndex = phase.orderIndex;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(phase: PhaseDto): void {
    this.api.updatePhase(phase.id, { name: this.editName, orderIndex: this.editOrderIndex }).subscribe(() => {
      this.editingId = null;
      this.load(this.projectId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deletePhase(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.load(this.projectId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
