import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { PhasesTabComponent } from './phases-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { PhaseDto } from '../core/models';

describe('PhasesTabComponent', () => {
  let fixture: ComponentFixture<PhasesTabComponent>;
  let component: PhasesTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const phase: PhaseDto = { id: 5, projectId: 1, name: 'Setup A', orderIndex: 0 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getPhases', 'createPhase', 'updatePhase', 'deletePhase']);
    apiSpy.getPhases.and.returnValue(of([phase]));

    const contextStub = { selectedProjectId: signal<number | null>(1) };

    await TestBed.configureTestingModule({
      imports: [PhasesTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PhasesTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads phases for the selected project', () => {
    expect(apiSpy.getPhases).toHaveBeenCalledWith(1);
    expect(component.phases).toEqual([phase]);
  });

  it('creates a phase for the selected project and reloads', () => {
    apiSpy.createPhase.and.returnValue(of({ id: 6, projectId: 1, name: 'Setup B', orderIndex: 1 }));
    component.newName = 'Setup B';
    component.newOrderIndex = 1;

    component.create();

    expect(apiSpy.createPhase).toHaveBeenCalledWith(1, { name: 'Setup B', orderIndex: 1 });
    expect(apiSpy.getPhases).toHaveBeenCalledTimes(2);
  });

  it('deletes on the second confirm click', () => {
    apiSpy.deletePhase.and.returnValue(of(undefined));

    component.confirmDelete(phase.id);
    expect(apiSpy.deletePhase).not.toHaveBeenCalled();

    component.confirmDelete(phase.id);
    expect(apiSpy.deletePhase).toHaveBeenCalledWith(phase.id);
  });

  it('starts and saves an edit', () => {
    apiSpy.updatePhase.and.returnValue(of({ ...phase, name: 'Renamed' }));

    component.startEdit(phase);
    component.editName = 'Renamed';
    component.saveEdit(phase);

    expect(apiSpy.updatePhase).toHaveBeenCalledWith(phase.id, { name: 'Renamed', orderIndex: phase.orderIndex });
    expect(component.editingId).toBeNull();
  });
});
