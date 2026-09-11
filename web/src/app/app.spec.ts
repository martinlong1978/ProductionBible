import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { App } from './app';
import { ProjectContextService } from './core/project-context.service';
import { ProjectDto } from './core/models';

describe('App', () => {
  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };

  beforeEach(async () => {
    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render navigation links and the current project name', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('nav')).toBeTruthy();
    expect(compiled.textContent).toContain('Storyboard');
    expect(compiled.textContent).toContain('Production Plan');
    expect(compiled.textContent).toContain('HalfNut ELS');
  });
});
