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
});
