import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { BeatsTabComponent } from './beats-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { BeatDto, EpisodeDto } from '../core/models';

describe('BeatsTabComponent', () => {
  let fixture: ComponentFixture<BeatsTabComponent>;
  let component: BeatsTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [],
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'getBeats', 'createBeat', 'updateBeat', 'deleteBeat']);
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getBeats.and.returnValue(of([beat]));

    const contextStub = { selectedProjectId: signal<number | null>(1) };

    await TestBed.configureTestingModule({
      imports: [BeatsTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BeatsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads episodes for the project and beats for the first episode', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(apiSpy.getBeats).toHaveBeenCalledWith(10);
    expect(component.beats).toEqual([beat]);
  });

  it('creates a beat for the selected episode and reloads', () => {
    apiSpy.createBeat.and.returnValue(of({ ...beat, id: 101 }));
    component.newTimecode = '00:40';
    component.newPurpose = 'Next beat';
    component.newOrdinal = 1;
    component.newDurationSeconds = 20;

    component.create();

    expect(apiSpy.createBeat).toHaveBeenCalledWith(10, {
      timecode: '00:40', purpose: 'Next beat', ordinal: 1, durationSeconds: 20,
    });
    expect(apiSpy.getBeats).toHaveBeenCalledTimes(2);
  });

  it('deletes on the second confirm click', () => {
    apiSpy.deleteBeat.and.returnValue(of(undefined));

    component.confirmDelete(beat.id);
    expect(apiSpy.deleteBeat).not.toHaveBeenCalled();

    component.confirmDelete(beat.id);
    expect(apiSpy.deleteBeat).toHaveBeenCalledWith(beat.id);
  });

  it('starts and saves an edit', () => {
    apiSpy.updateBeat.and.returnValue(of({ ...beat, purpose: 'Renamed beat' }));

    component.startEdit(beat);
    component.editPurpose = 'Renamed beat';
    component.saveEdit(beat);

    expect(apiSpy.updateBeat).toHaveBeenCalledWith(beat.id, {
      timecode: beat.timecode, purpose: 'Renamed beat', ordinal: beat.ordinal, durationSeconds: beat.durationSeconds,
    });
    expect(component.editingId).toBeNull();
  });
});
