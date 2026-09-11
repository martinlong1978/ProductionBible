# Generic CRUD UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a "Manage" section giving full CRUD UI for Project, Episode, Phase, Beat, and Asset, backed entirely by existing backend endpoints.

**Architecture:** One new route/component (`ManageComponent`) with an in-page tab strip; one child component per entity, each following the existing app pattern (standalone Angular component, Tailwind utility classes matching `bible.component.html`/`production-plan.component.html`, jasmine spec with `ApiClientService` spied). `api-client.service.ts` and `models.ts` gain the CRUD surface they're currently missing.

**Tech Stack:** Angular (standalone components), RxJS, Tailwind v4 (`@theme` tokens in `web/src/styles.css`), Jasmine/Karma.

**Spec:** `docs/superpowers/specs/2026-09-11-generic-crud-ui-design.md`

## Global Constraints

- Frontend-only. No backend/C#/migration changes — every endpoint used below already exists (verified against `src/ProductionBible.Api/Controllers/*.cs`).
- No native `confirm()`/`alert()` dialogs — delete uses an in-page two-step confirm.
- Storyboard's existing inline Status/Notes quick-edit (`bible.component.ts`/`.html`) is not modified.
- `timeline-model.ts` is not modified (issue #22 is separate, already tracked).
- No new HTTP error handling — new code follows the existing no-op-on-error pattern (`.subscribe(x => ...)`, no error callback), matching `bible.component.ts`'s `saveEdit`.
- After the last task, rebuild the Angular app into `src/ProductionBible.Api/wwwroot` and verify with `dotnet run` per `CLAUDE.md`'s "Critical gotcha" — a frontend change not rebuilt into wwwroot is invisible to the running app.

---

## File Structure

Create:
- `web/src/app/manage/manage.component.ts` / `.html` / `.spec.ts` — tab shell
- `web/src/app/manage/projects-tab.component.ts` / `.html` / `.spec.ts`
- `web/src/app/manage/episodes-tab.component.ts` / `.html` / `.spec.ts`
- `web/src/app/manage/phases-tab.component.ts` / `.html` / `.spec.ts`
- `web/src/app/manage/beats-tab.component.ts` / `.html` / `.spec.ts`
- `web/src/app/manage/assets-tab.component.ts` / `.html` / `.spec.ts`
- `web/src/app/core/project-context.service.spec.ts` — new, covers the added `refresh()` method

Modify:
- `web/src/app/core/models.ts` — add `PhaseDto` + all missing `Create*Request`/`Update*Request` types, fix stale `BeatDto`
- `web/src/app/core/api-client.service.ts` — add CRUD methods for Project/Episode/Phase/Beat/Asset
- `web/src/app/core/api-client.service.spec.ts` — tests for the new methods
- `web/src/app/core/project-context.service.ts` — add `refresh()`
- `web/src/app/app.routes.ts` — add `/manage` route
- `web/src/app/app.html` — add "Manage" nav link (desktop + mobile)

---

## Task 1: Data layer — models.ts + ApiClientService

**Files:**
- Modify: `web/src/app/core/models.ts`
- Modify: `web/src/app/core/api-client.service.ts`
- Test: `web/src/app/core/api-client.service.spec.ts`

**Interfaces:**
- Produces: `PhaseDto { id, projectId, name, orderIndex }`; `CreateProjectRequest { name, description }`; `UpdateProjectRequest { name, description }`; `CreateEpisodeRequest { name, orderIndex }`; `UpdateEpisodeRequest { name, orderIndex }`; `CreatePhaseRequest { name, orderIndex }`; `UpdatePhaseRequest { name, orderIndex }`; `CreateBeatRequest { timecode, purpose, ordinal, durationSeconds }`; `UpdateBeatRequest { timecode, purpose, ordinal, durationSeconds }`; `CreateAssetRequest { assetTypeId, code, title, scriptText, status, notes, sequenceNumber, targetLengthSeconds, phaseId, attributes, beatIds }`; fixed `BeatDto` (adds `ordinal`, `durationSeconds`, `startSeconds`, `endSeconds`); `ApiClientService.{createProject,updateProject,deleteProject,createEpisode,updateEpisode,deleteEpisode,getPhases,createPhase,updatePhase,deletePhase,createBeat,updateBeat,deleteBeat,createAsset,deleteAsset}`.

- [ ] **Step 1: Write the failing tests**

Append to `web/src/app/core/api-client.service.spec.ts` (after the existing `updateAsset` test, before the closing `});`):

```ts
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — `service.createProject is not a function` (and similarly for every other new method).

- [ ] **Step 3: Add the missing types to models.ts**

In `web/src/app/core/models.ts`, replace the `BeatDto` interface with:

```ts
export interface BeatDto {
  id: number;
  episodeId: number;
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
  startSeconds: number;
  endSeconds: number;
  assetIds: number[];
}

export interface CreateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}

export interface UpdateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}
```

Append at the end of the file:

```ts
export interface PhaseDto {
  id: number;
  projectId: number;
  name: string;
  orderIndex: number;
}

export interface CreatePhaseRequest {
  name: string;
  orderIndex: number;
}

export interface UpdatePhaseRequest {
  name: string;
  orderIndex: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string | null;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
}

export interface CreateEpisodeRequest {
  name: string;
  orderIndex: number;
}

export interface UpdateEpisodeRequest {
  name: string;
  orderIndex: number;
}

export interface CreateAssetRequest {
  assetTypeId: number;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  phaseId: number | null;
  attributes: Record<string, string> | null;
  beatIds: number[] | null;
}
```

- [ ] **Step 4: Add the methods to ApiClientService**

In `web/src/app/core/api-client.service.ts`, update the import line to:

```ts
import {
  AssetDto, AssetTypeDto, BeatDto, CreateAssetRequest, CreateBeatRequest, CreateEpisodeRequest,
  CreatePhaseRequest, CreateProjectRequest, EpisodeDto, PhaseDto, ProjectDto, UpdateAssetRequest,
  UpdateBeatRequest, UpdateEpisodeRequest, UpdatePhaseRequest, UpdateProjectRequest,
} from './models';
```

Append these methods inside the class, after `updateAsset`:

```ts
  createProject(request: CreateProjectRequest): Observable<ProjectDto> {
    return this.http.post<ProjectDto>('/api/projects', request);
  }

  updateProject(id: number, request: UpdateProjectRequest): Observable<ProjectDto> {
    return this.http.put<ProjectDto>(`/api/projects/${id}`, request);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${id}`);
  }

  createEpisode(projectId: number, request: CreateEpisodeRequest): Observable<EpisodeDto> {
    return this.http.post<EpisodeDto>(`/api/projects/${projectId}/episodes`, request);
  }

  updateEpisode(id: number, request: UpdateEpisodeRequest): Observable<EpisodeDto> {
    return this.http.put<EpisodeDto>(`/api/episodes/${id}`, request);
  }

  deleteEpisode(id: number): Observable<void> {
    return this.http.delete<void>(`/api/episodes/${id}`);
  }

  getPhases(projectId: number): Observable<PhaseDto[]> {
    return this.http.get<PhaseDto[]>(`/api/projects/${projectId}/phases`);
  }

  createPhase(projectId: number, request: CreatePhaseRequest): Observable<PhaseDto> {
    return this.http.post<PhaseDto>(`/api/projects/${projectId}/phases`, request);
  }

  updatePhase(id: number, request: UpdatePhaseRequest): Observable<PhaseDto> {
    return this.http.put<PhaseDto>(`/api/phases/${id}`, request);
  }

  deletePhase(id: number): Observable<void> {
    return this.http.delete<void>(`/api/phases/${id}`);
  }

  createBeat(episodeId: number, request: CreateBeatRequest): Observable<BeatDto> {
    return this.http.post<BeatDto>(`/api/episodes/${episodeId}/beats`, request);
  }

  updateBeat(id: number, request: UpdateBeatRequest): Observable<BeatDto> {
    return this.http.put<BeatDto>(`/api/beats/${id}`, request);
  }

  deleteBeat(id: number): Observable<void> {
    return this.http.delete<void>(`/api/beats/${id}`);
  }

  createAsset(episodeId: number, request: CreateAssetRequest): Observable<AssetDto> {
    return this.http.post<AssetDto>(`/api/episodes/${episodeId}/assets`, request);
  }

  deleteAsset(id: number): Observable<void> {
    return this.http.delete<void>(`/api/assets/${id}`);
  }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS — all tests including the new ones.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/core/models.ts web/src/app/core/api-client.service.ts web/src/app/core/api-client.service.spec.ts
git commit -m "feat: add CRUD methods to ApiClientService, fix stale BeatDto"
```

---

## Task 2: ProjectContextService.refresh()

**Why:** The Projects tab (Task 4) creates/edits/deletes projects. `ProjectContextService.projects` is the one signal the nav bar's project switcher reads app-wide — it must be refreshable from outside, or the nav goes stale after a Manage > Projects edit until a full page reload.

**Files:**
- Modify: `web/src/app/core/project-context.service.ts`
- Test: `web/src/app/core/project-context.service.spec.ts` (new file)

**Interfaces:**
- Consumes: `ApiClientService.getProjects(): Observable<ProjectDto[]>` (existing).
- Produces: `ProjectContextService.refresh(): void` — re-fetches the project list, keeps the current selection if it still exists, otherwise falls back to the first project (or `null` if the list is now empty).

- [ ] **Step 1: Write the failing test**

Create `web/src/app/core/project-context.service.spec.ts`:

```ts
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — `service.refresh is not a function`.

- [ ] **Step 3: Implement refresh()**

In `web/src/app/core/project-context.service.ts`, extract the constructor's subscription body into a private method and add `refresh()`:

```ts
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/core/project-context.service.ts web/src/app/core/project-context.service.spec.ts
git commit -m "feat: add ProjectContextService.refresh for Manage > Projects"
```

---

## Task 3: Manage shell — route, nav link, tab strip

**Files:**
- Create: `web/src/app/manage/manage.component.ts`
- Create: `web/src/app/manage/manage.component.html`
- Test: `web/src/app/manage/manage.component.spec.ts`
- Modify: `web/src/app/app.routes.ts`
- Modify: `web/src/app/app.html`

**Interfaces:**
- Produces: `ManageComponent.activeTab: 'projects' | 'episodes' | 'phases' | 'beats' | 'assets'`, `ManageComponent.setTab(tab): void`. Tasks 4-8 each add their own tab component and wire one `<ng-container>` branch into `manage.component.html` — this task ships the shell with all five buttons working and a placeholder message per tab so the route is live end-to-end before any tab has real content.

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/manage.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManageComponent } from './manage.component';

describe('ManageComponent', () => {
  let fixture: ComponentFixture<ManageComponent>;
  let component: ManageComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('defaults to the projects tab', () => {
    expect(component.activeTab).toBe('projects');
  });

  it('switches tabs via setTab', () => {
    component.setTab('assets');
    expect(component.activeTab).toBe('assets');
  });

  it('renders a button per tab', () => {
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button[data-tab]');
    const tabs = Array.from(buttons).map((b) => b.getAttribute('data-tab'));
    expect(tabs).toEqual(['projects', 'episodes', 'phases', 'beats', 'assets']);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./manage.component`.

- [ ] **Step 3: Implement the shell**

Create `web/src/app/manage/manage.component.ts`:

```ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ManageTab = 'projects' | 'episodes' | 'phases' | 'beats' | 'assets';

@Component({
  selector: 'app-manage',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './manage.component.html',
})
export class ManageComponent {
  activeTab: ManageTab = 'projects';

  setTab(tab: ManageTab): void {
    this.activeTab = tab;
  }
}
```

Create `web/src/app/manage/manage.component.html`:

```html
<div class="space-y-6">
  <div class="inline-flex flex-wrap rounded-md border border-border bg-bg p-1">
    <button type="button" data-tab="projects" (click)="setTab('projects')"
            class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
            [class.bg-accent]="activeTab === 'projects'" [class.text-white]="activeTab === 'projects'"
            [class.text-text-secondary]="activeTab !== 'projects'">Projects</button>
    <button type="button" data-tab="episodes" (click)="setTab('episodes')"
            class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
            [class.bg-accent]="activeTab === 'episodes'" [class.text-white]="activeTab === 'episodes'"
            [class.text-text-secondary]="activeTab !== 'episodes'">Episodes</button>
    <button type="button" data-tab="phases" (click)="setTab('phases')"
            class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
            [class.bg-accent]="activeTab === 'phases'" [class.text-white]="activeTab === 'phases'"
            [class.text-text-secondary]="activeTab !== 'phases'">Phases</button>
    <button type="button" data-tab="beats" (click)="setTab('beats')"
            class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
            [class.bg-accent]="activeTab === 'beats'" [class.text-white]="activeTab === 'beats'"
            [class.text-text-secondary]="activeTab !== 'beats'">Beats</button>
    <button type="button" data-tab="assets" (click)="setTab('assets')"
            class="rounded px-3 py-1.5 text-sm font-medium transition-colors"
            [class.bg-accent]="activeTab === 'assets'" [class.text-white]="activeTab === 'assets'"
            [class.text-text-secondary]="activeTab !== 'assets'">Assets</button>
  </div>

  <section *ngIf="activeTab === 'projects'" class="rounded-lg border border-border bg-surface p-4 text-text-secondary">Projects tab coming in Task 4.</section>
  <section *ngIf="activeTab === 'episodes'" class="rounded-lg border border-border bg-surface p-4 text-text-secondary">Episodes tab coming in Task 5.</section>
  <section *ngIf="activeTab === 'phases'" class="rounded-lg border border-border bg-surface p-4 text-text-secondary">Phases tab coming in Task 6.</section>
  <section *ngIf="activeTab === 'beats'" class="rounded-lg border border-border bg-surface p-4 text-text-secondary">Beats tab coming in Task 7.</section>
  <section *ngIf="activeTab === 'assets'" class="rounded-lg border border-border bg-surface p-4 text-text-secondary">Assets tab coming in Task 8.</section>
</div>
```

Add the route in `web/src/app/app.routes.ts` — insert after the `'storyboard'` route:

```ts
  { path: 'manage', component: ManageComponent },
```

(add `import { ManageComponent } from './manage/manage.component';` at the top).

Add the nav link in `web/src/app/app.html`. In the desktop `<nav>` (after the Production Plan link):

```html
          <a routerLink="/manage" routerLinkActive="bg-accent text-white"
             class="rounded-md px-3 py-1.5 text-sm font-medium text-text-secondary hover:bg-surface-hover hover:text-text-primary">Manage</a>
```

And in the mobile `<nav>` (after the Production Plan link):

```html
      <a routerLink="/manage" (click)="toggleMobileNav()"
         class="rounded-md px-3 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover">Manage</a>
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html web/src/app/manage/manage.component.spec.ts web/src/app/app.routes.ts web/src/app/app.html
git commit -m "feat: add Manage section shell with tab navigation"
```

---

## Task 4: Projects tab

**Files:**
- Create: `web/src/app/manage/projects-tab.component.ts`
- Create: `web/src/app/manage/projects-tab.component.html`
- Test: `web/src/app/manage/projects-tab.component.spec.ts`
- Modify: `web/src/app/manage/manage.component.ts` / `.html` (wire in the real component)

**Interfaces:**
- Consumes: `ApiClientService.{createProject,updateProject,deleteProject}` (Task 1), `ProjectContextService.{projects,refresh}` (Task 2).
- Produces: nothing consumed by later tasks — Episodes/Phases tabs (Tasks 5-6) read the project list straight from `ProjectContextService.projects()` and the global selector, not from this component.

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/projects-tab.component.spec.ts`:

```ts
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./projects-tab.component`.

- [ ] **Step 3: Implement the component**

Create `web/src/app/manage/projects-tab.component.ts`:

```ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ProjectDto } from '../core/models';

@Component({
  selector: 'app-projects-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects-tab.component.html',
})
export class ProjectsTabComponent {
  newName = '';
  newDescription = '';

  editingId: number | null = null;
  editName = '';
  editDescription = '';

  confirmingDeleteId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {}

  create(): void {
    if (!this.newName.trim()) return;
    this.api.createProject({ name: this.newName, description: this.newDescription || null }).subscribe(() => {
      this.newName = '';
      this.newDescription = '';
      this.projectContext.refresh();
    });
  }

  startEdit(project: ProjectDto): void {
    this.editingId = project.id;
    this.editName = project.name;
    this.editDescription = project.description ?? '';
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(project: ProjectDto): void {
    this.api.updateProject(project.id, { name: this.editName, description: this.editDescription || null }).subscribe(() => {
      this.editingId = null;
      this.projectContext.refresh();
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteProject(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.projectContext.refresh();
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
```

Create `web/src/app/manage/projects-tab.component.html`:

```html
<div class="space-y-4">
  <div class="rounded-lg border border-border bg-surface">
    <table class="w-full text-left text-sm">
      <thead>
        <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
          <th class="px-4 py-2">Name</th>
          <th class="px-4 py-2">Description</th>
          <th class="px-4 py-2"></th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let project of projectContext.projects()" class="border-b border-border last:border-0 hover:bg-surface-hover">
          <ng-container *ngIf="editingId !== project.id; else editRow">
            <td class="px-4 py-2 font-medium">{{ project.name }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ project.description }}</td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="startEdit(project)"
                      class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
              <button type="button" *ngIf="confirmingDeleteId !== project.id" (click)="confirmDelete(project.id)"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Delete</button>
              <ng-container *ngIf="confirmingDeleteId === project.id">
                <button type="button" (click)="confirmDelete(project.id)"
                        class="ml-2 rounded-md border border-status-error px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Really delete?</button>
                <button type="button" (click)="cancelDelete()"
                        class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
              </ng-container>
            </td>
          </ng-container>
          <ng-template #editRow>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editName"
                     class="w-full rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editDescription"
                     class="w-full rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="saveEdit(project)"
                      class="rounded-md bg-accent px-2 py-1 text-xs font-medium text-white hover:bg-accent-hover">Save</button>
              <button type="button" (click)="cancelEdit()"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
            </td>
          </ng-template>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Name</span>
      <input type="text" [(ngModel)]="newName"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Description</span>
      <input type="text" [(ngModel)]="newDescription"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <button type="button" (click)="create()"
            class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Add project</button>
  </div>
</div>
```

- [ ] **Step 4: Wire into ManageComponent**

In `web/src/app/manage/manage.component.ts`, import `ProjectsTabComponent` and add it to `imports: [CommonModule, ProjectsTabComponent]`.

In `web/src/app/manage/manage.component.html`, replace the projects placeholder `<section>` with:

```html
  <app-projects-tab *ngIf="activeTab === 'projects'"></app-projects-tab>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/manage/projects-tab.component.ts web/src/app/manage/projects-tab.component.html web/src/app/manage/projects-tab.component.spec.ts web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html
git commit -m "feat: add Projects tab CRUD to Manage section"
```

---

## Task 5: Episodes tab

**Files:**
- Create: `web/src/app/manage/episodes-tab.component.ts`
- Create: `web/src/app/manage/episodes-tab.component.html`
- Test: `web/src/app/manage/episodes-tab.component.spec.ts`
- Modify: `web/src/app/manage/manage.component.ts` / `.html`

**Interfaces:**
- Consumes: `ApiClientService.{getEpisodes,createEpisode,updateEpisode,deleteEpisode}` (existing `getEpisodes`, rest from Task 1), `ProjectContextService.selectedProjectId` (existing).
- Produces: nothing consumed elsewhere.

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/episodes-tab.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { EpisodesTabComponent } from './episodes-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { EpisodeDto } from '../core/models';

describe('EpisodesTabComponent', () => {
  let fixture: ComponentFixture<EpisodesTabComponent>;
  let component: EpisodesTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'createEpisode', 'updateEpisode', 'deleteEpisode']);
    apiSpy.getEpisodes.and.returnValue(of([episode]));

    const contextStub = { selectedProjectId: signal<number | null>(1) };

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
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./episodes-tab.component`.

- [ ] **Step 3: Implement the component**

Create `web/src/app/manage/episodes-tab.component.ts`:

```ts
import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { EpisodeDto } from '../core/models';

@Component({
  selector: 'app-episodes-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './episodes-tab.component.html',
})
export class EpisodesTabComponent {
  episodes: EpisodeDto[] = [];

  newName = '';
  newOrderIndex = 0;

  editingId: number | null = null;
  editName = '';
  editOrderIndex = 0;

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      if (projectId !== null) this.load(projectId);
      else this.episodes = [];
    });
  }

  private load(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      this.episodes = episodes;
    });
  }

  create(): void {
    if (!this.newName.trim() || this.projectId === null) return;
    this.api.createEpisode(this.projectId, { name: this.newName, orderIndex: this.newOrderIndex }).subscribe(() => {
      this.newName = '';
      this.newOrderIndex = 0;
      this.load(this.projectId!);
    });
  }

  startEdit(episode: EpisodeDto): void {
    this.editingId = episode.id;
    this.editName = episode.name;
    this.editOrderIndex = episode.orderIndex;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(episode: EpisodeDto): void {
    this.api.updateEpisode(episode.id, { name: this.editName, orderIndex: this.editOrderIndex }).subscribe(() => {
      this.editingId = null;
      this.load(this.projectId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteEpisode(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.load(this.projectId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
```

Create `web/src/app/manage/episodes-tab.component.html`:

```html
<div class="space-y-4">
  <div class="rounded-lg border border-border bg-surface">
    <table class="w-full text-left text-sm">
      <thead>
        <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
          <th class="px-4 py-2">Order</th>
          <th class="px-4 py-2">Name</th>
          <th class="px-4 py-2"></th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let episode of episodes" class="border-b border-border last:border-0 hover:bg-surface-hover">
          <ng-container *ngIf="editingId !== episode.id; else editRow">
            <td class="px-4 py-2 text-text-secondary">{{ episode.orderIndex }}</td>
            <td class="px-4 py-2 font-medium">{{ episode.name }}</td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="startEdit(episode)"
                      class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
              <button type="button" *ngIf="confirmingDeleteId !== episode.id" (click)="confirmDelete(episode.id)"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Delete</button>
              <ng-container *ngIf="confirmingDeleteId === episode.id">
                <button type="button" (click)="confirmDelete(episode.id)"
                        class="ml-2 rounded-md border border-status-error px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Really delete?</button>
                <button type="button" (click)="cancelDelete()"
                        class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
              </ng-container>
            </td>
          </ng-container>
          <ng-template #editRow>
            <td class="px-4 py-2">
              <input type="number" [(ngModel)]="editOrderIndex"
                     class="w-20 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editName"
                     class="w-full rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="saveEdit(episode)"
                      class="rounded-md bg-accent px-2 py-1 text-xs font-medium text-white hover:bg-accent-hover">Save</button>
              <button type="button" (click)="cancelEdit()"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
            </td>
          </ng-template>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Name</span>
      <input type="text" [(ngModel)]="newName"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Order</span>
      <input type="number" [(ngModel)]="newOrderIndex"
             class="w-20 rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <button type="button" (click)="create()"
            class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Add episode</button>
  </div>
</div>
```

- [ ] **Step 4: Wire into ManageComponent**

In `web/src/app/manage/manage.component.ts`, import `EpisodesTabComponent`, add to `imports`.
In `web/src/app/manage/manage.component.html`, replace the episodes placeholder with:

```html
  <app-episodes-tab *ngIf="activeTab === 'episodes'"></app-episodes-tab>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/manage/episodes-tab.component.ts web/src/app/manage/episodes-tab.component.html web/src/app/manage/episodes-tab.component.spec.ts web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html
git commit -m "feat: add Episodes tab CRUD to Manage section"
```

---

## Task 6: Phases tab

**Files:**
- Create: `web/src/app/manage/phases-tab.component.ts`
- Create: `web/src/app/manage/phases-tab.component.html`
- Test: `web/src/app/manage/phases-tab.component.spec.ts`
- Modify: `web/src/app/manage/manage.component.ts` / `.html`

**Interfaces:**
- Consumes: `ApiClientService.{getPhases,createPhase,updatePhase,deletePhase}` (Task 1), `ProjectContextService.selectedProjectId` (existing).
- Produces: nothing consumed elsewhere (Task 8's Asset editor calls `ApiClientService.getPhases` directly for its Phase dropdown, not through this component).

This task is structurally identical to Task 5 (Episodes), scoped to `Phase` instead of `Episode`, using `OrderIndex` the same way.

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/phases-tab.component.spec.ts`:

```ts
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
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./phases-tab.component`.

- [ ] **Step 3: Implement the component**

Create `web/src/app/manage/phases-tab.component.ts`:

```ts
import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { PhaseDto } from '../core/models';

@Component({
  selector: 'app-phases-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phases-tab.component.html',
})
export class PhasesTabComponent {
  phases: PhaseDto[] = [];

  newName = '';
  newOrderIndex = 0;

  editingId: number | null = null;
  editName = '';
  editOrderIndex = 0;

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      if (projectId !== null) this.load(projectId);
      else this.phases = [];
    });
  }

  private load(projectId: number): void {
    this.api.getPhases(projectId).subscribe((phases) => {
      this.phases = phases;
    });
  }

  create(): void {
    if (!this.newName.trim() || this.projectId === null) return;
    this.api.createPhase(this.projectId, { name: this.newName, orderIndex: this.newOrderIndex }).subscribe(() => {
      this.newName = '';
      this.newOrderIndex = 0;
      this.load(this.projectId!);
    });
  }

  startEdit(phase: PhaseDto): void {
    this.editingId = phase.id;
    this.editName = phase.name;
    this.editOrderIndex = phase.orderIndex;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(phase: PhaseDto): void {
    this.api.updatePhase(phase.id, { name: this.editName, orderIndex: this.editOrderIndex }).subscribe(() => {
      this.editingId = null;
      this.load(this.projectId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deletePhase(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.load(this.projectId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
```

Create `web/src/app/manage/phases-tab.component.html` (identical structure to `episodes-tab.component.html`, "Phase" naming):

```html
<div class="space-y-4">
  <div class="rounded-lg border border-border bg-surface">
    <table class="w-full text-left text-sm">
      <thead>
        <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
          <th class="px-4 py-2">Order</th>
          <th class="px-4 py-2">Name</th>
          <th class="px-4 py-2"></th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let phase of phases" class="border-b border-border last:border-0 hover:bg-surface-hover">
          <ng-container *ngIf="editingId !== phase.id; else editRow">
            <td class="px-4 py-2 text-text-secondary">{{ phase.orderIndex }}</td>
            <td class="px-4 py-2 font-medium">{{ phase.name }}</td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="startEdit(phase)"
                      class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
              <button type="button" *ngIf="confirmingDeleteId !== phase.id" (click)="confirmDelete(phase.id)"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Delete</button>
              <ng-container *ngIf="confirmingDeleteId === phase.id">
                <button type="button" (click)="confirmDelete(phase.id)"
                        class="ml-2 rounded-md border border-status-error px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Really delete?</button>
                <button type="button" (click)="cancelDelete()"
                        class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
              </ng-container>
            </td>
          </ng-container>
          <ng-template #editRow>
            <td class="px-4 py-2">
              <input type="number" [(ngModel)]="editOrderIndex"
                     class="w-20 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editName"
                     class="w-full rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="saveEdit(phase)"
                      class="rounded-md bg-accent px-2 py-1 text-xs font-medium text-white hover:bg-accent-hover">Save</button>
              <button type="button" (click)="cancelEdit()"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
            </td>
          </ng-template>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Name</span>
      <input type="text" [(ngModel)]="newName"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Order</span>
      <input type="number" [(ngModel)]="newOrderIndex"
             class="w-20 rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <button type="button" (click)="create()"
            class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Add phase</button>
  </div>
</div>
```

- [ ] **Step 4: Wire into ManageComponent**

In `web/src/app/manage/manage.component.ts`, import `PhasesTabComponent`, add to `imports`.
In `web/src/app/manage/manage.component.html`, replace the phases placeholder with:

```html
  <app-phases-tab *ngIf="activeTab === 'phases'"></app-phases-tab>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/manage/phases-tab.component.ts web/src/app/manage/phases-tab.component.html web/src/app/manage/phases-tab.component.spec.ts web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html
git commit -m "feat: add Phases tab CRUD to Manage section"
```

---

## Task 7: Beats tab

**Files:**
- Create: `web/src/app/manage/beats-tab.component.ts`
- Create: `web/src/app/manage/beats-tab.component.html`
- Test: `web/src/app/manage/beats-tab.component.spec.ts`
- Modify: `web/src/app/manage/manage.component.ts` / `.html`

**Interfaces:**
- Consumes: `ApiClientService.{getEpisodes,getBeats,createBeat,updateBeat,deleteBeat}` (existing `getEpisodes`/`getBeats`, rest from Task 1).
- Produces: nothing consumed elsewhere.

Adds an episode picker (same `<select>` pattern as `bible.component.html`) since Manage has no global episode selection.

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/beats-tab.component.spec.ts`:

```ts
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
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./beats-tab.component`.

- [ ] **Step 3: Implement the component**

Create `web/src/app/manage/beats-tab.component.ts`:

```ts
import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { BeatDto, EpisodeDto } from '../core/models';

@Component({
  selector: 'app-beats-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './beats-tab.component.html',
})
export class BeatsTabComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];

  newTimecode = '';
  newPurpose = '';
  newOrdinal = 0;
  newDurationSeconds = 0;

  editingId: number | null = null;
  editTimecode = '';
  editPurpose = '';
  editOrdinal = 0;
  editDurationSeconds = 0;

  confirmingDeleteId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) this.loadEpisodes(projectId);
      else {
        this.episodes = [];
        this.selectedEpisodeId = null;
        this.beats = [];
      }
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      this.episodes = episodes;
      if (episodes.length > 0) this.selectEpisode(episodes[0].id);
      else {
        this.selectedEpisodeId = null;
        this.beats = [];
      }
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.loadBeats(episodeId);
  }

  private loadBeats(episodeId: number): void {
    this.api.getBeats(episodeId).subscribe((beats) => {
      this.beats = beats;
    });
  }

  create(): void {
    if (this.selectedEpisodeId === null) return;
    this.api.createBeat(this.selectedEpisodeId, {
      timecode: this.newTimecode, purpose: this.newPurpose,
      ordinal: this.newOrdinal, durationSeconds: this.newDurationSeconds,
    }).subscribe(() => {
      this.newTimecode = '';
      this.newPurpose = '';
      this.newOrdinal = 0;
      this.newDurationSeconds = 0;
      this.loadBeats(this.selectedEpisodeId!);
    });
  }

  startEdit(beat: BeatDto): void {
    this.editingId = beat.id;
    this.editTimecode = beat.timecode;
    this.editPurpose = beat.purpose;
    this.editOrdinal = beat.ordinal;
    this.editDurationSeconds = beat.durationSeconds;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(beat: BeatDto): void {
    this.api.updateBeat(beat.id, {
      timecode: this.editTimecode, purpose: this.editPurpose,
      ordinal: this.editOrdinal, durationSeconds: this.editDurationSeconds,
    }).subscribe(() => {
      this.editingId = null;
      this.loadBeats(this.selectedEpisodeId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteBeat(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.loadBeats(this.selectedEpisodeId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
```

Create `web/src/app/manage/beats-tab.component.html`:

```html
<div class="space-y-4">
  <div class="flex items-center gap-3 rounded-lg border border-border bg-surface p-4">
    <label for="beats-episode-select" class="text-sm font-medium text-text-secondary">Episode</label>
    <select id="beats-episode-select" [ngModel]="selectedEpisodeId" (ngModelChange)="selectEpisode($event)"
            class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm text-text-primary focus:border-accent focus:outline-none">
      <option *ngFor="let ep of episodes" [ngValue]="ep.id">{{ ep.name }}</option>
    </select>
  </div>

  <div class="rounded-lg border border-border bg-surface">
    <table class="w-full text-left text-sm">
      <thead>
        <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
          <th class="px-4 py-2">Ordinal</th>
          <th class="px-4 py-2">Timecode</th>
          <th class="px-4 py-2">Purpose</th>
          <th class="px-4 py-2">Duration (s)</th>
          <th class="px-4 py-2"></th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let beat of beats" class="border-b border-border last:border-0 hover:bg-surface-hover">
          <ng-container *ngIf="editingId !== beat.id; else editRow">
            <td class="px-4 py-2 text-text-secondary">{{ beat.ordinal }}</td>
            <td class="px-4 py-2 font-mono">{{ beat.timecode }}</td>
            <td class="px-4 py-2 font-medium">{{ beat.purpose }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ beat.durationSeconds }}</td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="startEdit(beat)"
                      class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
              <button type="button" *ngIf="confirmingDeleteId !== beat.id" (click)="confirmDelete(beat.id)"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Delete</button>
              <ng-container *ngIf="confirmingDeleteId === beat.id">
                <button type="button" (click)="confirmDelete(beat.id)"
                        class="ml-2 rounded-md border border-status-error px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Really delete?</button>
                <button type="button" (click)="cancelDelete()"
                        class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
              </ng-container>
            </td>
          </ng-container>
          <ng-template #editRow>
            <td class="px-4 py-2">
              <input type="number" [(ngModel)]="editOrdinal"
                     class="w-16 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editTimecode"
                     class="w-24 rounded-md border border-border bg-bg px-2 py-1 text-sm font-mono focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="text" [(ngModel)]="editPurpose"
                     class="w-full rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2">
              <input type="number" [(ngModel)]="editDurationSeconds"
                     class="w-20 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
            </td>
            <td class="px-4 py-2 text-right">
              <button type="button" (click)="saveEdit(beat)"
                      class="rounded-md bg-accent px-2 py-1 text-xs font-medium text-white hover:bg-accent-hover">Save</button>
              <button type="button" (click)="cancelEdit()"
                      class="ml-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
            </td>
          </ng-template>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Timecode</span>
      <input type="text" [(ngModel)]="newTimecode" placeholder="00:00"
             class="w-24 rounded-md border border-border bg-bg px-3 py-1.5 text-sm font-mono focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Purpose</span>
      <input type="text" [(ngModel)]="newPurpose"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Ordinal</span>
      <input type="number" [(ngModel)]="newOrdinal"
             class="w-16 rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Duration (s)</span>
      <input type="number" [(ngModel)]="newDurationSeconds"
             class="w-24 rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <button type="button" (click)="create()"
            class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Add beat</button>
  </div>
</div>
```

- [ ] **Step 4: Wire into ManageComponent**

In `web/src/app/manage/manage.component.ts`, import `BeatsTabComponent`, add to `imports`.
In `web/src/app/manage/manage.component.html`, replace the beats placeholder with:

```html
  <app-beats-tab *ngIf="activeTab === 'beats'"></app-beats-tab>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/manage/beats-tab.component.ts web/src/app/manage/beats-tab.component.html web/src/app/manage/beats-tab.component.spec.ts web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html
git commit -m "feat: add Beats tab CRUD to Manage section"
```

---

## Task 8: Assets tab (full editor)

**Files:**
- Create: `web/src/app/manage/assets-tab.component.ts`
- Create: `web/src/app/manage/assets-tab.component.html`
- Test: `web/src/app/manage/assets-tab.component.spec.ts`
- Modify: `web/src/app/manage/manage.component.ts` / `.html`

**Interfaces:**
- Consumes: `ApiClientService.{getEpisodes,getAssets,getAssetTypes,getPhases,getBeats,createAsset,updateAsset,deleteAsset}` (existing `getEpisodes`/`getAssets`/`getAssetTypes`/`updateAsset`, rest from Task 1), `ASSET_STATUSES` (`web/src/app/core/status-style.ts`, existing), `ProjectContextService.selectedProjectId`.
- Produces: nothing consumed elsewhere. This is the terminal task for Track B item #3 — the full editor's Beat-links multi-select and Attributes editor satisfy the "AssetBeat link management UI" requirement directly (see spec's "Per-entity fields" section).

- [ ] **Step 1: Write the failing test**

Create `web/src/app/manage/assets-tab.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AssetsTabComponent } from './assets-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto } from '../core/models';

describe('AssetsTabComponent', () => {
  let fixture: ComponentFixture<AssetsTabComponent>;
  let component: AssetsTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const assetType: AssetTypeDto = { id: 1, name: 'Shot' };
  const phase: PhaseDto = { id: 5, projectId: 1, name: 'Setup A', orderIndex: 0 };
  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const asset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, phaseId: 5, completedAtUtc: null,
    attributes: { Location: 'Workshop' }, beatIds: [100],
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getAssets', 'getAssetTypes', 'getPhases', 'getBeats',
      'createAsset', 'updateAsset', 'deleteAsset',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getAssets.and.returnValue(of([asset]));
    apiSpy.getAssetTypes.and.returnValue(of([assetType]));
    apiSpy.getPhases.and.returnValue(of([phase]));
    apiSpy.getBeats.and.returnValue(of([beat]));

    const contextStub = { selectedProjectId: signal<number | null>(1) };

    await TestBed.configureTestingModule({
      imports: [AssetsTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads episode, asset type, phase, and asset data for the selected project', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(apiSpy.getAssetTypes).toHaveBeenCalled();
    expect(apiSpy.getPhases).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(apiSpy.getAssets).toHaveBeenCalledWith(10);
    expect(component.assets).toEqual([asset]);
    expect(component.beats).toEqual([beat]);
  });

  it('starts an edit with the full field set including attributes and beat links', () => {
    component.startEdit(asset);
    expect(component.editCode).toBe('A-01');
    expect(component.editAttributes).toEqual([{ key: 'Location', value: 'Workshop' }]);
    expect(component.editBeatIds).toEqual([100]);
  });

  it('saves an edit, rebuilding the attributes map from the editable rows', () => {
    apiSpy.updateAsset.and.returnValue(of(asset));
    component.startEdit(asset);
    component.editAttributes = [{ key: 'Location', value: 'Studio' }, { key: 'Notes', value: 'Reshoot' }];
    component.editBeatIds = [100];

    component.saveEdit(asset);

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(1000, jasmine.objectContaining({
      attributes: { Location: 'Studio', Notes: 'Reshoot' },
      beatIds: [100],
    }));
  });

  it('creates an asset for the selected episode and reloads', () => {
    apiSpy.createAsset.and.returnValue(of({ ...asset, id: 1001, code: 'A-02' }));
    component.newCode = 'A-02';
    component.newTitle = 'New shot';
    component.newAssetTypeId = 1;

    component.create();

    expect(apiSpy.createAsset).toHaveBeenCalledWith(10, jasmine.objectContaining({
      code: 'A-02', title: 'New shot', assetTypeId: 1, status: 'Planned',
    }));
    expect(apiSpy.getAssets).toHaveBeenCalledTimes(2);
  });

  it('deletes on the second confirm click', () => {
    apiSpy.deleteAsset.and.returnValue(of(undefined));

    component.confirmDelete(asset.id);
    expect(apiSpy.deleteAsset).not.toHaveBeenCalled();

    component.confirmDelete(asset.id);
    expect(apiSpy.deleteAsset).toHaveBeenCalledWith(asset.id);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx ng test --watch=false`
Expected: FAIL — cannot find module `./assets-tab.component`.

- [ ] **Step 3: Implement the component**

Create `web/src/app/manage/assets-tab.component.ts`:

```ts
import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ASSET_STATUSES } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto } from '../core/models';

interface AttributeRow {
  key: string;
  value: string;
}

@Component({
  selector: 'app-assets-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assets-tab.component.html',
})
export class AssetsTabComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  assets: AssetDto[] = [];
  assetTypes: AssetTypeDto[] = [];
  phases: PhaseDto[] = [];
  beats: BeatDto[] = [];
  protected readonly assetStatuses = ASSET_STATUSES;

  newCode = '';
  newTitle = '';
  newAssetTypeId: number | null = null;

  editingId: number | null = null;
  editCode = '';
  editTitle = '';
  editScriptText = '';
  editAssetTypeId = 0;
  editStatus = '';
  editNotes = '';
  editSequenceNumber: number | null = null;
  editTargetLengthSeconds: number | null = null;
  editPhaseId: number | null = null;
  editAttributes: AttributeRow[] = [];
  editBeatIds: number[] = [];

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    protected readonly projectContext: ProjectContextService,
  ) {
    this.api.getAssetTypes().subscribe((types) => {
      this.assetTypes = types;
    });

    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      if (projectId === null) {
        this.episodes = [];
        this.selectedEpisodeId = null;
        this.assets = [];
        this.phases = [];
        return;
      }
      this.api.getPhases(projectId).subscribe((phases) => {
        this.phases = phases;
      });
      this.loadEpisodes(projectId);
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      this.episodes = episodes;
      if (episodes.length > 0) this.selectEpisode(episodes[0].id);
      else {
        this.selectedEpisodeId = null;
        this.assets = [];
        this.beats = [];
      }
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.api.getBeats(episodeId).subscribe((beats) => {
      this.beats = beats;
    });
    this.loadAssets(episodeId);
  }

  private loadAssets(episodeId: number): void {
    this.api.getAssets(episodeId).subscribe((assets) => {
      this.assets = assets;
    });
  }

  create(): void {
    if (this.selectedEpisodeId === null || this.newAssetTypeId === null || !this.newCode.trim()) return;
    this.api.createAsset(this.selectedEpisodeId, {
      assetTypeId: this.newAssetTypeId,
      code: this.newCode,
      title: this.newTitle,
      scriptText: null,
      status: 'Planned',
      notes: null,
      sequenceNumber: null,
      targetLengthSeconds: null,
      phaseId: null,
      attributes: null,
      beatIds: null,
    }).subscribe(() => {
      this.newCode = '';
      this.newTitle = '';
      this.newAssetTypeId = null;
      this.loadAssets(this.selectedEpisodeId!);
    });
  }

  startEdit(asset: AssetDto): void {
    this.editingId = asset.id;
    this.editCode = asset.code;
    this.editTitle = asset.title;
    this.editScriptText = asset.scriptText ?? '';
    this.editAssetTypeId = asset.assetTypeId;
    this.editStatus = asset.status;
    this.editNotes = asset.notes ?? '';
    this.editSequenceNumber = asset.sequenceNumber;
    this.editTargetLengthSeconds = asset.targetLengthSeconds;
    this.editPhaseId = asset.phaseId;
    this.editAttributes = Object.entries(asset.attributes).map(([key, value]) => ({ key, value }));
    this.editBeatIds = [...asset.beatIds];
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  addAttributeRow(): void {
    this.editAttributes = [...this.editAttributes, { key: '', value: '' }];
  }

  removeAttributeRow(index: number): void {
    this.editAttributes = this.editAttributes.filter((_, i) => i !== index);
  }

  toggleBeatLink(beatId: number, linked: boolean): void {
    this.editBeatIds = linked
      ? [...this.editBeatIds, beatId]
      : this.editBeatIds.filter((id) => id !== beatId);
  }

  saveEdit(asset: AssetDto): void {
    const attributes: Record<string, string> = {};
    for (const row of this.editAttributes) {
      if (row.key.trim()) attributes[row.key] = row.value;
    }

    this.api.updateAsset(asset.id, {
      assetTypeId: this.editAssetTypeId,
      code: this.editCode,
      title: this.editTitle,
      scriptText: this.editScriptText || null,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: this.editSequenceNumber,
      targetLengthSeconds: this.editTargetLengthSeconds,
      phaseId: this.editPhaseId,
      attributes,
      beatIds: this.editBeatIds,
    }).subscribe(() => {
      this.editingId = null;
      this.loadAssets(this.selectedEpisodeId!);
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteAsset(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.loadAssets(this.selectedEpisodeId!);
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
```

Create `web/src/app/manage/assets-tab.component.html`:

```html
<div class="space-y-4">
  <div class="flex items-center gap-3 rounded-lg border border-border bg-surface p-4">
    <label for="assets-episode-select" class="text-sm font-medium text-text-secondary">Episode</label>
    <select id="assets-episode-select" [ngModel]="selectedEpisodeId" (ngModelChange)="selectEpisode($event)"
            class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm text-text-primary focus:border-accent focus:outline-none">
      <option *ngFor="let ep of episodes" [ngValue]="ep.id">{{ ep.name }}</option>
    </select>
  </div>

  <ul class="space-y-2">
    <li *ngFor="let asset of assets" class="rounded-lg border border-border bg-surface p-4">
      <ng-container *ngIf="editingId !== asset.id; else editForm">
        <div class="flex flex-wrap items-center gap-2">
          <span class="font-mono text-sm text-text-secondary">{{ asset.code }}</span>
          <span class="font-medium">{{ asset.title }}</span>
          <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white"
                [ngClass]="{'bg-status-planned': asset.status === 'Planned'}">{{ asset.status }}</span>
          <button type="button" (click)="startEdit(asset)"
                  class="ml-auto rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
          <button type="button" *ngIf="confirmingDeleteId !== asset.id" (click)="confirmDelete(asset.id)"
                  class="rounded-md border border-border px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Delete</button>
          <ng-container *ngIf="confirmingDeleteId === asset.id">
            <button type="button" (click)="confirmDelete(asset.id)"
                    class="rounded-md border border-status-error px-2 py-1 text-xs font-medium text-status-error hover:bg-surface-hover">Really delete?</button>
            <button type="button" (click)="cancelDelete()"
                    class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Cancel</button>
          </ng-container>
        </div>
      </ng-container>

      <ng-template #editForm>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Code</span>
            <input type="text" [(ngModel)]="editCode"
                   class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Title</span>
            <input type="text" [(ngModel)]="editTitle"
                   class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Asset type</span>
            <select [(ngModel)]="editAssetTypeId"
                    class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
              <option *ngFor="let type of assetTypes" [ngValue]="type.id">{{ type.name }}</option>
            </select>
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Status</span>
            <select [(ngModel)]="editStatus"
                    class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
              <option *ngFor="let s of assetStatuses" [value]="s">{{ s }}</option>
            </select>
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Sequence number</span>
            <input type="number" [(ngModel)]="editSequenceNumber"
                   class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Target length (s)</span>
            <input type="number" [(ngModel)]="editTargetLengthSeconds"
                   class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
          </label>
          <label class="block text-sm">
            <span class="mb-1 block text-text-secondary">Phase</span>
            <select [(ngModel)]="editPhaseId"
                    class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
              <option [ngValue]="null">(none)</option>
              <option *ngFor="let phase of phases" [ngValue]="phase.id">{{ phase.name }}</option>
            </select>
          </label>
          <label class="block text-sm sm:col-span-2">
            <span class="mb-1 block text-text-secondary">Script text</span>
            <textarea [(ngModel)]="editScriptText"
                      class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none"></textarea>
          </label>
          <label class="block text-sm sm:col-span-2">
            <span class="mb-1 block text-text-secondary">Notes</span>
            <textarea [(ngModel)]="editNotes"
                      class="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none"></textarea>
          </label>
        </div>

        <div class="mt-3">
          <span class="mb-1 block text-sm text-text-secondary">Attributes</span>
          <div class="space-y-2">
            <div *ngFor="let row of editAttributes; let i = index" class="flex items-center gap-2">
              <input type="text" [(ngModel)]="row.key" placeholder="Key"
                     class="w-1/3 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
              <input type="text" [(ngModel)]="row.value" placeholder="Value"
                     class="flex-1 rounded-md border border-border bg-bg px-2 py-1 text-sm focus:border-accent focus:outline-none" />
              <button type="button" (click)="removeAttributeRow(i)"
                      class="rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Remove</button>
            </div>
          </div>
          <button type="button" (click)="addAttributeRow()"
                  class="mt-2 rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Add attribute</button>
        </div>

        <div class="mt-3">
          <span class="mb-1 block text-sm text-text-secondary">Beat links</span>
          <div class="flex flex-wrap gap-3">
            <label *ngFor="let beat of beats" class="flex items-center gap-1.5 text-sm">
              <input type="checkbox" [checked]="editBeatIds.includes(beat.id)"
                     (change)="toggleBeatLink(beat.id, $any($event.target).checked)" />
              {{ beat.timecode }} &mdash; {{ beat.purpose }}
            </label>
          </div>
        </div>

        <div class="mt-3 flex gap-2">
          <button type="button" (click)="saveEdit(asset)"
                  class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Save</button>
          <button type="button" (click)="cancelEdit()"
                  class="rounded-md border border-border px-3 py-1.5 text-sm font-medium hover:bg-surface-hover">Cancel</button>
        </div>
      </ng-template>
    </li>
  </ul>

  <div class="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-surface p-4">
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Code</span>
      <input type="text" [(ngModel)]="newCode"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Title</span>
      <input type="text" [(ngModel)]="newTitle"
             class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
    </label>
    <label class="text-sm">
      <span class="mb-1 block text-text-secondary">Asset type</span>
      <select [(ngModel)]="newAssetTypeId"
              class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm focus:border-accent focus:outline-none">
        <option [ngValue]="null">(select)</option>
        <option *ngFor="let type of assetTypes" [ngValue]="type.id">{{ type.name }}</option>
      </select>
    </label>
    <button type="button" (click)="create()"
            class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Add asset</button>
  </div>
</div>
```

- [ ] **Step 4: Wire into ManageComponent**

In `web/src/app/manage/manage.component.ts`, import `AssetsTabComponent`, add to `imports`.
In `web/src/app/manage/manage.component.html`, replace the assets placeholder with:

```html
  <app-assets-tab *ngIf="activeTab === 'assets'"></app-assets-tab>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd web && npx ng test --watch=false`
Expected: PASS — full suite.

- [ ] **Step 6: Commit**

```bash
git add web/src/app/manage/assets-tab.component.ts web/src/app/manage/assets-tab.component.html web/src/app/manage/assets-tab.component.spec.ts web/src/app/manage/manage.component.ts web/src/app/manage/manage.component.html
git commit -m "feat: add Assets tab full editor to Manage section"
```

---

## Task 9: Rebuild and live verification

**Why:** Per `CLAUDE.md`'s "Critical gotcha" — `dotnet run` serves a pre-built, committed copy of the Angular app. Every task above changed `web/src/`, which is invisible to `dotnet run` until rebuilt into `wwwroot` and verified live, not just via `npm start` or `ng test`.

**Files:**
- Modify: `src/ProductionBible.Api/wwwroot/**` (generated, committed build output)

- [ ] **Step 1: Run the full frontend test suite**

Run: `cd web && npx ng test --watch=false`
Expected: PASS — all specs across the whole app, not just this feature's.

- [ ] **Step 2: Run the full backend test suite**

Run: `dotnet test ProductionBible.sln`
Expected: PASS. (No backend changes in this plan, but this is the project's standard pre-merge gate.)

- [ ] **Step 3: Rebuild the frontend into wwwroot**

Run: `cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot`
Expected: build succeeds with no errors.

- [ ] **Step 4: Run the app and verify live in a browser**

Run: `dotnet run --project src/ProductionBible.Api` (background), then navigate to `http://localhost:5280/manage` in a browser (or Claude-in-Chrome). Verify:
- All five tabs render and switch.
- Projects tab: create a project, edit its name, delete it (two-click confirm), and confirm the nav bar's project dropdown picks up the change without a page reload (proves `ProjectContextService.refresh()` works).
- Episodes tab: create an episode for the current project, edit it, delete it.
- Phases tab: create a phase, edit it, delete it.
- Beats tab: switch episodes via the picker, create a beat with an Ordinal/Duration, edit it, delete it.
- Assets tab: create an asset, open its full editor, add an attribute row, toggle a beat link, save, and confirm the change round-trips (re-open the editor and see the same attribute/beat link).
- Storyboard view still works unchanged (quick Status/Notes edit only, no regression from the Beat/Asset CRUD changes).

Stop the app afterward.

- [ ] **Step 5: Commit the rebuilt wwwroot**

```bash
git add src/ProductionBible.Api/wwwroot
git commit -m "chore: rebuild wwwroot for Manage section"
```

---

## Self-Review Notes

- **Spec coverage:** routing/nav (Task 3), API client additions (Task 1), models.ts fixes (Task 1), shared row-list/delete-confirm/full-edit patterns (Tasks 4-8, each reimplementing the small pattern locally per the spec's YAGNI note), all five per-entity field sets (Tasks 4-8 table), AssetBeat link management via Asset's beat-links multi-select (Task 8), error handling (no new pattern — every task's `.subscribe()` matches the existing no-op style), testing (a spec file per task). Deferred items (drag-reorder, AssetType CRUD, `timeline-model.ts`, app-wide error handling) are untouched by every task above, matching the spec's exclusions.
- **Type consistency:** `BeatDto`/`CreateBeatRequest`/`UpdateBeatRequest` field names (`ordinal`, `durationSeconds`, `timecode`, `purpose`) are used identically in Task 1 (definition), Task 7 (Beats tab), and Task 8's test fixtures. `PhaseDto` (`id`, `projectId`, `name`, `orderIndex`) matches across Tasks 1, 6, and 8. `ProjectContextService.refresh()` (Task 2) is called by name only from Task 4 (Projects tab) — verified the name matches.
- **Placeholder scan:** no TBD/TODO markers; every step has runnable code and exact commands.
