import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { of } from 'rxjs';
import { EpisodesTabComponent } from './episodes-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { EpisodeDto } from '../core/models';

describe('EpisodesTabComponent', () => {
  let fixture: ComponentFixture<EpisodesTabComponent>;
  let component: EpisodesTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;
  let selectedProjectId: WritableSignal<number | null>;

  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'createEpisode', 'updateEpisode', 'deleteEpisode']);
    apiSpy.getEpisodes.and.returnValue(of([episode]));

    selectedProjectId = signal<number | null>(1);
    const contextStub = { selectedProjectId };

    await TestBed.configureTestingModule({
      imports: [EpisodesTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EpisodesTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads episodes for the selected project', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.episodes).toEqual([episode]);
  });

  it('creates an episode for the selected project and reloads', () => {
    apiSpy.createEpisode.and.returnValue(of({ id: 11, projectId: 1, name: 'EP6', orderIndex: 6 }));
    component.newName = 'EP6';
    component.newOrderIndex = 6;

    component.create();

    expect(apiSpy.createEpisode).toHaveBeenCalledWith(1, { name: 'EP6', orderIndex: 6 });
    expect(apiSpy.getEpisodes).toHaveBeenCalledTimes(2);
  });

  it('deletes on the second confirm click', () => {
    apiSpy.deleteEpisode.and.returnValue(of(undefined));

    component.confirmDelete(episode.id);
    expect(apiSpy.deleteEpisode).not.toHaveBeenCalled();

    component.confirmDelete(episode.id);
    expect(apiSpy.deleteEpisode).toHaveBeenCalledWith(episode.id);
  });

  it('starts and saves an edit', () => {
    apiSpy.updateEpisode.and.returnValue(of({ ...episode, name: 'Renamed' }));

    component.startEdit(episode);
    component.editName = 'Renamed';
    component.saveEdit(episode);

    expect(apiSpy.updateEpisode).toHaveBeenCalledWith(episode.id, { name: 'Renamed', orderIndex: episode.orderIndex });
    expect(component.editingId).toBeNull();
  });

  it('reloads when the selected project changes', () => {
    const otherEpisode: EpisodeDto = { id: 20, projectId: 2, name: 'EP2', orderIndex: 1 };
    apiSpy.getEpisodes.and.returnValue(of([otherEpisode]));

    selectedProjectId.set(2);
    fixture.detectChanges();

    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(2);
    expect(component.episodes).toEqual([otherEpisode]);
  });
});
