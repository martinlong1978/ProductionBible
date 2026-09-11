import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { ProjectsTabComponent } from './projects-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ProjectDto } from '../core/models';

describe('ProjectsTabComponent', () => {
  let fixture: ComponentFixture<ProjectsTabComponent>;
  let component: ProjectsTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;
  let contextSpy: jasmine.SpyObj<ProjectContextService>;

  const projectA: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['createProject', 'updateProject', 'deleteProject']);
    contextSpy = jasmine.createSpyObj('ProjectContextService', ['refresh'], {
      projects: signal<ProjectDto[]>([projectA]),
    });

    await TestBed.configureTestingModule({
      imports: [ProjectsTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates a project and refreshes the context', () => {
    apiSpy.createProject.and.returnValue(of({ id: 2, name: 'New Project', description: null }));
    component.newName = 'New Project';
    component.newDescription = '';

    component.create();

    expect(apiSpy.createProject).toHaveBeenCalledWith({ name: 'New Project', description: null });
    expect(contextSpy.refresh).toHaveBeenCalled();
    expect(component.newName).toBe('');
  });

  it('starts and saves an edit', () => {
    apiSpy.updateProject.and.returnValue(of({ id: 1, name: 'Renamed', description: null }));

    component.startEdit(projectA);
    component.editName = 'Renamed';
    component.saveEdit(projectA);

    expect(apiSpy.updateProject).toHaveBeenCalledWith(1, { name: 'Renamed', description: null });
    expect(contextSpy.refresh).toHaveBeenCalled();
    expect(component.editingId).toBeNull();
  });

  it('requires a second click to actually delete', () => {
    apiSpy.deleteProject.and.returnValue(of(undefined));

    component.confirmDelete(projectA.id);
    expect(apiSpy.deleteProject).not.toHaveBeenCalled();
    expect(component.confirmingDeleteId).toBe(projectA.id);

    component.confirmDelete(projectA.id);
    expect(apiSpy.deleteProject).toHaveBeenCalledWith(projectA.id);
    expect(contextSpy.refresh).toHaveBeenCalled();
  });
});
