# ProductionBible UI Redesign — Design Spec

## Overview

Phase 1 shipped a functionally complete ProductionBible app (Bible view, Timeline view,
Production Plan view) with zero visual design — plain unstyled HTML, no navigation shell
beyond two bare links, and no concept of "the current project" (every view independently
fetches the project list and silently takes `projects[0]`).

This spec covers a frontend-only redesign: a real navigation shell with a project selector,
a modern dark visual design system built on Tailwind CSS, and a restyle of all three
existing views. **No backend/API changes.** No new views, routes, or features beyond the
project selector itself.

## Current State (verified against the code)

- `src/app/app.ts` / `app.html` / `app.css`: root component is a bare `<nav>` with two
  `routerLink`s and an empty stylesheet. No project-selection UI.
- `src/app/app.routes.ts`: two routes, `bible` and `production-plan`, no project id in
  the URL.
- `src/app/bible/bible.component.ts` and `src/app/production-plan/production-plan.component.ts`
  each call `ApiClientService.getProjects()` independently in `ngOnInit` and use `projects[0]`.
  There is no shared state between them — if one day there are two projects, there is no
  way to pick between them, and the two views could silently disagree about which project
  they show.
- `src/app/bible/timeline-view.component.ts` is a child of `BibleComponent`, driven purely
  by `@Input()`/`ngOnChanges` — it has no direct API calls and needs no changes to its data
  flow, only to its visual styling.
- Styling: none. No CSS framework, no design tokens, no component library.
- Angular version: `22.1.x`, using the `@angular/build:application` esbuild-based builder
  (confirmed in `angular.json`), which has built-in PostCSS support — no separate Angular
  builder config changes are needed to add Tailwind.

## Goals

1. A project selector: a dropdown in the top nav, backed by one shared piece of state, so
   picking a project drives every view consistently.
2. A real navigation shell: wordmark, tab-style links for the three views, project dropdown,
   responsive collapse on narrow widths.
3. A modern dark visual design system (Tailwind CSS + a fresh, non-video-series palette)
   applied consistently across the shell and all three views.
4. No regression to existing functionality (episode/beat/asset browsing, inline editing,
   timeline view, production plan grouping) — this is a visual and navigational layer on
   top of working functionality, not a rewrite of it.

## Non-Goals

- No backend or API changes — `ApiClientService` and the DTOs are unchanged.
- No new routes/views beyond what exists (Bible, Timeline-as-a-mode-of-Bible, Production Plan).
- No light/dark theme toggle — dark only, per the approved design.
- No reuse of the video-series' own brand palette (`storyboard.html`/`anim` toolkit colors)
  — this app gets its own distinct visual identity.
- No multi-tenancy / auth concerns — the project selector is a convenience for browsing
  existing projects, not an access-control boundary.

## Architecture

### `ProjectContextService` (new)

`src/app/core/project-context.service.ts`, `providedIn: 'root'`, signal-based:

- `projects: Signal<ProjectDto[]>` — fetched once on construction via
  `ApiClientService.getProjects()`.
- `selectedProjectId: Signal<number | null>` — initialized from `localStorage`
  (`localStorage.getItem('pb.selectedProjectId')`) if present and still valid once the
  project list loads, otherwise defaults to the first project once loaded (`projects()[0]`,
  matching today's implicit behavior, but now centralized in one place instead of
  duplicated in two components).
- `selectProject(id: number): void` — updates the signal and writes through to
  `localStorage`.

This service is the **single source of truth** for "which project is currently being
viewed." It does not know about episodes, beats, or assets — that stays in the view
components, which react to `selectedProjectId()` changing.

### Consumers

- `App` (root component) injects `ProjectContextService` to render the project dropdown
  (`projects()`, `selectedProjectId()`, `selectProject()`) and the active-route highlighting
  in the nav.
- `BibleComponent` and `ProductionPlanComponent` stop calling `getProjects()` themselves.
  Instead they inject `ProjectContextService` and use an `effect()` that re-fetches episodes
  whenever `selectedProjectId()` changes (including the initial load, and any time the user
  picks a different project from the dropdown). The existing `ChangeDetectorRef.markForCheck()`
  pattern established in Phase 1 (required because this app has no zoneless/OnPush + signal
  auto-rendering wired up for `HttpClient`) is preserved for every subscribe callback that
  mutates component state.

### Data flow for a project switch

1. User picks a project in the nav dropdown → `ProjectContextService.selectProject(id)`.
2. Signal updates, written to `localStorage`.
3. Whichever view is currently active has an `effect()` watching `selectedProjectId()`;
   it re-fetches episodes for the new project and resets any episode/beat/asset selection
   state to the new project's first episode (mirroring today's initial-load behavior).

## Visual Design System

### Tooling

Tailwind CSS, installed as a dev dependency (`tailwindcss`, `@tailwindcss/postcss`, `postcss`)
and wired into the existing esbuild `@angular/build:application` builder via a
`.postcssrc.json` (or equivalent) at the `web/` root — Angular's builder picks up PostCSS
config automatically, no `angular.json` changes needed beyond what the builder already
supports. A `tailwind.config` (or CSS-based `@theme` config, depending on the installed
Tailwind major version at implementation time — the plan should pin an exact version and
follow that version's own setup docs) content-globs `src/**/*.{html,ts}`.

### Palette (dark, fresh — not the video-series brand colors)

| Token | Hex | Use |
|---|---|---|
| `bg` | `#0b0f19` | App background |
| `surface` | `#1a2233` | Panels, cards, nav bar |
| `surface-hover` | `#232d42` | Hover state on interactive surfaces |
| `border` | `#2a3448` | Panel/card borders, dividers |
| `text-primary` | `#e5e7eb` | Primary text |
| `text-secondary` | `#94a3b8` | Secondary/muted text |
| `accent` | `#6366f1` | Primary actions, active nav tab, links |
| `accent-hover` | `#818cf8` | Hover state on accent elements |
| `status-done` | `#10b981` (emerald) | "Done"/complete status pill |
| `status-progress` | `#f59e0b` (amber) | "In progress" status pill |
| `status-planned` | `#64748b` (slate) | "Planned"/default status pill |
| `status-error` | `#f43f5e` (rose) | Error/destructive actions |

These are proposed Tailwind-adjacent values (close to Tailwind's own `indigo`/`emerald`/
`amber`/`rose`/`slate` scales) so the implementer can use Tailwind's built-in palette
directly (e.g. `bg-indigo-500`) rather than hand-rolling custom color tokens, unless the
plan decides a custom theme extension is worth it for consistency. Either approach is
acceptable; the *values* above are what must render, not necessarily custom-named tokens.

### Typography

Inter, loaded from Google Fonts (`fonts.googleapis.com` + `fonts.gstatic.com`), with the
system UI stack (`-apple-system, Segoe UI, sans-serif`) as fallback. Monospace (for codes
like asset `Code`, timecodes) falls back to the system mono stack — no separate mono
webfont needed.

## Navigation Shell

Top bar, full width, `surface` background, bottom `border`:

- Left: wordmark ("ProductionBible" — plain styled text, no logo asset needed; this is an
  internal tool, not the video-series brand).
- Center-left: two top-level tab-style links only — "Bible" and "Production Plan" — using
  `routerLinkActive` for the accent-colored active state. "Timeline" stays exactly what it
  is today, a view-mode toggle inside `BibleComponent` (`viewMode: 'list' | 'timeline'`),
  not a route — promoting it to a real nav tab would require lifting episode/beat/asset
  loading out of `BibleComponent` and into the route table, which is a functional change
  this spec explicitly excludes (Non-Goals: no new routes). Instead, restyle the existing
  `toggleTimeline()` control as a segmented List/Timeline control at the top of the Bible
  view's content area, visually secondary to the top nav.
- Right: project dropdown. Shows the currently selected project's name; opens a list of
  all `projects()` on click; selecting one calls `selectProject(id)`.
- Below ~768px: nav links collapse behind a hamburger toggle; the project dropdown remains
  visible (it's the single most important piece of context).

## Per-View Restyle

### Bible (`bible.component.html`)

- Episode list / beat groups become sectioned panels (`surface` background, `border`,
  rounded corners, padding) instead of bare `<ul>` lists.
- Each asset becomes a card: title, `code` in mono, a colored status pill
  (`status-done`/`status-progress`/`status-planned` mapped from the asset's `status` string
  — the plan should define the exact string-to-token mapping since `status` is free text
  today, not an enum).
- The existing inline edit form (status input, notes textarea, Save/Cancel/Edit buttons,
  added in Phase 1 Task 10) gets real labeled form fields and primary (`accent`) /
  secondary (`surface-hover` outline) button styling — no change to its behavior.

### Timeline (`timeline-view.component.html`)

- Restyled as clean horizontal blocks per beat, using the same status-pill palette as the
  Bible view for visual consistency across the app. This is a styling-only change — the
  component's `@Input()`/`ngOnChanges`-driven data flow is untouched.

### Production Plan (`production-plan.component.html`)

- Same card/panel treatment as Bible, grouped by the existing `PhaseGroup` attribute
  (already read by this component per Phase 1), with sticky group headers so the current
  phase stays visible while scrolling a long list.

## Testing

Minimal automated testing on this piece of work — priority is implementation speed, not
coverage. Keep the existing test suite green (fix any test that breaks because
`BibleComponent`/`ProductionPlanComponent` no longer call `getProjects()` directly), but do
not add new unit-test coverage for `ProjectContextService` or the restyled views as a
required deliverable.

What is **not optional**: a live browser walkthrough of all three views — including
switching projects via the dropdown — before this work is considered done. Styling is not
unit-testable at all, and per the Phase 1 lesson, a fully broken UI (frozen after every
async load) was invisible to 56 passing automated tests and was only caught by manually
using the app in a browser. That check stays mandatory even though new automated coverage
does not.

## Global Constraints (for the implementation plan)

- Frontend-only. Do not touch `src/ProductionBible.Api`, `src/ProductionBible.Application`,
  or `src/ProductionBible.Importer`.
- Preserve the `ChangeDetectorRef.markForCheck()` pattern in every state-mutating
  `.subscribe()` callback (see Phase 1's Task 16 finding — this app has no zoneless/OnPush
  + signal-driven auto-rendering wired up for `HttpClient`-based async updates).
- No new routes beyond resolving the Timeline nav-tab question above.
- Tailwind and any other new dependency versions must be pinned exactly in `package.json`,
  not left as floating ranges, consistent with the rest of this workspace's dependency style.
