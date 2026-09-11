import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiClientService } from './api-client.service';
import { ProjectDto, AssetDto } from './models';

describe('ApiClientService', () => {
  let service: ApiClientService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiClientService],
    });
    service = TestBed.inject(ApiClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches projects from /api/projects', () => {
    const expected: ProjectDto[] = [{ id: 1, name: 'HalfNut ELS', description: null }];

    service.getProjects().subscribe((projects) => {
      expect(projects).toEqual(expected);
    });

    const req = httpMock.expectOne('/api/projects');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });

  it('fetches assets for an episode from /api/episodes/{id}/assets', () => {
    const expected: AssetDto[] = [];

    service.getAssets(5).subscribe((assets) => {
      expect(assets).toEqual(expected);
    });

    const req = httpMock.expectOne('/api/episodes/5/assets');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });

  it('sends a PUT to /api/assets/{id} for updateAsset', () => {
    service.updateAsset(7, {
      assetTypeId: 1, code: 'A-01', title: 'Title', scriptText: null,
      status: 'Shot', notes: 'Went well', sequenceNumber: 1, targetLengthSeconds: null,
      phaseId: null, attributes: null, beatIds: null,
    }).subscribe();

    const req = httpMock.expectOne('/api/assets/7');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends a POST to /api/projects for createProject', () => {
    service.createProject({ name: 'New Project', description: null }).subscribe();
    const req = httpMock.expectOne('/api/projects');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'New Project', description: null });
    req.flush({});
  });

  it('sends a PUT to /api/projects/{id} for updateProject', () => {
    service.updateProject(1, { name: 'Renamed', description: 'd' }).subscribe();
    const req = httpMock.expectOne('/api/projects/1');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends a DELETE to /api/projects/{id} for deleteProject', () => {
    service.deleteProject(1).subscribe();
    const req = httpMock.expectOne('/api/projects/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('sends a POST to /api/projects/{projectId}/episodes for createEpisode', () => {
    service.createEpisode(1, { name: 'EP6', orderIndex: 6 }).subscribe();
    const req = httpMock.expectOne('/api/projects/1/episodes');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('sends a PUT to /api/episodes/{id} for updateEpisode', () => {
    service.updateEpisode(10, { name: 'EP1', orderIndex: 1 }).subscribe();
    const req = httpMock.expectOne('/api/episodes/10');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends a DELETE to /api/episodes/{id} for deleteEpisode', () => {
    service.deleteEpisode(10).subscribe();
    const req = httpMock.expectOne('/api/episodes/10');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('fetches phases for a project from /api/projects/{id}/phases', () => {
    service.getPhases(1).subscribe();
    const req = httpMock.expectOne('/api/projects/1/phases');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('sends a POST to /api/projects/{projectId}/phases for createPhase', () => {
    service.createPhase(1, { name: 'Setup A', orderIndex: 0 }).subscribe();
    const req = httpMock.expectOne('/api/projects/1/phases');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('sends a PUT to /api/phases/{id} for updatePhase', () => {
    service.updatePhase(5, { name: 'Setup A', orderIndex: 0 }).subscribe();
    const req = httpMock.expectOne('/api/phases/5');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends a DELETE to /api/phases/{id} for deletePhase', () => {
    service.deletePhase(5).subscribe();
    const req = httpMock.expectOne('/api/phases/5');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('sends a POST to /api/episodes/{episodeId}/beats for createBeat', () => {
    service.createBeat(10, { timecode: '00:00', purpose: 'Cold open', ordinal: 0, durationSeconds: 38 }).subscribe();
    const req = httpMock.expectOne('/api/episodes/10/beats');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('sends a PUT to /api/beats/{id} for updateBeat', () => {
    service.updateBeat(100, { timecode: '00:00', purpose: 'Cold open', ordinal: 0, durationSeconds: 38 }).subscribe();
    const req = httpMock.expectOne('/api/beats/100');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends a DELETE to /api/beats/{id} for deleteBeat', () => {
    service.deleteBeat(100).subscribe();
    const req = httpMock.expectOne('/api/beats/100');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('sends a POST to /api/episodes/{episodeId}/assets for createAsset', () => {
    service.createAsset(10, {
      assetTypeId: 1, code: 'A-03', title: 'New shot', scriptText: null,
      status: 'Planned', notes: null, sequenceNumber: null, targetLengthSeconds: null,
      phaseId: null, attributes: null, beatIds: null,
    }).subscribe();
    const req = httpMock.expectOne('/api/episodes/10/assets');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('sends a DELETE to /api/assets/{id} for deleteAsset', () => {
    service.deleteAsset(1000).subscribe();
    const req = httpMock.expectOne('/api/assets/1000');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('sends a PATCH to /api/episodes/{episodeId}/beats/reorder for reorderBeats', () => {
    service.reorderBeats(10, [102, 100, 101]).subscribe();
    const req = httpMock.expectOne('/api/episodes/10/beats/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [102, 100, 101] });
    req.flush(null);
  });

  it('sends a PATCH to /api/beats/{beatId}/asset-beats/reorder for reorderAssetsWithinBeat', () => {
    service.reorderAssetsWithinBeat(100, [1002, 1000, 1001]).subscribe();
    const req = httpMock.expectOne('/api/beats/100/asset-beats/reorder');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ orderedIds: [1002, 1000, 1001] });
    req.flush(null);
  });
});
