import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ProjectDto } from '../core/models';

@Component({
  selector: 'app-projects-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects-tab.component.html',
})
export class ProjectsTabComponent {
  newName = '';
  newDescription = '';

  editingId: number | null = null;
  editName = '';
  editDescription = '';

  confirmingDeleteId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {}

  create(): void {
    if (!this.newName.trim()) return;
    this.api.createProject({ name: this.newName, description: this.newDescription || null }).subscribe(() => {
      this.newName = '';
      this.newDescription = '';
      this.projectContext.refresh();
      this.cdr.markForCheck();
    });
  }

  startEdit(project: ProjectDto): void {
    this.editingId = project.id;
    this.editName = project.name;
    this.editDescription = project.description ?? '';
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(project: ProjectDto): void {
    this.api.updateProject(project.id, { name: this.editName, description: this.editDescription || null }).subscribe(() => {
      this.editingId = null;
      this.projectContext.refresh();
      this.cdr.markForCheck();
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteProject(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.projectContext.refresh();
      this.cdr.markForCheck();
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
