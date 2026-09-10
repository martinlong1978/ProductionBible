# ProductionBible UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the ProductionBible frontend's zero-styling UI with a navigation shell (wordmark, tabs, a project selector) and a modern Tailwind CSS dark theme applied consistently across the Bible, Timeline, and Production Plan views.

**Architecture:** A new signal-based `ProjectContextService` becomes the single source of truth for "which project is selected," replacing each view's own independent `getProjects()` call. Tailwind CSS (utility classes, no component-scoped CSS files) provides the visual layer. Each of the three existing views gets restyled in its own task; `BibleComponent` and `ProductionPlanComponent` also switch their data-loading trigger from `ngOnInit` to an `effect()` watching `ProjectContextService.selectedProjectId()`.

**Tech Stack:** Angular 22 (standalone components, `@angular/build:application` esbuild builder), Tailwind CSS v4 (CSS-first config via `@theme`), RxJS (existing `ApiClientService` pattern, unchanged).

**Spec:** `docs/superpowers/specs/2026-09-10-ui-redesign-design.md`

## Global Constraints

- Frontend-only. Do not touch `src/ProductionBible.Api`, `src/ProductionBible.Application`, or `src/ProductionBible.Importer`.
- Preserve the `ChangeDetectorRef.markForCheck()` call in every state-mutating `.subscribe()` callback — this app has no zoneless/OnPush + signal-driven auto-rendering wired up for `HttpClient`-based async updates (Phase 1 Task 16 finding). Signal *reads* in templates (e.g. `projectContext.projects()`) do not need this — only plain-field mutations inside `.subscribe()` callbacks do.
- No new routes. "Timeline" stays a view-mode toggle inside `BibleComponent`, not a route.
- Any new npm dependency is pinned to an exact version in `package.json` (no `^`/`~` ranges) — install with `--save-exact`.
- Minimal automated test coverage is the explicit priority for this work — implementation speed over new coverage. Existing tests must stay green; do not add new test files beyond what each task below specifies.
- A live browser walkthrough of all three views, including switching projects via the dropdown, is mandatory before this work is considered done (Task 7) — not optional, regardless of the reduced automated-test scope.
- Every commit message ends with this exact footer:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
  ```

---

### Task 1: Tailwind CSS setup + shared status-pill helper

**Files:**
- Modify: `web/package.json`
- Create: `web/.postcssrc.json`
- Modify: `web/src/styles.css`
- Modify: `web/src/index.html`
- Create: `web/src/app/core/status-style.ts`
- Test: `web/src/app/core/status-style.spec.ts`

**Interfaces:**
- Produces: `statusPillClass(status: string): string` — returns one of `'bg-status-done'`, `'bg-status-progress'`, `'bg-status-planned'`. Consumed by Tasks 4, 5, 6.
- Produces: Tailwind theme color tokens usable as utility-class suffixes everywhere from Task 3 onward: `bg`, `surface`, `surface-hover`, `border`, `text-primary`, `text-secondary`, `accent`, `accent-hover`, `status-done`, `status-progress`, `status-planned`, `status-error` (e.g. `bg-bg`, `text-text-primary`, `border-border`, `bg-accent`, `hover:bg-accent-hover`).

- [ ] **Step 1: Write the failing test for `statusPillClass`**

Create `web/src/app/core/status-style.spec.ts`:

```typescript
import { statusPillClass } from './status-style';

describe('statusPillClass', () => {
  it('maps a done-style status to the done pill class', () => {
    expect(statusPillClass('Shot')).toBe('bg-status-done');
    expect(statusPillClass('Done')).toBe('bg-status-done');
  });

  it('maps an in-progress status to the progress pill class', () => {
    expect(statusPillClass('In Progress')).toBe('bg-status-progress');
  });

  it('falls back to the planned pill class for anything else', () => {
    expect(statusPillClass('Planned')).toBe('bg-status-planned');
    expect(statusPillClass('Something unexpected')).toBe('bg-status-planned');
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd web && npx ng test --watch=false --include='**/status-style.spec.ts'`
Expected: FAIL — `Cannot find module './status-style'` (the file doesn't exist yet).

- [ ] **Step 3: Implement `statusPillClass`**

Create `web/src/app/core/status-style.ts`:

```typescript
const DONE_STATUSES = new Set(['Shot', 'Done', 'Complete', 'Completed']);
const PROGRESS_STATUSES = new Set(['In Progress', 'Editing', 'Reviewing']);

export function statusPillClass(status: string): string {
  if (DONE_STATUSES.has(status)) return 'bg-status-done';
  if (PROGRESS_STATUSES.has(status)) return 'bg-status-progress';
  return 'bg-status-planned';
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd web && npx ng test --watch=false --include='**/status-style.spec.ts'`
Expected: PASS (3 specs).

- [ ] **Step 5: Install Tailwind CSS**

Run from `web/`:

```bash
npm install -D tailwindcss@latest @tailwindcss/postcss@latest postcss@latest --save-exact
```

Confirm `package.json`'s `devDependencies` now lists exact (no `^`/`~`) versions for `tailwindcss`, `@tailwindcss/postcss`, and `postcss`. Record the exact versions installed in your task report.

- [ ] **Step 6: Wire Tailwind into the Angular PostCSS pipeline**

Create `web/.postcssrc.json`:

```json
{
  "plugins": {
    "@tailwindcss/postcss": {}
  }
}
```

- [ ] **Step 7: Replace global styles with the Tailwind import and design tokens**

Replace the full contents of `web/src/styles.css` with:

```css
@import "tailwindcss";

@theme {
  --color-bg: #0b0f19;
  --color-surface: #1a2233;
  --color-surface-hover: #232d42;
  --color-border: #2a3448;
  --color-text-primary: #e5e7eb;
  --color-text-secondary: #94a3b8;
  --color-accent: #6366f1;
  --color-accent-hover: #818cf8;
  --color-status-done: #10b981;
  --color-status-progress: #f59e0b;
  --color-status-planned: #64748b;
  --color-status-error: #f43f5e;
  --font-sans: 'Inter', -apple-system, 'Segoe UI', sans-serif;
}

body {
  @apply m-0 bg-bg text-text-primary font-sans antialiased;
}
```

- [ ] **Step 8: Load the Inter font and set the page title**

In `web/src/index.html`, replace the `<head>` contents:

```html
<head>
  <meta charset="utf-8">
  <title>ProductionBible</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
</head>
```

- [ ] **Step 9: Verify the build picks up Tailwind**

Run: `cd web && npx ng build`
Expected: build succeeds. Then check the built CSS contains a Tailwind-generated rule:

```bash
grep -o "\.antialiased{[^}]*}" dist/web/browser/*.css
```

Expected: a match is found (proves Tailwind's utility generation and the `.postcssrc.json` wiring both work). If the build fails or no match is found, do not proceed to later tasks — the whole visual system depends on this step.

- [ ] **Step 10: Commit**

```bash
git add web/package.json web/package-lock.json web/.postcssrc.json web/src/styles.css web/src/index.html web/src/app/core/status-style.ts web/src/app/core/status-style.spec.ts
git commit -m "$(cat <<'EOF'
Add Tailwind CSS and a shared status-pill style helper

Foundation for the UI redesign: dark theme tokens via Tailwind's
@theme, Inter font, and a pure statusPillClass() helper the Bible,
Timeline, and Production Plan restyles will all use for consistent
status-pill coloring.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 2: `ProjectContextService` — shared project selection state

**Files:**
- Create: `web/src/app/core/project-context.service.ts`

**Interfaces:**
- Consumes: `ApiClientService.getProjects(): Observable<ProjectDto[]>` (existing, `web/src/app/core/api-client.service.ts`).
- Produces (consumed by Tasks 3, 4, 6): `ProjectContextService`, `providedIn: 'root'`, with:
  - `projects: Signal<ProjectDto[]>`
  - `selectedProjectId: Signal<number | null>`
  - `selectProject(id: number): void`

No automated test is required for this task (per Global Constraints — minimal test coverage is the priority here). Verify by build success; this service is exercised end-to-end by the mandatory browser walkthrough in Task 7.

- [ ] **Step 1: Implement the service**

Create `web/src/app/core/project-context.service.ts`:

```typescript
import { Injectable, signal } from '@angular/core';
import { ApiClientService } from './api-client.service';
import { ProjectDto } from './models';

const STORAGE_KEY = 'pb.selectedProjectId';

@Injectable({ providedIn: 'root' })
export class ProjectContextService {
  private readonly projectsSignal = signal<ProjectDto[]>([]);
  private readonly selectedProjectIdSignal = signal<number | null>(null);

  readonly projects = this.projectsSignal.asReadonly();
  readonly selectedProjectId = this.selectedProjectIdSignal.asReadonly();

  constructor(private readonly api: ApiClientService) {
    this.api.getProjects().subscribe((projects) => {
      this.projectsSignal.set(projects);
      if (projects.length === 0) return;

      const storedId = this.readStoredId();
      const match = storedId !== null && projects.some((p) => p.id === storedId);
      this.selectedProjectIdSignal.set(match ? storedId! : projects[0].id);
    });
  }

  selectProject(id: number): void {
    this.selectedProjectIdSignal.set(id);
    try {
      localStorage.setItem(STORAGE_KEY, String(id));
    } catch {
      // localStorage unavailable (e.g. private browsing) - selection still works for this session.
    }
  }

  private readStoredId(): number | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw === null ? null : Number(raw);
    } catch {
      return null;
    }
  }
}
```

- [ ] **Step 2: Verify it compiles**

Run: `cd web && npx ng build`
Expected: build succeeds (this service isn't consumed by anything yet, so this only checks for syntax/type errors).

- [ ] **Step 3: Commit**

```bash
git add web/src/app/core/project-context.service.ts
git commit -m "$(cat <<'EOF'
Add ProjectContextService as the single source of truth for project selection

Replaces the pattern (duplicated in BibleComponent and
ProductionPlanComponent) of each view independently calling
getProjects() and taking projects[0]. Persists the selection to
localStorage so it survives a reload.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 3: App shell — navigation bar, wordmark, tabs, project dropdown

**Files:**
- Modify: `web/src/app/app.ts`
- Modify: `web/src/app/app.html`
- Test: `web/src/app/app.spec.ts`

**Interfaces:**
- Consumes: `ProjectContextService` (Task 2) — `projects()`, `selectedProjectId()`, `selectProject(id)`. Tailwind tokens (Task 1).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Update the failing test first**

Replace the full contents of `web/src/app/app.spec.ts`:

```typescript
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
    expect(compiled.textContent).toContain('Bible');
    expect(compiled.textContent).toContain('Production Plan');
    expect(compiled.textContent).toContain('HalfNut ELS');
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd web && npx ng test --watch=false --include='**/app.spec.ts'`
Expected: FAIL — `App`'s constructor doesn't yet accept/use `ProjectContextService`, so the stub provider is unused and `compiled.textContent` won't contain `'HalfNut ELS'` (the current template has no project name anywhere).

- [ ] **Step 3: Implement the App component**

Replace the full contents of `web/src/app/app.ts`:

```typescript
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
```

- [ ] **Step 4: Implement the shell template**

Replace the full contents of `web/src/app/app.html`:

```html
<div class="min-h-screen bg-bg text-text-primary">
  <header class="border-b border-border bg-surface">
    <div class="mx-auto flex h-14 max-w-7xl items-center justify-between px-4">
      <div class="flex items-center gap-8">
        <span class="text-lg font-semibold tracking-tight">ProductionBible</span>
        <nav class="hidden md:flex items-center gap-1">
          <a routerLink="/bible" routerLinkActive="bg-accent text-white"
             class="rounded-md px-3 py-1.5 text-sm font-medium text-text-secondary hover:bg-surface-hover hover:text-text-primary">Bible</a>
          <a routerLink="/production-plan" routerLinkActive="bg-accent text-white"
             class="rounded-md px-3 py-1.5 text-sm font-medium text-text-secondary hover:bg-surface-hover hover:text-text-primary">Production Plan</a>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <div class="relative">
          <button type="button" (click)="toggleProjectMenu()"
                  class="flex items-center gap-2 rounded-md border border-border bg-surface px-3 py-1.5 text-sm font-medium hover:bg-surface-hover">
            {{ currentProjectName() }}
            <span class="text-text-secondary">&#9662;</span>
          </button>
          <ul *ngIf="projectMenuOpen"
              class="absolute right-0 z-10 mt-1 w-56 rounded-md border border-border bg-surface py-1 shadow-lg">
            <li *ngFor="let project of projectContext.projects()">
              <button type="button" (click)="choose(project.id)"
                      class="block w-full px-3 py-1.5 text-left text-sm hover:bg-surface-hover"
                      [class.text-accent]="project.id === projectContext.selectedProjectId()">
                {{ project.name }}
              </button>
            </li>
          </ul>
        </div>

        <button type="button" (click)="toggleMobileNav()"
                class="rounded-md border border-border p-1.5 md:hidden" aria-label="Toggle navigation">
          &#9776;
        </button>
      </div>
    </div>

    <nav *ngIf="mobileNavOpen" class="flex flex-col gap-1 border-t border-border px-4 py-2 md:hidden">
      <a routerLink="/bible" (click)="toggleMobileNav()"
         class="rounded-md px-3 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover">Bible</a>
      <a routerLink="/production-plan" (click)="toggleMobileNav()"
         class="rounded-md px-3 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover">Production Plan</a>
    </nav>
  </header>

  <main class="mx-auto max-w-7xl px-4 py-6">
    <router-outlet></router-outlet>
  </main>
</div>
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `cd web && npx ng test --watch=false --include='**/app.spec.ts'`
Expected: PASS (2 specs).

- [ ] **Step 6: Run the full test suite to confirm no other regressions**

Run: `cd web && npx ng test --watch=false`
Expected: all suites pass except `BibleComponent`/`ProductionPlanComponent` tests, which are expected to already be broken here since those components don't yet depend on `ProjectContextService` — Tasks 4 and 6 fix those. If `App`'s own suite and everything unrelated to Bible/Production Plan pass, proceed.

- [ ] **Step 7: Commit**

```bash
git add web/src/app/app.ts web/src/app/app.html web/src/app/app.spec.ts
git commit -m "$(cat <<'EOF'
Restyle the app shell with a nav bar and project dropdown

Top bar: wordmark, Bible/Production Plan tabs with active-route
highlighting, and a project dropdown backed by ProjectContextService.
Collapses to a hamburger menu below md breakpoint.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 4: Bible view — consume `ProjectContextService` + restyle

**Files:**
- Modify: `web/src/app/bible/bible.component.ts`
- Modify: `web/src/app/bible/bible.component.html`
- Test: `web/src/app/bible/bible.component.spec.ts`

**Interfaces:**
- Consumes: `ProjectContextService` (Task 2), `statusPillClass` (Task 1).
- Produces: `BibleComponent.setViewMode(mode: 'list' | 'timeline'): void` — replaces the old `toggleTimeline()` (removed; no other task depends on the old name).

- [ ] **Step 1: Update the test first**

Replace the full contents of `web/src/app/bible/bible.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { BibleComponent } from './bible.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, BeatDto, EpisodeDto, ProjectDto } from '../core/models';

describe('BibleComponent', () => {
  let fixture: ComponentFixture<BibleComponent>;
  let component: BibleComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const beat: BeatDto = { id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open', assetIds: [1000] };
  const linkedAsset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, completedAtUtc: null, attributes: {}, beatIds: [100],
  };
  const unlinkedAsset: AssetDto = { ...linkedAsset, id: 1001, code: 'A-02', beatIds: [] };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'getBeats', 'getAssets', 'updateAsset']);
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getBeats.and.returnValue(of([beat]));
    apiSpy.getAssets.and.returnValue(of([linkedAsset, unlinkedAsset]));

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [BibleComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BibleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('loads the first episode of the selected project on construction', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(component.beats).toEqual([beat]);
  });

  it('groups assets under the beat that links to them', () => {
    expect(component.assetsForBeat(beat)).toEqual([linkedAsset]);
  });

  it('lists assets with no beat link as unassigned', () => {
    expect(component.unassignedAssets).toEqual([unlinkedAsset]);
  });

  it('sets the view mode via setViewMode', () => {
    expect(component.viewMode).toBe('list');
    component.setViewMode('timeline');
    expect(component.viewMode).toBe('timeline');
    component.setViewMode('list');
    expect(component.viewMode).toBe('list');
  });

  it('saves inline edits to status and notes via updateAsset', () => {
    const updated: AssetDto = { ...linkedAsset, status: 'Shot', notes: 'Went well' };
    apiSpy.updateAsset.and.returnValue(of(updated));

    component.startEdit(linkedAsset);
    component.editStatus = 'Shot';
    component.editNotes = 'Went well';
    component.saveEdit(linkedAsset);

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(linkedAsset.id, jasmine.objectContaining({
      status: 'Shot',
      notes: 'Went well',
      code: linkedAsset.code,
      beatIds: linkedAsset.beatIds,
    }));
    expect(component.assets.find((a) => a.id === linkedAsset.id)?.status).toBe('Shot');
    expect(component.editingAssetId).toBeNull();
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd web && npx ng test --watch=false --include='**/bible.component.spec.ts'`
Expected: FAIL to compile/run — `BibleComponent`'s constructor doesn't accept `ProjectContextService` yet, and `setViewMode` doesn't exist.

- [ ] **Step 3: Implement the component changes**

Replace the full contents of `web/src/app/bible/bible.component.ts`:

```typescript
import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, BeatDto, EpisodeDto, UpdateAssetRequest } from '../core/models';
import { statusPillClass } from '../core/status-style';
import { TimelineViewComponent } from './timeline-view.component';

@Component({
  selector: 'app-bible',
  standalone: true,
  imports: [CommonModule, FormsModule, TimelineViewComponent],
  templateUrl: './bible.component.html',
})
export class BibleComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];
  assets: AssetDto[] = [];
  viewMode: 'list' | 'timeline' = 'list';
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadEpisodes(projectId);
      }
    });
  }

  setViewMode(mode: 'list' | 'timeline'): void {
    this.viewMode = mode;
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.api.getBeats(episodeId).subscribe((beats) => {
      this.beats = beats;
      this.cdr.markForCheck();
    });
    this.api.getAssets(episodeId).subscribe((assets) => {
      this.assets = assets;
      this.cdr.markForCheck();
    });
  }

  assetsForBeat(beat: BeatDto): AssetDto[] {
    return this.assets.filter((asset) => beat.assetIds.includes(asset.id));
  }

  get unassignedAssets(): AssetDto[] {
    const linkedIds = new Set(this.beats.flatMap((beat) => beat.assetIds));
    return this.assets.filter((asset) => !linkedIds.has(asset.id));
  }

  editingAssetId: number | null = null;
  editStatus = '';
  editNotes = '';

  startEdit(asset: AssetDto): void {
    this.editingAssetId = asset.id;
    this.editStatus = asset.status;
    this.editNotes = asset.notes ?? '';
  }

  cancelEdit(): void {
    this.editingAssetId = null;
  }

  saveEdit(asset: AssetDto): void {
    const request: UpdateAssetRequest = {
      assetTypeId: asset.assetTypeId,
      code: asset.code,
      title: asset.title,
      scriptText: asset.scriptText,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: asset.sequenceNumber,
      targetLengthSeconds: asset.targetLengthSeconds,
      attributes: asset.attributes,
      beatIds: asset.beatIds,
    };
    this.api.updateAsset(asset.id, request).subscribe((updated) => {
      const index = this.assets.findIndex((a) => a.id === asset.id);
      if (index !== -1) {
        this.assets[index] = updated;
      }
      this.editingAssetId = null;
      this.cdr.markForCheck();
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      this.episodes = episodes;
      if (episodes.length > 0) {
        this.selectEpisode(episodes[0].id);
      } else {
        this.selectedEpisodeId = null;
        this.beats = [];
        this.assets = [];
      }
      this.cdr.markForCheck();
    });
  }
}
```

- [ ] **Step 4: Restyle the template**

Replace the full contents of `web/src/app/bible/bible.component.html`:

```html
<div class="space-y-6">
  <div class="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-border bg-surface p-4">
    <div class="flex items-center gap-3">
      <label for="episode-select" class="text-sm font-medium text-text-secondary">Episode</label>
      <select id="episode-select" [ngModel]="selectedEpisodeId" (ngModelChange)="selectEpisode($event)"
              class="rounded-md border border-border bg-bg px-3 py-1.5 text-sm text-text-primary focus:border-accent focus:outline-none">
        <option *ngFor="let ep of episodes" [ngValue]="ep.id">{{ ep.name }}</option>
      </select>
    </div>

    <div class="inline-flex rounded-md border border-border bg-bg p-1">
      <button type="button" (click)="setViewMode('list')"
              class="rounded px-3 py-1 text-sm font-medium transition-colors"
              [class.bg-accent]="viewMode === 'list'"
              [class.text-white]="viewMode === 'list'"
              [class.text-text-secondary]="viewMode !== 'list'">List</button>
      <button type="button" (click)="setViewMode('timeline')"
              class="rounded px-3 py-1 text-sm font-medium transition-colors"
              [class.bg-accent]="viewMode === 'timeline'"
              [class.text-white]="viewMode === 'timeline'"
              [class.text-text-secondary]="viewMode !== 'timeline'">Timeline</button>
    </div>
  </div>

  <ng-container *ngIf="viewMode === 'list'; else timelineTpl">
    <ng-container *ngFor="let beat of beats">
      <section class="rounded-lg border border-border bg-surface p-4">
        <h3 class="mb-3 text-sm font-semibold text-text-secondary">{{ beat.timecode }} &mdash; {{ beat.purpose }}</h3>
        <ul class="space-y-2">
          <li *ngFor="let asset of assetsForBeat(beat)" class="rounded-md border border-border bg-bg p-3">
            <div class="flex flex-wrap items-center gap-2">
              <span class="font-mono text-sm text-text-secondary">{{ asset.code }}</span>
              <span class="font-medium">{{ asset.title }}</span>
              <span class="text-xs text-text-secondary">({{ asset.assetTypeName }})</span>
              <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
                {{ asset.status }}
              </span>
              <button type="button" *ngIf="editingAssetId !== asset.id" (click)="startEdit(asset)"
                      class="ml-auto rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
            </div>
            <p *ngIf="asset.notes" class="mt-1 text-sm text-text-secondary">{{ asset.notes }}</p>

            <div *ngIf="editingAssetId === asset.id" class="mt-3 space-y-2 border-t border-border pt-3">
              <label class="block text-sm">
                <span class="mb-1 block text-text-secondary">Status</span>
                <input type="text" [(ngModel)]="editStatus"
                       class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
              </label>
              <label class="block text-sm">
                <span class="mb-1 block text-text-secondary">Notes</span>
                <textarea [(ngModel)]="editNotes"
                          class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none"></textarea>
              </label>
              <div class="flex gap-2">
                <button type="button" (click)="saveEdit(asset)"
                        class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Save</button>
                <button type="button" (click)="cancelEdit()"
                        class="rounded-md border border-border px-3 py-1.5 text-sm font-medium hover:bg-surface-hover">Cancel</button>
              </div>
            </div>
          </li>
        </ul>
      </section>
    </ng-container>

    <section class="rounded-lg border border-border bg-surface p-4" *ngIf="unassignedAssets.length > 0">
      <h3 class="mb-3 text-sm font-semibold text-text-secondary">Unassigned</h3>
      <ul class="space-y-2">
        <li *ngFor="let asset of unassignedAssets" class="rounded-md border border-border bg-bg p-3">
          <div class="flex flex-wrap items-center gap-2">
            <span class="font-mono text-sm text-text-secondary">{{ asset.code }}</span>
            <span class="font-medium">{{ asset.title }}</span>
            <span class="text-xs text-text-secondary">({{ asset.assetTypeName }})</span>
            <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
              {{ asset.status }}
            </span>
            <button type="button" *ngIf="editingAssetId !== asset.id" (click)="startEdit(asset)"
                    class="ml-auto rounded-md border border-border px-2 py-1 text-xs font-medium hover:bg-surface-hover">Edit</button>
          </div>
          <p *ngIf="asset.notes" class="mt-1 text-sm text-text-secondary">{{ asset.notes }}</p>

          <div *ngIf="editingAssetId === asset.id" class="mt-3 space-y-2 border-t border-border pt-3">
            <label class="block text-sm">
              <span class="mb-1 block text-text-secondary">Status</span>
              <input type="text" [(ngModel)]="editStatus"
                     class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none" />
            </label>
            <label class="block text-sm">
              <span class="mb-1 block text-text-secondary">Notes</span>
              <textarea [(ngModel)]="editNotes"
                        class="w-full rounded-md border border-border bg-surface px-3 py-1.5 text-sm focus:border-accent focus:outline-none"></textarea>
            </label>
            <div class="flex gap-2">
              <button type="button" (click)="saveEdit(asset)"
                      class="rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white hover:bg-accent-hover">Save</button>
              <button type="button" (click)="cancelEdit()"
                      class="rounded-md border border-border px-3 py-1.5 text-sm font-medium hover:bg-surface-hover">Cancel</button>
            </div>
          </div>
        </li>
      </ul>
    </section>
  </ng-container>
  <ng-template #timelineTpl>
    <app-timeline-view [beats]="beats" [assets]="assets"></app-timeline-view>
  </ng-template>
</div>
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `cd web && npx ng test --watch=false --include='**/bible.component.spec.ts'`
Expected: PASS (5 specs).

- [ ] **Step 6: Commit**

```bash
git add web/src/app/bible/bible.component.ts web/src/app/bible/bible.component.html web/src/app/bible/bible.component.spec.ts
git commit -m "$(cat <<'EOF'
Bible view: consume ProjectContextService and apply the dark theme

Episode loading now reacts to the shared selected-project signal
instead of BibleComponent fetching its own project list. Restyled
beat/asset lists as cards with status pills; the List/Timeline toggle
becomes a proper segmented control via the new setViewMode() method.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 5: Timeline view restyle

**Files:**
- Modify: `web/src/app/bible/timeline-view.component.ts`
- Modify: `web/src/app/bible/timeline-view.component.html`

**Interfaces:**
- Consumes: `statusPillClass` (Task 1).
- Produces: nothing new — this is a pure visual change, `@Input()`s and `lanes` are unchanged.

No test changes are needed: `timeline-view.component.spec.ts` only asserts `component.lanes` grouping logic, which this task does not touch.

- [ ] **Step 1: Add the status-pill helper to the component**

In `web/src/app/bible/timeline-view.component.ts`, add the import and field. The full file becomes:

```typescript
import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetDto, BeatDto } from '../core/models';
import { statusPillClass } from '../core/status-style';

interface TimelineLane {
  name: string;
  assets: AssetDto[];
}

const LANE_BY_ASSET_TYPE: Record<string, string> = {
  PieceToCamera: 'A-Roll',
  Shot: 'B-Roll',
  Animation: 'Animations',
  Title: 'Titles',
};

const LANE_ORDER = ['A-Roll', 'B-Roll', 'Animations', 'Titles', 'Other'];

@Component({
  selector: 'app-timeline-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './timeline-view.component.html',
})
export class TimelineViewComponent implements OnChanges {
  @Input() beats: BeatDto[] = [];
  @Input() assets: AssetDto[] = [];

  lanes: TimelineLane[] = [];
  protected readonly statusPillClass = statusPillClass;

  ngOnChanges(): void {
    this.lanes = this.buildLanes();
  }

  private buildLanes(): TimelineLane[] {
    const orderedAssetIds = this.beats.flatMap((beat) => beat.assetIds);
    const orderIndex = new Map(orderedAssetIds.map((id, index) => [id, index]));

    const grouped = new Map<string, AssetDto[]>();
    for (const asset of this.assets) {
      const laneName = LANE_BY_ASSET_TYPE[asset.assetTypeName] ?? 'Other';
      if (!grouped.has(laneName)) grouped.set(laneName, []);
      grouped.get(laneName)!.push(asset);
    }

    for (const list of grouped.values()) {
      list.sort((a, b) => {
        const aIndex = orderIndex.has(a.id) ? orderIndex.get(a.id)! : Number.MAX_SAFE_INTEGER;
        const bIndex = orderIndex.has(b.id) ? orderIndex.get(b.id)! : Number.MAX_SAFE_INTEGER;
        return aIndex - bIndex;
      });
    }

    return LANE_ORDER.filter((name) => grouped.has(name)).map((name) => ({
      name,
      assets: grouped.get(name)!,
    }));
  }
}
```

- [ ] **Step 2: Restyle the template**

Replace the full contents of `web/src/app/bible/timeline-view.component.html`:

```html
<div class="space-y-4">
  <div class="rounded-lg border border-border bg-surface p-4" *ngFor="let lane of lanes">
    <h4 class="mb-3 text-sm font-semibold text-text-secondary">{{ lane.name }}</h4>
    <div class="flex flex-wrap gap-2">
      <div class="flex min-w-40 flex-col gap-1 rounded-md border border-border bg-bg p-3" *ngFor="let asset of lane.assets">
        <div class="flex items-center gap-2">
          <span class="font-mono text-xs text-text-secondary">{{ asset.code }}</span>
          <span class="rounded-full px-2 py-0.5 text-[10px] font-medium text-white" [ngClass]="statusPillClass(asset.status)">
            {{ asset.status }}
          </span>
        </div>
        <span class="text-sm font-medium">{{ asset.title }}</span>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Run the existing test to confirm no regression**

Run: `cd web && npx ng test --watch=false --include='**/timeline-view.component.spec.ts'`
Expected: PASS (4 specs, unchanged).

- [ ] **Step 4: Commit**

```bash
git add web/src/app/bible/timeline-view.component.ts web/src/app/bible/timeline-view.component.html
git commit -m "$(cat <<'EOF'
Restyle the timeline view with status pills and the dark theme

Visual-only change: lanes and their asset ordering are untouched.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 6: Production Plan view — consume `ProjectContextService` + restyle

**Files:**
- Create: `web/src/app/production-plan/phase-grouping.ts`
- Modify: `web/src/app/production-plan/production-plan.component.ts`
- Modify: `web/src/app/production-plan/production-plan.component.html`
- Test: `web/src/app/production-plan/production-plan.component.spec.ts`

**Interfaces:**
- Consumes: `ProjectContextService` (Task 2), `statusPillClass` (Task 1).
- Produces: `buildPhaseGroups(assets: AssetDto[]): PhaseGroup[]` (pure function) — not consumed elsewhere in this plan, extracted so the phase-grouping logic is testable without Angular's DI/effect machinery.

- [ ] **Step 1: Write the failing test for the extracted pure function, and update the component spec**

Replace the full contents of `web/src/app/production-plan/production-plan.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { ProductionPlanComponent } from './production-plan.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, EpisodeDto, ProjectDto } from '../core/models';
import { buildPhaseGroups } from './phase-grouping';

describe('buildPhaseGroups', () => {
  function asset(id: number, code: string, sequenceNumber: number, phase: string | undefined): AssetDto {
    return {
      id, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
      completedAtUtc: null, attributes: phase ? { PhaseGroup: phase } : {}, beatIds: [],
    };
  }

  it('merges assets from every episode in the project', () => {
    const groups = buildPhaseGroups([
      asset(2, 'F-01', 1, 'Phase 1: The software time machine'),
      asset(1, 'B-02', 2, 'Phase 2: Makerspace trip'),
    ]);
    const allCodes = groups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });

  it('orders phase groups by the lowest sequence number in that phase', () => {
    const groups = buildPhaseGroups([
      asset(2, 'F-01', 1, 'Phase 1: The software time machine'),
      asset(1, 'B-02', 2, 'Phase 2: Makerspace trip'),
    ]);
    expect(groups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('falls back to Unphased when an asset has no PhaseGroup attribute', () => {
    const groups = buildPhaseGroups([asset(3, 'X-01', 1, undefined)]);
    expect(groups.some((g) => g.phase === 'Unphased')).toBeTrue();
  });
});

describe('ProductionPlanComponent', () => {
  let fixture: ComponentFixture<ProductionPlanComponent>;
  let component: ProductionPlanComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'getAssets']);
    apiSpy.getEpisodes.and.returnValue(of([episode1]));
    apiSpy.getAssets.and.returnValue(of([{
      id: 1, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'B-02', title: 'B-02',
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: 1, targetLengthSeconds: null,
      completedAtUtc: null, attributes: { PhaseGroup: 'Phase 2: Makerspace trip' }, beatIds: [],
    }]));

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [ProductionPlanComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductionPlanComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('loads phase groups for the selected project on construction', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.groups.map((g) => g.phase)).toEqual(['Phase 2: Makerspace trip']);
  });
});
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd web && npx ng test --watch=false --include='**/production-plan.component.spec.ts'`
Expected: FAIL — `./phase-grouping` doesn't exist yet, and `ProductionPlanComponent` doesn't yet accept `ProjectContextService`.

- [ ] **Step 3: Implement the extracted pure function**

Create `web/src/app/production-plan/phase-grouping.ts`:

```typescript
import { AssetDto } from '../core/models';

export interface PhaseGroup {
  phase: string;
  assets: AssetDto[];
}

export function buildPhaseGroups(assets: AssetDto[]): PhaseGroup[] {
  const sorted = [...assets].sort(
    (a, b) => (a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) - (b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

  const map = new Map<string, AssetDto[]>();
  for (const asset of sorted) {
    const phase = asset.attributes['PhaseGroup'] ?? 'Unphased';
    if (!map.has(phase)) map.set(phase, []);
    map.get(phase)!.push(asset);
  }

  return Array.from(map.entries()).map(([phase, assets]) => ({ phase, assets }));
}
```

- [ ] **Step 4: Update the component**

Replace the full contents of `web/src/app/production-plan/production-plan.component.ts`:

```typescript
import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { statusPillClass } from '../core/status-style';
import { buildPhaseGroups, PhaseGroup } from './phase-grouping';

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent {
  groups: PhaseGroup[] = [];
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadGroups(projectId);
      }
    });
  }

  private loadGroups(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (episodes.length === 0) {
        this.groups = [];
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
        this.groups = buildPhaseGroups(assetLists.flat());
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
```

- [ ] **Step 5: Restyle the template**

Replace the full contents of `web/src/app/production-plan/production-plan.component.html`:

```html
<div class="space-y-6">
  <section class="rounded-lg border border-border bg-surface" *ngFor="let group of groups">
    <h3 class="sticky top-0 rounded-t-lg border-b border-border bg-surface px-4 py-3 text-sm font-semibold text-text-secondary">
      {{ group.phase }}
    </h3>
    <div class="overflow-x-auto">
      <table class="w-full text-left text-sm">
        <thead>
          <tr class="border-b border-border text-xs uppercase tracking-wide text-text-secondary">
            <th class="px-4 py-2">Seq</th>
            <th class="px-4 py-2">Code</th>
            <th class="px-4 py-2">Title</th>
            <th class="px-4 py-2">Location</th>
            <th class="px-4 py-2">Angle &amp; camera</th>
            <th class="px-4 py-2">Audio</th>
            <th class="px-4 py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let asset of group.assets" class="border-b border-border last:border-0 hover:bg-surface-hover">
            <td class="px-4 py-2 text-text-secondary">{{ asset.sequenceNumber }}</td>
            <td class="px-4 py-2 font-mono">{{ asset.code }}</td>
            <td class="px-4 py-2 font-medium">{{ asset.title }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['Location'] }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AngleAndCamera'] }}</td>
            <td class="px-4 py-2 text-text-secondary">{{ asset.attributes['AudioNotes'] }}</td>
            <td class="px-4 py-2">
              <span class="rounded-full px-2 py-0.5 text-xs font-medium text-white" [ngClass]="statusPillClass(asset.status)">
                {{ asset.status }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</div>
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `cd web && npx ng test --watch=false --include='**/production-plan.component.spec.ts'`
Expected: PASS (4 specs).

- [ ] **Step 7: Run the full test suite**

Run: `cd web && npx ng test --watch=false`
Expected: all suites pass now (this is the first point since Task 3 where every suite is green again).

- [ ] **Step 8: Commit**

```bash
git add web/src/app/production-plan/phase-grouping.ts web/src/app/production-plan/production-plan.component.ts web/src/app/production-plan/production-plan.component.html web/src/app/production-plan/production-plan.component.spec.ts
git commit -m "$(cat <<'EOF'
Production Plan view: consume ProjectContextService and apply the dark theme

Extracts phase-grouping logic into a pure buildPhaseGroups() function
so it's testable without Angular's effect/DI machinery. Restyled as
bordered panels with sticky phase headers and status pills in the
table's Status column.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

---

### Task 7: Mandatory live browser verification (multi-project + full walkthrough)

**Files:** none expected — this task exists to catch what automated tests cannot (Phase 1's frozen-UI regression was invisible to 56 passing tests and was only caught this way). Any file changed here is a fix for something this walkthrough finds.

**Interfaces:** none — this task consumes the finished app as a whole.

The dev database currently has exactly one project ("HalfNut ELS"), so the project dropdown can't be exercised with only the seeded data. This task creates a second, throwaway project via the API for the sole purpose of testing the dropdown, then deletes it.

- [ ] **Step 1: Start the API**

Run in its own terminal/background process (per this repo's own established gotcha: never bundle `dotnet run` with other commands in one shell call, or the wrapping shell can reap the background process):

```bash
cd D:\Data\source\ProductionBible
dotnet run --project src/ProductionBible.Api
```

Wait until it logs that it's listening on `http://0.0.0.0:5280`.

- [ ] **Step 2: Create a throwaway second project**

In a separate terminal:

```bash
curl -X POST http://localhost:5280/api/projects -H "Content-Type: application/json" -d "{\"name\":\"UI Redesign Test Project\",\"description\":null}"
```

Record the `id` from the response — call it `<test-project-id>`.

- [ ] **Step 3: Serve the frontend and open it in a browser**

```bash
cd D:\Data\source\ProductionBible\web
npm start
```

Open `http://localhost:4200` in a browser (use the Chrome automation tools if available; otherwise a manual check with a screenshot is acceptable, but it must be an actual rendered page, not just `curl`/HTML source inspection).

- [ ] **Step 4: Verify the shell and Bible view**

Confirm: dark theme renders (no unstyled/black-and-white "1996" look), nav bar shows "ProductionBible", "Bible" and "Production Plan" tabs with the active one highlighted, and the project dropdown shows "HalfNut ELS" by default. Click into the Bible view: episode picker works, beats render as cards, status pills show colors, clicking "Edit" on an asset shows the styled inline form, "Save" persists a change (visible immediately, no manual refresh needed — this is the exact class of bug Phase 1's Task 16 found).

- [ ] **Step 5: Verify the Timeline toggle**

Click the "Timeline" segmented-control button. Confirm lanes render (A-Roll/B-Roll/Animations/Titles/Other, whichever have assets) with status-colored pills. Click back to "List" and confirm it returns correctly.

- [ ] **Step 6: Verify the project dropdown actually switches data**

Click the project dropdown in the nav bar. Confirm both "HalfNut ELS" and "UI Redesign Test Project" appear. Select "UI Redesign Test Project". Confirm the Bible view updates to reflect the new (empty) project — no episodes, no stale data left over from "HalfNut ELS". Switch back to "HalfNut ELS" and confirm its data reloads correctly. Reload the browser page entirely and confirm the previously-selected project ("HalfNut ELS") is remembered (localStorage persistence).

- [ ] **Step 7: Verify the Production Plan view**

Click the "Production Plan" tab. Confirm phase groups render as styled panels with sticky headers, the table is readable, and status pills render in the Status column.

- [ ] **Step 8: Verify responsive collapse**

Resize the browser window (or use the Chrome tool's device emulation) to below 768px width. Confirm the "Bible"/"Production Plan" tabs disappear from the top bar and a hamburger button appears; clicking it reveals the same links in a dropdown panel. Confirm the project dropdown remains visible and usable at this width.

- [ ] **Step 9: Fix anything broken**

If any step above fails, fix it now — this task isn't complete until the whole walkthrough passes. Common risk areas based on this app's history: change detection not firing after an async load (add the missing `markForCheck()` — see Global Constraints), and Tailwind classes not being generated (check `.postcssrc.json` and that `npm start`'s dev server picked up the config; a full restart of `ng serve` is sometimes required after a Tailwind config change).

- [ ] **Step 10: Clean up the throwaway project**

```bash
curl -X DELETE http://localhost:5280/api/projects/<test-project-id>
```

Confirm the dropdown, after a refresh, shows only "HalfNut ELS" again.

- [ ] **Step 11: Stop both dev servers**

Stop the `dotnet run` and `npm start` processes.

- [ ] **Step 12: Final full-suite check and commit (if Step 9 required fixes)**

```bash
cd D:\Data\source\ProductionBible\web && npx ng test --watch=false
```

Expected: all suites still pass. If Step 9 changed any files, stage and commit them:

```bash
git add -A
git commit -m "$(cat <<'EOF'
Fix issues found during the mandatory UI redesign browser walkthrough

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01AJCRA8DYyZtpzqCVs7dmvp
EOF
)"
```

If Step 9 required no fixes, no commit is needed for this task — report that the walkthrough passed clean.
