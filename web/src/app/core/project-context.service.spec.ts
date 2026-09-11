import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { ProjectContextService } from './project-context.service';
import { ApiClientService } from './api-client.service';
import { ProjectDto } from './models';

describe('ProjectContextService', () => {
  let projectsSubject: Subject<ProjectDto[]>;
  let apiSpy: jasmine.SpyObj<ApiClientService>;
  let service: ProjectContextService;

  const projectA: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const projectB: ProjectDto = { id: 2, name: 'Second Project', description: null };

  beforeEach(() => {
    projectsSubject = new Subject<ProjectDto[]>();
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getProjects']);
    apiSpy.getProjects.and.returnValue(projectsSubject.asObservable());

    TestBed.configureTestingModule({
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    });
    service = TestBed.inject(ProjectContextService);
    projectsSubject.next([projectA]);
  });

  it('keeps the current selection on refresh if it still exists', () => {
    expect(service.selectedProjectId()).toBe(1);

    apiSpy.getProjects.and.returnValue(of([projectA, projectB]));
    service.refresh();

    expect(service.projects()).toEqual([projectA, projectB]);
    expect(service.selectedProjectId()).toBe(1);
  });

  it('falls back to the first project if the selected one was deleted', () => {
    service.selectProject(1);
    apiSpy.getProjects.and.returnValue(of([projectB]));
    service.refresh();

    expect(service.selectedProjectId()).toBe(2);
  });
});
