import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ProjectContextService } from './core/project-context.service';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  mobileNavOpen = false;
  projectMenuOpen = false;

  constructor(protected readonly projectContext: ProjectContextService) {}

  toggleMobileNav(): void {
    this.mobileNavOpen = !this.mobileNavOpen;
  }

  toggleProjectMenu(): void {
    this.projectMenuOpen = !this.projectMenuOpen;
  }

  choose(id: number): void {
    this.projectContext.selectProject(id);
    this.projectMenuOpen = false;
  }

  currentProjectName(): string {
    const id = this.projectContext.selectedProjectId();
    const project = this.projectContext.projects().find((p) => p.id === id);
    return project?.name ?? 'Select project';
  }
}
