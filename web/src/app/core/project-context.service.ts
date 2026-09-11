import { Injectable, signal } from '@angular/core';
import { ApiClientService } from './api-client.service';
import { ProjectDto } from './models';

const STORAGE_KEY = 'pb.selectedProjectId';

@Injectable({ providedIn: 'root' })
export class ProjectContextService {
  private readonly projectsSignal = signal<ProjectDto[]>([]);
  private readonly selectedProjectIdSignal = signal<number | null>(null);

  readonly projects = this.projectsSignal.asReadonly();
  readonly selectedProjectId = this.selectedProjectIdSignal.asReadonly();

  constructor(private readonly api: ApiClientService) {
    this.refresh();
  }

  refresh(): void {
    this.api.getProjects().subscribe((projects) => {
      this.projectsSignal.set(projects);
      if (projects.length === 0) {
        this.selectedProjectIdSignal.set(null);
        return;
      }

      const currentId = this.selectedProjectIdSignal();
      if (currentId !== null && projects.some((p) => p.id === currentId)) return;

      const storedId = this.readStoredId();
      const match = storedId !== null && projects.some((p) => p.id === storedId);
      this.selectedProjectIdSignal.set(match ? storedId! : projects[0].id);
    });
  }

  selectProject(id: number): void {
    this.selectedProjectIdSignal.set(id);
    try {
      localStorage.setItem(STORAGE_KEY, String(id));
    } catch {
      // localStorage unavailable (e.g. private browsing) - selection still works for this session.
    }
  }

  private readStoredId(): number | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw === null ? null : Number(raw);
    } catch {
      return null;
    }
  }
}
