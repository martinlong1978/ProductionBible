# HalfNut ELS
## Production Shooting Plan

<p class="coverline">Martin's Maker Space &middot; five-part YouTube series</p>
<p class="coverline">Printed field document &middot; companion to <em>storyboard.html</em>, not a replacement for it</p>
<p class="coverline">Generated 26 August 2026</p>

<div class="cover-note">

**How to use this document**

Every shot in the series has its own page, in the order this document recommends actually shooting them — grouped by physical setup and location, not by episode. Shooting order and story order are deliberately different: this plan exists so you rig once per setup instead of once per beat.

Each shot page has the same six fields — Location, Setup, Angle & camera, Audio to capture, Target length, and Script (where one exists) — plus a blank **Production notes** box for what actually happened on the day: which take worked, what you'd do differently, anything that needs a pickup.

A few pages carry an explicit **prerequisite** flag — a relief groove that needs cutting before a later shot, a part that needs to be on hand before a session starts, a firmware branch that needs building. Read the whole day's pages before you start rigging, not just the next one.

Two things this document does not replace: the full creative rationale for each beat (that's `storyboard.html`), and the pre-shoot checklists in CLAUDE.md (encoder bench test, part orders, branch builds).

</div>

<div class="pagebreak"></div>

## Shooting order, at a glance

| Phase | What | Shots | Where |
|---|---|---|---|
| 1 | The software time machine | F-01 | Home, at the lathe |
| 2 | Makerspace trip | B-02, B-04 | Local makerspace — **book ahead** |
| 3 | Bench day | C-01 through C-14 (14 shots) | Home workshop bench |
| 4 | Lathe day | A-series, B-series, F-02/F-03/F-04 (31 shots) | Home, at the lathe |
| 5 | Desk afternoon | D-01 through D-09 (9 shots) | Desk, screen only |
| 6 | Pieces to camera, last | E-S, then 7 episode/position sessions (8 pages) | Bench and lathe, wardrobe matched per episode |

**Why this order, in one paragraph:** F-01 needs a firmware downgrade and gates half of Episode 2 twice over, so it happens before anything else can go wrong with the board. The makerspace trip is a separate location entirely and gates Episode 1, so it's scheduled early rather than left to chance. The bench day needs no lathe and can run any time the parts are in hand. The lathe day is the highest-value single day in the whole schedule — Setups A and B together unlock most of Episodes 1, 3 and 5 in one sitting. Screen captures can happen whenever, so they're parked until the physical footage is in the can. Pieces to camera are scripted last on purpose: the bible is explicit that you should shoot them only once you've seen the rushes from everything else and know what still actually needs saying on camera.

**Two things that cannot be picked up later, from the bible — read this before you start:**
- **The bad footage.** Every scrapped thread, every failed pass, every wrong reading — including the ones you didn't plan. It's the most valuable material in the series.
- **Clean machine audio.** A silent repeat pass of every lathe shot, TX at the machine, nobody speaking, 32-bit float. Five cold opens depend on it and it cannot be recreated once everything works correctly.

<div class="pagebreak"></div>

## Phase 1 — The software time machine

<p class="phase-note">One shot, at home, at the lathe. This is the highest-priority single shot in the entire production — the bible calls it "the shot the whole of EP2 rests on," and it gets harder to redo the longer it's left, because it depends on a firmware branch being flashed <em>before</em> anything else touches the board.</p>

<div class="pagebreak"></div>

### F-01 — The software time machine
<p class="meta">Sequence 1 of 65 &nbsp;&middot;&nbsp; Phase 1: The software time machine &nbsp;&middot;&nbsp; EP2 00:00 &middot; 22:30</p>

| | |
|---|---|
| **Location** | Home, at the lathe |
| **Setup** | Setup A (running) for the actual cutting pass — spindle up, cutting a real thread |
| **Angle & camera** | Macro on the first 30 mm of the pass, where the pitch is visibly wrong. Then the two finished parts (pre-fix and current-firmware) side by side. |
| **Audio to capture** | Clean cutting sound, TX at the machine, 32-bit float, nobody speaking during the actual cut. |
| **Target length** | ~10-15 min raw for the full sequence (flash, bench-test, cut, reflash, cut again) — edits down to a few seconds of "the moment the pitch goes wrong" plus the two-parts-side-by-side beauty shot. |

#### Script

> No dialogue during the cut itself — this is a macro capture. The framing VO ("F-01, the pre-fix thread... this is the most valuable shot in the series") is scripted separately as part of EP2's cold open and 22:30 payoff beats — record this footage first, write the exact narration once you've seen what you actually got.

#### Additional considerations

- **This is not literally the commit before `c3db8cd`.** That old build targets the bare TTGO T-Display's own 135×240 panel directly via TFT_eSPI, not the current board's 240×320 LVGL UI — it can't just be flashed as-is.
- **Instead:** `git checkout demo/ep2-pre-sync-fix` in the `TeensyELS` repo — local branch, commit `80e7545`, never pushed, never to be pushed or merged. A four-line change confined to `Leadscrew::update()` that drops the `pulsesToTargetSpeed` lead term, reproducing the pre-fix decision timing on current firmware and hardware, unmodified otherwise.
- **Bench-test at low speed with no workpiece before filming.** This intentionally reintroduces a known overshoot bug into real motion-control code.
- Build, flash, then thread a coarse pitch — 2.5 mm — at 800 rpm, which is where the effect is largest. Cut the same part again on current, unmodified firmware for the comparison.
- **Reflash to current firmware immediately afterwards** — do not leave the demo branch on the machine.
- This shot also feeds C-13 (scrapped threads on the dark surface, Phase 3) — keep the offcut.
- Hard ordering constraint from the bible: **shoot this before any other reflash or board rework**, including C-14's live rework in Phase 3.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

## Phase 2 — Makerspace trip

<p class="phase-note">A separate location entirely — not Martin's own Chester Model B. Book the makerspace's mini lathe ahead of time; this is the one phase in the whole plan that depends on someone else's schedule. Both shots happen in the same visit, back to back.</p>

<div class="pagebreak"></div>

### B-02 — Change gears, oily and tedious
<p class="meta">Sequence 2 of 65 &nbsp;&middot;&nbsp; Phase 2: Makerspace trip &nbsp;&middot;&nbsp; EP1 04:00</p>

| | |
|---|---|
| **Location** | Local makerspace, on their mini lathe — **not** the Chester Model B |
| **Setup** | Machine off. The change-gear set out of its box. The chart on the inside of the gear cover, showing columns of tooth counts. |
| **Angle & camera** | Overhead on the oily gears; macro on the chart's tooth-count columns; hands fitting a gear. Fit one wrongly on purpose and let the mesh be visibly bad. |
| **Audio to capture** | TX on Martin — this shot carries the one line explaining the location switch. |
| **Target length** | ~15-20 min raw (setup, deliberately-bad mesh, multiple macro passes) |

#### Script

> My time machine isn't working right now, so I've visited my local makerspace to show you how my lathe used to work.

#### Additional considerations

- Say this line **once, here** — it isn't repeated. The cold-open empty banjo (B-01) and the 4:1/8:1 selector lever (B-06) stay on Martin's own machine later, so the episode still reads as being about his lathe overall.
- Capture the mess and the tedium deliberately — oily hands are the point of this shot.
- Same trip as B-04 below — shoot both while the mini lathe is set up.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-04 — The thread dial that only reads one way
<p class="meta">Sequence 3 of 65 &nbsp;&middot;&nbsp; Phase 2: Makerspace trip &nbsp;&middot;&nbsp; EP1 05:40</p>

| | |
|---|---|
| **Location** | Local makerspace, same mini lathe, same visit as B-02 |
| **Setup** | A thread dial with its numbered face, geared off this lathe's leadscrew — generic, illustrative, not Martin's own machine |
| **Angle & camera** | Macro on the dial face, hand-feeding the carriage so the numbers pass the index mark. |
| **Audio to capture** | TX on Martin — this is part of the same continuous VO as B-03 (filmed later, on Martin's own lathe, in Phase 4). |
| **Target length** | ~10 min raw |

#### Script

> And the thread dial exists because of a genuinely awkward problem: once you disengage at the end of a pass and wind the carriage back, how do you know you're going to drop back into the *same* groove? You watch the dial, and you engage on the same number every time.
>
> But it gets worse. A thread dial is geared off the leadscrew, so it only reads correctly for *one* threading system — metric or imperial. Cut the other one on the same machine and the dial just lies to you.

#### Additional considerations

- This is the middle of a longer VO that continues on Martin's own lathe (B-03, Phase 4): "*And mine doesn't have one at all...*" — cut straight from this dial to Martin's own bare leadscrew end for the joke to land. Keep the two shoots' framing/lighting close enough in style to cut together cleanly despite being different locations and different sessions.
- Capture the numbers actually passing the index mark clearly — this is the visual proof the dial "reads" a specific position.

#### Production notes

<div class="notes-box"></div>
<div class="pagebreak"></div>

### C-10 — Enclosure print: timelapse, disassembly, keycaps
<p class="meta">Sequence 4 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 23:30</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Timelapse camera on the print bed for the full print run, plus a macro pass of a keycap going on. **Capture:** which legend goes on which key |
| **Audio to capture** | Printer ambient noise for the timelapse (largely discarded); natural room sound + DJI TX nearby for the keycap-fitting macro |
| **Target length** | Timelapse: full print duration (hours, interval capture). Keycap macro: ~5–10 min raw for a few clean seconds per key |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP4 24:30 beat.

#### Additional considerations

- **Two-touchpoint shot.** Start the print — and the timelapse capture — at the very beginning of the bench day, so it runs in the background while every other Setup C shot in this session is filmed. Come back to remove parts, assemble, and fit keycaps at the **end** of the day once the print has finished.
- Confirm which legend goes on which key against the keypad layout before fitting — this is the one continuity point the source material flags explicitly.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-02 — Full component flat-lay
<p class="meta">Sequence 5 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 00:40</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Board, display, motor, driver, encoder, PSU, printed parts, connectors, loom — everything visible and separated. This is the EP4 thumbnail — leave room in frame for a cost figure |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking during the take |
| **Target length** | ~10 min session — most of the time is arranging the layout, not shooting |

#### Script

> No dialogue — b-roll only, cut under narration from EP4's 00:40 costing beat.

#### Additional considerations

- This doubles as the EP4 thumbnail — shoot it clean and leave dead space for a cost-figure overlay.
- Arrange every component before rolling; this is a static/slow-pan shot, not something to fix in the edit.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-01 — Board: bare, assembled, enclosed
<p class="meta">Sequence 6 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 00:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Three matched overhead shots on the same mark: bare 4-layer board slid into frame, then the assembled board, then it in the enclosure — for a hard cut sequence |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~10 min including board-state changes between the three set-ups |

#### Script

> No dialogue — b-roll only, cold open for EP4.

#### Additional considerations

- All three shots must share the exact same mark/framing/light so the cut reads as one continuous move — do not reposition the camera between them.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-03 — Assembled board: macro tour
<p class="meta">Sequence 7 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 03:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Slow slide across the board, not cuts. Must pass over: the soldered-down WROOM module, the BSS138 level shifters, the keypad switches, the display socket |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~15–20 s per pass, 3–4 passes (~10 min raw) |

#### Script

> No dialogue — b-roll only, cut under narration from EP4's 03:00 "What's on the board and why" beat.

#### Additional considerations

- Slide, don't cut — the point of this shot is a continuous move across the board, edited as one take, not a montage of macro inserts.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-04 — Hand-soldering the connectors
<p class="meta">Sequence 8 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 10:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Macro, iron in shot, joint forming. Covers J1, J2, J3, J4 and the rotary encoder E1 |
| **Audio to capture** | Natural work sound (iron, solder, tool clinks); DJI TX nearby |
| **Target length** | Shoot more than you need — ~20–30 min raw for a few minutes of usable footage |

#### Script

> No dialogue — b-roll only, cut under narration from EP4's 10:00 "What arrives" beat.

#### Additional considerations

- Source material explicitly flags this as "satisfying footage — shoot more than you need." Don't ration takes here.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-05 — The encoder-voltage decision area
<p class="meta">Sequence 9 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 10:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Macro on the R13/R14 vs Q1/Q8 area, tight enough to read the designators |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~5 min |

#### Script

> No dialogue — b-roll only, cut under narration pointing at the encoder-voltage decision.

#### Additional considerations

- Keep this rig set up afterward — C-14 (next shot) reuses the exact same macro framing on the same board.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-14 — The three corrections (board rework)
<p class="meta">Sequence 10 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 19:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — same macro rig as C-05, shot immediately after while still set up |
| **Angle & camera** | Board in hand, plus a macro on R17 and on the R13/R14 link positions, tight enough to read designators |
| **Audio to capture** | If reworking live: natural soldering-iron work sound + DJI TX nearby. If a still/talk-over instead: room tone only |
| **Target length** | Live rework on camera: ~20–40 min raw (component swap is fiddly). Still/talk-over instead: ~10 min |

#### Script

> Before you order one of these, I owe you three corrections. All of them are mine, and all three are the same mistake in different clothes: something that was fine on my bench and quietly wrong for everybody else.
>
> **One.** I shipped this board populated for the wrong kind of encoder. There are two ways an encoder can get to the processor on here, and they are not options — they are alternatives. The way it ships suits an open-collector encoder. I am running a voltage-output one. Which means my encoder has been putting five volts onto a three-point-three volt pin that is not five volt tolerant, and it has been doing it for a year, and it works, and that is not the same as being right.
>
> **Two.** There is a one-K resistor on this board that should be a ten-K. It is the pull-up on one of the two encoder channels — the other channel has the ten-K. There is no reason on earth for the two channels of a quadrature pair to be different, so it is not a design decision, it is a slip that got fabricated. It works. It just draws ten times the current it needs to on that one line.
>
> **Three**, and this is the one that actually stops you building one. The files I have been telling you to order from — the ones with the part numbers in — were not in the repository. They were on my computer. They were in the instructions. They were not in the download. Which is, I think, the most on-brand mistake in this entire project.
>
> *(Then, briefly: the resistor is corrected in the schematic and board file; the production files are committed; the encoder recommendation now leads with the open-collector part, which needs nothing fitted.)*

#### Additional considerations

- Source material is explicit: "If you rework the board on camera — links off, Q1/Q8 and R15/R16 on — that is the whole segment shot in one go and far better than talking over a still."
- **If doing the live rework:** you need **BSS138 SOT-23 (LCSC C7420339)** and **1 kΩ 0805 resistors (LCSC C17513)** on hand *before* this session, plus a means of removing R13/R14 (the 0 Ω links). Without these in hand ahead of time, fall back to a still/talk-over instead — do not attempt to source them mid-session.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-06 — Teensy 4.1 vs ESP32-WROOM-32E
<p class="meta">Sequence 11 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP2 10:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Both modules in one frame at the same scale, then a macro on each individually |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~10 min |

#### Script

> No dialogue — b-roll only, cut under narration from EP2's 10:00 beat (the ESP32-port reasoning).

#### Additional considerations

- Match scale carefully in the two-shot — the comparison only reads if both boards appear at true relative size.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-07 — Stepper/driver comparison: AliExpress vs StepperOnline
<p class="meta">Sequence 12 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 13:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | The AliExpress closed-loop set and the StepperOnline 1-CL57Y-S30, with their drivers, framed as a pair. Both motors' rear encoders visible; both driver labels legible |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~10 min |

#### Script

> No dialogue — b-roll only, cut under EP4's 13:00 "Choosing a stepper" VO (already scripted in storyboard.html).

#### Additional considerations

- Source material is explicit: "The comparison is the beat, so frame them as a pair" — don't shoot the two motors as separate inserts.
- Keep this rig set up afterward — C-08 (next shot) uses the same driver, same physical setup.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-08 — CL57Y driver: DIP switch bank
<p class="meta">Sequence 13 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP4 13:00 · EP5 10:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — same rig as C-07, shot immediately after |
| **Angle & camera** | Macro on the DIP switch positions, matched against the datasheet's switch table — EP5 needs this number legible on screen |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~5 min |

#### Script

> No dialogue — b-roll only, cut under EP4's 13:00 beat and referenced again in EP5's 10:00 settings beat.

#### Additional considerations

- This single shot serves two episodes (EP4 and EP5) — get it right once rather than reshooting later.
- Frame tight enough that the switch positions are individually readable against the datasheet table, not just "a switch bank."

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-09 — J5 header and the USB-serial jig
<p class="meta">Sequence 14 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP5 02:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Macro tight enough to count pins on the J5 2×3 1.27 mm header and the USB-serial adapter/pogo jig. **Capture:** the adapter actually connected, and the pin-1 reference |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~5–10 min |

#### Script

> No dialogue — b-roll only, cut under narration from EP5's 02:00 "Programming a board with no USB socket" beat.

#### Additional considerations

- Pin-1 reference must be clearly visible — this is a header pitch small enough that miswiring is a real risk for viewers copying it.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-12 — The archaeology row
<p class="meta">Sequence 15 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP1 13:30</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Teensy, first encoder mount, first motor bracket, and every earlier board revision, laid out in date order. One overhead of the whole row, then a macro pass along it |
| **Audio to capture** | Room tone only; DJI TX nearby, nobody speaking |
| **Target length** | ~15 min — most of it is arranging objects in correct date order before rolling |

#### Script

> No dialogue — b-roll only, cut under narration from EP1's 13:30 "Getting it onto my lathe" beat.

#### Additional considerations

- **No breadboard** — there never was one. Do not stage one for continuity; the source material is explicit that this would misrepresent the build history.
- Confirm date order of the board revisions before laying out the row.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-13 — Scrapped threads on the dark surface
<p class="meta">Sequence 16 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; EP2 00:00</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — fixed overhead rig, hard raking light, dark surface |
| **Angle & camera** | Slow orbit on the worst scrapped thread; then a hand dropping it into the scrap bin, with the sound |
| **Audio to capture** | The scrap-bin drop sound is load-bearing — capture it cleanly, close mic, minimal room echo |
| **Target length** | ~10 min |

#### Script

> No dialogue — b-roll only, this is EP2's cold open.

#### Additional considerations

- **Dependency:** this shot needs the scrapped part(s) from F-01 already produced. F-01 happens in an earlier phase of this schedule, so the offcuts should already be in hand by this bench day — confirm you have them before this session, don't schedule this ahead of F-01.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### C-11 — Chip tray objects (all five)
<p class="meta">Sequence 17 of 65 &nbsp;&middot;&nbsp; Phase 3: Bench day (Setup C) &nbsp;&middot;&nbsp; All five episodes</p>

| | |
|---|---|
| **Location** | Home workshop bench — no lathe needed |
| **Setup** | Setup C — same mark, same light, all five objects shot in one go |
| **Angle & camera** | (a) EP1 — the first wrong-pitch thread. (b) EP2 — the retired Teensy in its bag. (c) EP3 — the part threaded into a shoulder with no relief groove. (d) EP4 — an earlier board revision. (e) EP5 — a single change gear, then pull back to the running lathe |
| **Audio to capture** | Room tone only for (a)–(d); (e) needs the running-lathe pull-back — DJI TX at the machine if that pull-back is filmed live rather than reused from Setup A footage |
| **Target length** | ~20–30 min for all five objects, swapping under identical lighting |

#### Script

> No dialogue — b-roll only, closes each of the five episodes.

#### Additional considerations

- **Deliberately scheduled last in this session.** Every one of these five objects is a pickup shot that gates nothing downstream — the source material is explicit: "do it when convenient, not early."
- (c) and possibly others depend on earlier manufactured-failure shots (e.g. F-03 for the no-relief-groove part) — confirm those objects exist before this session.
- (e)'s "pull back to the running lathe" may be more efficient to source from Setup A footage already shot, rather than a fresh lathe run — confirm which approach before scheduling camera/lathe time for this.

#### Production notes

<div class="notes-box"></div>
<div class="pagebreak"></div>

### A-01 — Tool entering the work
<p class="meta">Sequence 18 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 00:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running, multi-cam. Steel bar, ~25 mm, in the 3-jaw. Thread R, 1.5 mm, ~400 rpm. Stops set both ends. This is a mid-depth pass, not the first pass of the job. |
| **Angle & camera** | Macro on the tool entering the work. Cutting oil applied just before the take. Frame so the chip forming is readable. |
| **Audio to capture** | Part of the silent cold-open cluster (see note below) — TX at the machine, headstock or a stand, nobody speaking. 32-bit float. |
| **Target length** | Capture the moment of entry plus 4–5s of clean cutting. Shoot several takes — this is the opening frame candidate for the whole series. |

#### Script

> No dialogue — the cold open (EP1 00:00) is machine sound only, no narration, no music. This is one of four shots that make up that sequence.

#### Additional considerations

- Shot 1 of the cold-open cluster: A-01, A-02, A-03, B-01 are one continuous story beat (the bible's Coverage check table groups them together for EP1 00:00) — shoot all four in this session, in this order, before moving on.
- After filming this cluster with whatever ambient audio comes naturally, do a **separate silent repeat pass** of A-01/A-02/A-03 with the DJI TX at the machine and nobody speaking. CLAUDE.md flags clean machine-only audio for cold opens as "impossible to pick up later" — this cannot be deferred to another day.
- Rehearse the pass before rolling — this is a hero shot and worth several attempts.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-02 — Chuck coming up to speed
<p class="meta">Sequence 19 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 00:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Spindle only, no cut. Start from rest, run up to ~600 rpm. |
| **Angle & camera** | Macro on the chuck jaws coming up to speed, shallow depth of field. |
| **Audio to capture** | Part of the silent cold-open cluster — TX at the machine, nobody speaking. 32-bit float. This is *the* shot CLAUDE.md calls out for sound — the first frame of the series. |
| **Target length** | The full acceleration from rest to ~600 rpm, several takes. |

#### Script

> No dialogue — cold open, machine sound only.

#### Additional considerations

- Second shot of the cold-open cluster (see A-01). Capture the acceleration *and* the sound — this is explicitly the first frame of the series, so the audio here matters as much as the picture.
- Do this before the machine has been run hard elsewhere in the day, if possible — a fresh, clean spin-up sound is worth protecting.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-03 — The leadscrew turning with nothing driving it
<p class="meta">Sequence 20 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 00:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Feed or thread, carriage travelling under power. Gear cover *off*. |
| **Angle & camera** | Slow push-in along the bed, leadscrew and carriage both in shot. |
| **Audio to capture** | Part of the silent cold-open cluster — TX at the machine, nobody speaking. 32-bit float. |
| **Target length** | Get it twice — the bible calls this "the whole premise" of the series, so it's worth a second take from a slightly different push-in speed or start point. |

#### Script

> No dialogue — cold open, machine sound only.

#### Additional considerations

- Third shot of the cold-open cluster. The whole point of the frame is that the leadscrew is turning and the gear cover is open with visibly nothing mechanical driving it — check the framing catches both the leadscrew *and* the empty gear train before rolling.
- Immediately after this shot, the sequence whips to the gear cover and holds on the empty banjo — that's B-01, next.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-01 — The empty banjo
<p class="meta">Sequence 21 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 00:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped, safe and unhurried. Machine off. Gear cover closed, then opened on camera. Banjo empty. |
| **Angle & camera** | Hand opens the cover, hold on the empty banjo. |
| **Audio to capture** | Part of the silent cold-open cluster — TX at the machine or nearby, nobody speaking. This is a quieter, hands-only shot, so ambient workshop sound is fine; it still cuts against the silent A-01–A-03 repeat pass. |
| **Target length** | A few seconds of the cover opening, then hold on the empty banjo for a good 3–5s — this is the cold-open punchline. |

#### Script

> No dialogue — cold open, machine sound only.

#### Additional considerations

- Final shot of the cold-open cluster, and it's also **the thumbnail** — capture the emptiness cleanly, well lit, because this frame gets used twice (cold open and thumbnail).
- Once this cluster is done, move on to the silent repeat pass for A-01/A-02/A-03 before breaking down anything.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-05 — Stopping on the line
<p class="meta">Sequence 22 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 00:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. **Relief groove already cut** on the workpiece before this shot starts (see prerequisite below). Thread R, 1.5 mm, 400 rpm. Left stop set *inside* the groove. |
| **Angle & camera** | Tight on the tool, then repeat from a second angle. |
| **Audio to capture** | No talking during filming ("No talking" per the bible's shot note) — TX at the machine, clean cutting-sound pass. A short voice-over line is added in post, not spoken live. |
| **Target length** | Run in, decelerate, stop dead on the same line — twice, back to back, from two angles. A few seconds per take. |

#### Script

> Voice-over, added in post (not spoken on camera): *"Same place. Every pass. Without watching a dial."*

#### Additional considerations

- **Prerequisite: needs a relief groove already cut.** The groove itself doesn't get cut on camera until A-09, later in this session. Either pre-cut a practice groove off-camera before this shot, or use a spare piece from a rehearsal pass — don't leave this shot until after A-09 and expect to reuse that exact groove, since A-10 threads into it immediately afterward.
- The repetition is the argument — cut the two angles back to back so the identical stop reads clearly in the edit.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### F-02 — The wrong-PPR thread
<p class="meta">Sequence 23 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 16:30 &middot; EP5 21:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Manufactured failure. Enter a spindle-encoder PPR out by a clean factor. Cut a thread at low speed in something soft. |
| **Angle & camera** | The cut itself, then the nut refusing to start on it. |
| **Audio to capture** | Normal — TX on Martin if narrating live, otherwise TX at the machine for a clean b-roll pass. Nothing unusual here. |
| **Target length** | One low-speed pass (short), plus the nut-fitting attempt — a minute or two total, easily trimmed. |

#### Script

> No dedicated dialogue — cut under the narration from EP1's 16:30 beat ("the tease — it worked, at the defaults") and reused for EP5's troubleshooting-table row on the same symptom.

#### Additional considerations

- Simple and low-stakes — a good one to do early in the day while everything is fresh, since it needs no special setup beyond a deliberately wrong PPR value.
- Covers two separate beats in one take (EP1 tease and EP5 troubleshooting row), so don't rush it — get a clean, unambiguous "nut won't start" shot.
- Remember to set the PPR back to the correct value immediately after, before any other shot on this list.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-06 — Powered feed, one unbroken take
<p class="meta">Sequence 24 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 08:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Feed mode, 0.1 mm/rev, ~500 rpm. Turning or facing cut, decent depth. Right stop set. |
| **Angle & camera** | One unbroken 15s take, no cutaway. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. If Martin narrates live over this, TX moves to him instead (check which version is wanted before rolling — the bible's own script says "let it run for a good ten seconds unnarrated," so plan on an unnarrated pass first). |
| **Target length** | 15s minimum, unbroken, no cutaway — shoot a few full takes since there's no room to cut around a mistake mid-take. |

#### Script

> Real VO exists for this beat and is read over the top afterward, not spoken live: *"This alone might justify the whole build. Mode, feed. Rate, whatever you want in millimetres or thousandths per revolution. Enable... A feed rate chosen by typing a number beats a feed rate chosen off a gearbox chart, every time... And set a stop, and the cut ends in exactly the same place, pass after pass, without you watching for it."* Film this shot silent/unnarrated per the bible's own direction, and lay the VO over it in the edit.

#### Additional considerations

- The bible is explicit: let this run unnarrated for the full 15s+ so the finish is visible without interruption — the words above go on top in post.
- Continuous chip and surface finish are the things to protect in framing; don't let the tool or your hand block the cut.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-07 — Full threading job, start to finish
<p class="meta">Sequence 25 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 12:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running, three cameras. Full threading job, Thread R, both stops set, at least six passes to depth, increasing cross-slide each time. |
| **Angle & camera** | Three cameras rolling throughout — wide, over-shoulder on the panel, macro on the tool. |
| **Audio to capture** | Live demo while talking (EP3) — TX **on Martin**, per CLAUDE.md's rule for live demos at the machine. The lav picking up machine bleed is expected and reads as authentic. Also do a separate silent clean-audio repeat pass with TX at the machine afterward, since this is a real cutting-sound shot too. |
| **Target length** | This runs for real minutes — six-plus full passes, each with retract/wind-back/advance/re-engage. Expect several minutes of raw footage even though the edited beat is much shorter. |

#### Script

> Real VO exists (spoken live, matching the actual actions): *"This is the full run, real time, six passes minimum so you can actually see the repetition doing the work. I'll talk through the setup, then get out of the way and let the machine do it... The moment worth watching for is the wait after ENABLE — the sync chip changing state... And the thing to notice by the end: I wound the carriage back by hand, at whatever speed I felt like, between every single pass. Didn't matter. The helix is held in the maths, not in where the carriage happens to be sitting."* Also read live, from the numbered setup steps in storyboard.html: Mode → Thread R, set the rate, jog and set both stops, take the first depth of cut.

#### Additional considerations

- Capture the whole cycle including "the boring parts" — retract, wind back, advance, engage — that rhythm is what proves the repeatability. Don't cut around it even if it feels slow while filming.
- This is a long, real-time take — make sure all three cameras have enough card space and battery before starting, since restarting mid-sequence breaks the continuity that's the whole point.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-08 — The sync chip changing state
<p class="meta">Sequence 26 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 12:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Same rig as A-07. The instant ENABLE is pressed with the spindle already running. |
| **Angle & camera** | **Locked-off macro on the display.** Do not move the camera during this shot. |
| **Audio to capture** | Same session as A-07 — TX on Martin if talking through it live, otherwise TX at the machine for a clean pass. |
| **Target length** | Just the ENABLE moment and the wait for sync — a few seconds, but worth several attempts to get the framing exactly right on the display. |

#### Script

> The specific line this shot exists for: *"The moment worth watching for is the wait after ENABLE — the sync chip changing state. That's the least-shown moment in every ELS video I've watched, and it's the interesting one: that's the controller finding the exact spindle angle to start from."* (from EP3's 12:30 beat, shared with A-07.)

#### Additional considerations

- Shoot this **immediately after or adjacent to A-07** — same setup, spindle already running, don't re-rig.
- The bible is emphatic: "Every ELS video cuts away from this. Do not." — this is the one shot in the whole series where the display moment must not be interrupted or cut around.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-09 — Cutting the relief groove
<p class="meta">Sequence 27 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 20:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Grooving/parting tool, ~300 rpm. Cutting the relief groove at the shoulder, ~4 mm wide. |
| **Angle & camera** | Macro. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. |
| **Target length** | The groove-forming cut itself, plus a clean beauty pass of the finished groove before threading — a couple of short takes. |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP3 20:30 beat.

#### Additional considerations

- This groove is threaded into immediately next, in A-10 — don't move or re-chuck the workpiece between the two shots.
- Consider prepping a spare piece with an extra groove here too, since A-12 later in this session needs its own relief groove and it's efficient to cut both while the grooving tool is already set up.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-10 — Threading into the groove
<p class="meta">Sequence 28 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 20:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Thread R into the groove from A-09, same workpiece. Stop set inside the groove. |
| **Angle & camera** | Macro, tool arriving. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. |
| **Target length** | The tool running out into the groove and stopping — short — plus a still shot of the finished result. |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP3 20:30 beat.

#### Additional considerations

- Must follow A-09 directly — same workpiece, same groove, no re-chucking in between.
- Also get a still of the finished thread meeting the groove cleanly once the pass is done.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-11 — Left-hand thread
<p class="meta">Sequence 29 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 22:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Thread L, left-hand thread, fresh bar. 1.5 mm, 400 rpm. |
| **Angle & camera** | Wide + macro. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. |
| **Target length** | A full pass or two, plus time afterward to hold the finished LH thread up next to a RH one for comparison. |

#### Script

> Real VO exists for this beat (read over the footage): *"Thread L runs the carriage the other way — left stop to right stop. Obviously that gives you left-hand threads, which is worth having on its own."*

#### Additional considerations

- Capture the carriage running the opposite way clearly — that's the visual point of this shot.
- Keep a finished right-hand thread piece on hand from earlier in the day for the side-by-side comparison shot.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-12 — The clever one: threading away from a shoulder
<p class="meta">Sequence 30 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 22:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running. Normal RH thread cut *away* from the shoulder: Thread L mode, starting inside a relief groove, running out off the end of the bar. |
| **Angle & camera** | Macro at the groove for the start, wide for the run-out. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. |
| **Target length** | One full pass, macro-to-wide — a short take, but get the start-in-groove framing right, it's the shot that explains the trick. |

#### Script

> Real VO exists for this beat (read over the footage): *"But there's a second use that's arguably better. You can cut a normal right-hand thread away from a shoulder, by starting in the relief groove and running out into open air. Which means the acceleration phase — the part that isn't properly in sync — happens inside the groove, where there's no thread to spoil. And the deceleration happens off the end of the part, where there's nothing at all. Both of the bad bits land somewhere they don't matter."*

#### Additional considerations

- **Prerequisite: needs its own relief groove**, same prerequisite pattern as A-05. Reuse a spare groove piece prepped during A-09's session, or cut a fresh one here.
- The bible calls out the start-in-groove moment specifically as "the shot that explains the trick" — don't let the macro framing wander off it.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### A-13 — Mid-cut HALT
<p class="meta">Sequence 31 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 26:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup A — running (demo, not a real cut). Mid-cut, carriage under power. Press HALT. **Retract the tool first** — this is a demonstration, not an actual cut. |
| **Angle & camera** | Over-shoulder on the panel + wide. |
| **Angle & camera (cont.)** | — |
| **Audio to capture** | Normal — TX on Martin if narrating live, or a clean pass with TX at the machine if this is filmed as pure demo b-roll. |
| **Target length** | A few seconds — the deceleration and stop, held long enough to read clearly. |

#### Script

> Real VO exists, from EP3's 26:30 "safety mechanics" beat, the part relevant to this shot: *"While the carriage is moving, the panel answers exactly two keys: halt, and enable. Nothing else. Any menu that happens to be open closes itself. Your attention belongs on the tool, not on a settings screen, and the panel enforces that whether you meant to leave it open or not."*

#### Additional considerations

- **Retract the tool before pressing HALT** — the bible is explicit this is a demo, not a cut, so there should be no tool-in-work risk during the demonstration.
- Capture the panel refusing all other input while decelerating — that's the actual point being demonstrated, not just the stop itself.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### F-03 — Thread to a shoulder with no relief groove
<p class="meta">Sequence 32 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 28:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Manufactured failure. Thread right up to a shoulder with **no relief groove**, stop set at the shoulder. |
| **Angle & camera** | Macro on the mangled last few millimetres. |
| **Audio to capture** | Clean cutting-sound pass — TX at the machine, 32-bit float. |
| **Target length** | One deliberate bad pass — short, but get a clean macro of the damage afterward too. |

#### Script

> No dialogue during the cut itself. The finished piece is later used as an object for EP3's chip-tray outro, with the line: *"This is what the run-out looks like when you don't give it anywhere to go."*

#### Additional considerations

- This piece becomes chip-tray object **C-11c** later — don't discard it, set it aside cleanly for that session.
- Deliberately bad on purpose — no need to protect the workpiece or the cut, this is the failure case.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-06 — The 4:1/8:1 selector lever
<p class="meta">Sequence 33 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 04:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped, safe and unhurried. The 4:1/8:1 selector lever on the headstock. |
| **Angle & camera** | Macro, lever moved between positions. |
| **Audio to capture** | Ambient — nothing running, low-stakes. TX wherever convenient. |
| **Target length** | A few seconds per lever position, both positions shown clearly. |

#### Script

> Directly usable line from EP1's 04:00 beat: *"On the Chester Model B a single selector lever gives a 4:1 or 8:1 reduction between the spindle and the first change gear — and after that it is change gears the whole way to the leadscrew."*

#### Additional considerations

- The bible calls this out as "the one thing on this lathe that *is* a gearbox" — needed so the change-gear argument (mostly filmed at the makerspace, see Phase 2) stays accurate to Martin's own machine.
- This shot stays on Martin's own lathe deliberately, unlike B-02/B-04 — it's part of keeping the episode grounded in his actual machine.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-05 — One spindle turn, one small carriage move
<p class="meta">Sequence 34 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 02:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Change gears fitted (or ELS enabled). Turn the chuck by hand, slowly, several turns. |
| **Angle & camera** | Split frame or two takes: hand on chuck, and the carriage creeping. |
| **Audio to capture** | Ambient — nothing running, low-stakes. TX wherever convenient. |
| **Target length** | Several slow hand-turns of the chuck, a take or two for each half of the split frame. |

#### Script

> Real VO exists for this beat (read over the footage): *"A screw thread is a ratio. One turn of the spindle, and the tool has to move along by exactly one pitch."*

#### Additional considerations

- Capture the fixed relationship between one spindle turn and a small carriage move clearly — that's the whole teaching point of this shot.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-03 — The half-nuts, kept engaged
<p class="meta">Sequence 35 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP1 05:40</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped/hand-turned. Half-nut lever engaged and left clamped through a full pass and reversal — **not** disengaged. Spindle reversing to wind the carriage back. |
| **Angle & camera** | Macro, side on, on the split nut closing. Then a wider two-shot: half-nut lever untouched, spindle direction switch, carriage retracing. |
| **Audio to capture** | Ambient/hand-turned — low-stakes, TX wherever convenient, or on Martin if narrating live over this. |
| **Target length** | The lever closing (a few seconds), then a longer take of the reversal-and-retrace sequence to show the lever staying still throughout. |

#### Script

> Real VO exists for this exact shot: *"And mine doesn't have one at all. So the technique on this lathe was: never disengage. Leave the half-nuts clamped for the whole job, and wind the spindle backwards between passes instead — the carriage retraces the exact same helix going the other way, and you're always back where you started. It works. It is also one more thing to get wrong under time pressure, on top of everything else this operation demands."*

#### Additional considerations

- The point of this shot is that **the lever never moves once it's clamped** — make sure the framing holds the lever in shot throughout the reversal so that's unambiguous.
- This pairs with B-04 (the thread dial, shot at the makerspace in Phase 2) — in the edit it cuts straight from the makerspace dial to this shot of Martin's own leadscrew end with no dial at all.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-07 — Jogging: hold vs. click
<p class="meta">Sequence 36 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 06:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Idle, no stops set. Then repeat with a stop set. Guard open, no tool in the work. |
| **Angle & camera** | Over-shoulder on panel + wide on carriage. |
| **Audio to capture** | Ambient — nothing running at speed, low-stakes. TX on Martin if narrating live (this is a PTC-flagged beat). |
| **Target length** | Each behaviour (hold-to-jog, click-to-run, second-press-cancels) needs its own clean take — plan on three to four short takes. |

#### Script

> Directly usable line from EP3's 06:00 beat (no separate blockquote exists, but this is written as a direct statement): *"If there's no stop on that side, the arrow jogs while you hold it and decelerates when you let go. If there is a stop, a single click runs to it under power and holds."* Press the same arrow again and it cancels — mention this too.

#### Additional considerations

- No fixed word-for-word script beyond the line above — this is closer to a live demo with commentary than a scripted read. Talk through what's happening as it happens.
- Capture all three behaviours in this one session: hold-to-jog/release-to-decelerate with no stop, single-click run-to-stop with one set, and second-press cancelling.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-08 — The jog-speed picker
<p class="meta">Sequence 37 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 06:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Idle. OK pressed to open the jog-speed picker. |
| **Angle & camera** | Macro on the display, then wide showing the carriage at 1% and at 100%. |
| **Audio to capture** | Ambient — low-stakes, TX on Martin if narrating live (same session as B-07). |
| **Target length** | A short macro take on the display, then two short wide takes (1% and 100% carriage speed) for comparison. |

#### Script

> No fixed verbatim script — talk through it live. Cover *why 1% exists*: creeping up on a datum, per the bible's own note.

#### Additional considerations

- Same session as B-07 — shoot back to back, same rig, same panel state.
- The point of the comparison shot is the visible speed difference between 1% and 100% — make sure both are framed identically so they cut together cleanly.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-09 — Setting and clearing stops
<p class="meta">Sequence 38 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 10:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Idle. Jog to a position, STOPS, set. Then attempt to clear with a click; then hold to clear; then STOPS hold for clear-both. |
| **Angle & camera** | Macro on the display throughout. |
| **Audio to capture** | Ambient — low-stakes, TX on Martin if narrating live (this beat has real PTC dialogue, see script). |
| **Target length** | Each sub-action (set, click-fails, hold-clears, clear-both) is a short take — plan on four short macro takes. |

#### Script

> Real VO exists for this beat: *"Setting a stop is a click. Clearing one is a hold — and clearing both is a hold with a confirmation bar that fills for a second before it does anything. That's deliberate, and it's not me being precious about interface design. Setting a stop in the wrong place costs you five seconds. Clearing one loses a position you might have spent ten minutes finding with a dial indicator. Those two things should not cost the same gesture."*

#### Additional considerations

- Capture the marker landing on the travel bar, the "hold to clear" prompt appearing, and the confirm bar filling — all three are named explicitly as things to catch on camera.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-10 — Zeroing the DRO
<p class="meta">Sequence 39 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 26:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Idle. Hold OK to zero the DRO. Then set a second stop so the datum moves. |
| **Angle & camera** | Macro on the display. |
| **Audio to capture** | Ambient — low-stakes, TX on Martin if narrating live. |
| **Target length** | Two short macro takes — the zeroing action, then the datum-shift moment. |

#### Script

> Real VO exists, from EP3's 26:30 "safety mechanics" beat, the part relevant to this shot: *"The DRO is always referenced to an end stop — never to wherever the controller happened to boot up. And it flashes whenever that datum changes, so you can't miss it moving under you."*

#### Additional considerations

- Capture the readout flashing when the datum changes and the numbers jumping — that's the specific moment the shot exists to show.
- This is part of the same 26:30 "safety mechanics" beat as A-13 and F-04 — consider scripting/reading all three in one pass if it helps continuity, even though they're filmed at different points in this session.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-11 — Picking up an existing thread
<p class="meta">Sequence 40 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 24:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — **spindle stopped.** A part with an existing thread, re-chucked. Hand-wind the tool into a groove until it seats. Then MENU → Sync. |
| **Angle & camera** | Macro on the tool in the groove; then the display. |
| **Audio to capture** | Ambient — low-stakes, TX on Martin if narrating live. |
| **Target length** | Take this slowly — the bible says explicitly "do not rush this," so budget more time than the shot list implies, even though the final cut will be short. |

#### Script

> No fixed verbatim script — directly usable phrase from the beat: describe Sync as declaring *"this spindle angle and this carriage position are in sync."* Every later engagement re-enters that helix instead of ploughing a new one across it.

#### Additional considerations

- Called out as "the strongest 'oh' moment in EP3" — don't rush it, and make sure the Diagnostics screen reporting the manual anchor is clearly legible on camera so viewers can verify it took.
- This is a genuinely hard job on a manual lathe — if the first take doesn't seat cleanly, redo it rather than cutting around a fumble.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-12 — The spindle encoder and its drive
<p class="meta">Sequence 41 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP4 17:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Machine off. Your spindle encoder and its drive. |
| **Angle & camera** | Four angles minimum, plus a macro on the take-off. |
| **Audio to capture** | Ambient — machine off, low-stakes. |
| **Target length** | A handful of short static shots — four-plus angles, each just a few seconds. |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP4 17:00 beat.

#### Additional considerations

- **Critical:** the bible states the whole EP4 encoder correction segment depends on the 4:1 gear train between spindle and encoder being clearly visible in this footage — don't settle for an angle where it's ambiguous.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-13 — The stepper, adapter, and gears meshing
<p class="meta">Sequence 42 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP2 04:30 &middot; EP4 13:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Setup B — stopped. Machine off. The stepper, its machined adapter, and the two change gears meshing. |
| **Angle & camera** | Macro on the mesh; wider showing motor to leadscrew. |
| **Audio to capture** | Ambient — machine off, low-stakes. |
| **Target length** | A macro take on the mesh, a wider take of the full motor-to-leadscrew run, plus a short take of the motor being turned by hand to show the 2:1 ratio. |

#### Script

> No dialogue — b-roll only, cut under narration from EP2's 04:30 beat and reused for EP4's 13:00 stepper-choice beat.

#### Additional considerations

- Capture the tooth counts if they're legible — the point of this shot is proving **gears, not a belt**, so frame for that clearly.
- Turn the motor by hand on camera to show two motor turns per one leadscrew turn.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-14 — The loom, board to driver
<p class="meta">Sequence 43 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP4 20:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe/panel |
| **Setup** | Setup B — stopped. The loom, both ends. Board end and driver end. |
| **Angle & camera** | Macro on the RJ45 at the board; follow the cable; macro at the driver terminals. |
| **Audio to capture** | Ambient — machine off, low-stakes. |
| **Target length** | A slow follow-along the cable run, plus two macro end-shots — a couple of minutes raw to get a clean unbroken follow. |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP4 21:00 wiring beat.

#### Additional considerations

- Capture the **full run** so someone watching could copy it — don't skip sections of the cable path.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-15 — SW1, the manual override
<p class="meta">Sequence 44 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP4 20:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe/panel |
| **Setup** | Power on, motor enabled. Press SW1. |
| **Angle & camera** | Macro on the button, wide on the leadscrew. |
| **Audio to capture** | Ambient — TX on Martin if narrating live, otherwise TX nearby for a clean pass. |
| **Target length** | A couple of short takes — the button press and the motor releasing. |

#### Script

> Real VO exists in EP4's 21:00 wiring beat: *"Last thing on this board — SW1. It's a button that pulls stepper enable high directly, independent of whatever the firmware thinks is happening. That's your manual override, and it's the thing you press when you want the motor to let go, right now, no menus involved."*

#### Additional considerations

- Capture the motor actually letting go — the point is that this works independently of the firmware, so make sure that's visually clear (e.g. the leadscrew becomes freely turnable by hand immediately after the press).

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-16 — Panel mounted in position
<p class="meta">Sequence 45 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP4 23:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | Panel mounted in its working position. |
| **Angle & camera** | Wide from the operator's standing position. |
| **Audio to capture** | Ambient — low-stakes. |
| **Target length** | One or two wide takes from the normal operating position. |

#### Script

> No dialogue — b-roll only, cut under narration from the matching EP4 24:30 enclosure/mounting beat.

#### Additional considerations

- Capture reach without leaning over the work, readability from a normal standing position, and distance from where swarf lands — all three are explicitly named as things this shot needs to demonstrate.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### F-04 — The driver alarm
<p class="meta">Sequence 46 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP3 26:30 &middot; EP5 19:00</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe/panel |
| **Setup** | Manufactured failure. **Leadscrew disconnected.** Wind the acceleration setting up until the closed-loop driver faults. |
| **Angle & camera** | The alarm modal on screen, the latch holding, and OK firing the reset pulse without dismissing the dialog. |
| **Audio to capture** | Ambient — machine off/leadscrew disconnected, low-stakes. TX on Martin if narrating live. |
| **Target length** | A short take building up to the fault, then a clear hold on the modal and the OK-press behaviour — a couple of minutes raw. |

#### Script

> Real VO exists, from EP3's 26:30 "safety mechanics" beat, the part relevant to this shot: *"And if the stepper driver raises an alarm, the machine halts, latches, and stays latched until you acknowledge it. Not because a fault that trips and clears is dangerous by itself — it's that the carriage moved while it was tripped, and the moment it moved, your sync stopped being trustworthy. Acknowledging isn't clearing the fault. It's you confirming you know the sync is gone."*

#### Additional considerations

- Do this right after B-15/B-16 since the panel is already the focus of attention.
- The bible flags this as "one of the newest features" and the only shot anywhere in the series that shows the alarm actually working — don't skip it even if time is tight.
- Leadscrew must be disconnected before winding acceleration up — confirm this before starting, for safety and to avoid an uncontrolled real fault on a connected mechanism.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-17 — The five-step verification checklist
<p class="meta">Sequence 47 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP5 10:00 &middot; 15:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe |
| **Setup** | **Leadscrew disconnected** for steps 1–2, reconnected for steps 3–5. Dial indicator on the carriage. |
| **Angle & camera** | Split-screen pairs: the setting on the phone, the thing being measured on the machine. |
| **Audio to capture** | TX on Martin — this is read live as a checklist while performing each step. |
| **Target length** | Five short segments, one per checklist step — a few minutes raw altogether, good closing sequence for the day since it re-confirms everything else filmed today actually works. |

#### Script

> Read live, from the checklist itself (EP5's 15:30 beat): direction (jog left, does it go left?) &middot; distance (dial indicator vs. DRO, out by a clean factor means steps-per-mm or microstepping) &middot; encoder (one full spindle turn by hand, watching Diagnostics — this *is* the measurement) &middot; pitch without cutting (ten hand turns, measure carriage travel, compare to ten × pitch) &middot; then a test thread with a nut. No separate scripted paragraph exists beyond the checklist wording — narrate each step as you do it.

#### Additional considerations

- All five steps need to be captured as a followable sequence — direction, distance against the indicator, one spindle turn on Diagnostics, ten hand turns measured, then the test thread with a nut.
- This is a strong candidate for the very end of the lathe day, since it's essentially a proof-of-everything session and works best once the day's other reconfigurations are done.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### B-18 — Cold boot, dark workshop
<p class="meta">Sequence 48 of 65 &nbsp;&middot;&nbsp; Phase 4: Home lathe day (Setup A + B, F-02/F-03/F-04 folded in) &nbsp;&middot;&nbsp; EP5 00:00 &middot; EP1 18:30</p>

| | |
|---|---|
| **Location** | Martin's workshop, at the lathe/panel |
| **Setup** | Dark workshop. Board powered from cold. |
| **Angle & camera** | Macro on the panel, slightly slow. |
| **Audio to capture** | Ambient — quiet room, low-stakes. |
| **Target length** | The full boot sequence, unbroken — splash, brand mark, firmware version, then the rest screen. Maybe 10–20s depending on actual boot time. |

#### Script

> No dialogue — b-roll only, cut under narration or used silently in the EP1 18:30 montage and EP5's 00:00 cold open.

#### Additional considerations

- **Needs genuinely dark/evening conditions** — this is not something to fake with the lights off during the day; schedule it for the very end of the day once natural light has actually gone, so the panel glow reads properly on camera.
- Capture the whole boot sequence unbroken: splash, brand mark, firmware version, then the rest screen appearing.
- This closes out the lathe day — after this, break down the rig and move on to Phase 5 (Setup D, desk work) whenever convenient.

#### Production notes

<div class="notes-box"></div>
<div class="pagebreak"></div>

### D-01 — JLCPCB order walkthrough
<p class="meta">Sequence 49 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP4 06:30</p>

| | |
|---|---|
| **Location** | Desk, screen recording — no camera, no lathe needed |
| **Setup** | Record at 1080p or better, slow the mouse down deliberately — people will pause this to read the values |
| **Angle & camera** | N/A — screen capture, not filmed. Full-screen browser window, no other tabs/bookmarks visible |
| **Audio to capture** | None live. This is a silent screen capture — narration is recorded separately as voice-over and laid over the footage in the edit (see the EP4 06:30 beat's script in storyboard.html for the exact words) |
| **Target length** | *(confirm before shoot day)* — a real JLCPCB order start to finish likely runs 5–10 minutes raw (upload, layer/size confirmation, assembly toggle, BOM/CPL upload, placement preview review); edits down to well under a minute |

#### Script

> No live narration during capture. The beat's own VO exists in storyboard.html (EP4 06:30) and gets added afterward. What matters here is that every on-screen value stays legible and correct: the uploaded file is `GERBER-TeensyELS.zip` (as the zip, not unzipped), the auto-detected layer count reads **4 layers**, board size reads **~113×109 mm**, and the placement preview shows U1, U2 and the MOSFET orientations clearly enough to freeze-frame.

#### Additional considerations

- Use the real, current production files from `kicad/LVGL/jlcpcb/production_files/` — the bible is explicit that `kicad/LVGL/production/bom.csv` and `positions.csv` are stale leftovers from an older revision (socketed display, Teensy footprint) and must not appear on screen.
- This is a real paid order — decide in advance whether you're placing a genuine order during this capture or a walkthrough you abandon before payment. Either is fine, but know which before you start recording so the ending doesn't look like an accident.
- Keep personal account details (name, address, saved payment info) out of frame — check the account/billing pages aren't visible before hitting record.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-09 — KiCad 3D viewer, board rotating
<p class="meta">Sequence 50 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP4 03:00</p>

| | |
|---|---|
| **Location** | Desk, screen recording |
| **Setup** | KiCad 3D viewer, `TeensyELS.step` loaded. Free rotation, no lighting problem — this is why it's used instead of photographing the bare board |
| **Angle & camera** | N/A — screen capture. Capture several full rotations plus a few closer orbits on specific areas (WROOM module, level shifters, keypad switches, display socket) to match the callouts in the EP4 03:00 script |
| **Audio to capture** | None live — silent capture, narration added later (EP4 03:00's real script already exists in storyboard.html) |
| **Target length** | 1–2 minutes raw rotation footage is plenty; edit picks the smoothest few seconds |

#### Script

> No live narration during capture. Make sure the rotation is slow and smooth enough to cut against the macro shots of the real board (C-03) without looking rushed.

#### Additional considerations

- This pairs directly with shot **C-03** (assembled board macro tour) — the edit cuts between this animated view and the real board, so keep framing/scale roughly consistent with how C-03 was shot.
- Free and better than photographing a bare board, per the bible — no special lighting rig needed for this one.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-05 — The two one-line diffs
<p class="meta">Sequence 51 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP2 02:00 · 04:30</p>

| | |
|---|---|
| **Location** | Desk, screen recording |
| **Setup** | A diff viewer (GitHub's own commit view, or a local diff tool), not a terminal — the bible is explicit on this |
| **Angle & camera** | N/A — screen capture. Show commit `ed9d9c3` (the ratio inverted) and commit `70220e8` (steps/mm and the gearbox) as two clean, readable diff views |
| **Audio to capture** | None live — silent capture. This footage cuts under EP2's Bug 1 (02:00) and Bug 2 (04:30) beats, both of which already have full scripted VO in storyboard.html |
| **Target length** | Under 2 minutes to capture both diffs cleanly at a readable zoom level |

#### Script

> No live narration during capture. The only requirement is legibility — zoom in enough that the single changed line is readable on a phone screen, since this is exactly the kind of frame viewers pause on.

#### Additional considerations

- Two separate commits, two separate diff views — don't try to force them into one capture. Cut them together in the edit.
- Confirm both commit hashes resolve correctly in whatever repo state you're viewing before recording — a stale local checkout could show the wrong diff.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-03 — The test suite going green
<p class="meta">Sequence 52 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP2 25:30</p>

| | |
|---|---|
| **Location** | Desk, screen recording |
| **Setup** | Terminal, `pio test -e native`, let it run all the way to the summary line — 487 cases per CLAUDE.md |
| **Angle & camera** | N/A — screen capture. Full terminal window, font size large enough to read on a phone |
| **Audio to capture** | None live — silent capture. Pairs with EP2's 25:30 beat, which has full scripted VO already in storyboard.html |
| **Target length** | *(confirm before shoot day)* — record the actual real-world run time of the full suite once beforehand so you know whether it needs speeding up or trimming in the edit; do not guess |

#### Script

> No live narration during capture. Let the terminal output actually reach the summary line before cutting — don't fake the ending.

#### Additional considerations

- Run this from a clean checkout so the count of passing tests genuinely matches what's said on camera (487, per CLAUDE.md) — if the number has drifted since, re-verify before recording rather than repeating a stale figure.
- Consider a second pass showing one test's source alongside its name, if there's room in the edit — helps a non-developer viewer understand what "487 tests" actually means.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-04 — render.sh generating the screenshots
<p class="meta">Sequence 53 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP2 25:30</p>

| | |
|---|---|
| **Location** | Desk, screen recording |
| **Setup** | Terminal running `bash tools/screenshot/render.sh`, then a file browser window opening a few of the resulting PNGs |
| **Angle & camera** | N/A — screen capture |
| **Audio to capture** | None live — silent capture, shares the EP2 25:30 beat's existing script with D-03 |
| **Target length** | *(confirm before shoot day)* — time the real run once beforehand; capture the whole thing rather than cutting mid-run |

#### Script

> No live narration during capture. The point of the shot is the folder actually filling with PNGs — don't skip past that part, and open at least two or three of the generated screens afterward so the payoff is visible, not just implied.

#### Additional considerations

- This directly supplies the "rendered PNGs" used elsewhere in the series (EP3's 04:00 screen beat), so make sure the run is against current firmware, not a stale build.
- These are also the assets EP3 uses instead of filming the real panel — worth having a few of the best-looking ones bookmarked for reuse.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-02 — PlatformIO install and flash
<p class="meta">Sequence 54 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP5 05:00</p>

| | |
|---|---|
| **Location** | Desk, screen recording |
| **Setup** | PlatformIO install, clone the repo, set `upload_port`, then `pio run -e esp32dev_usb -t upload`, watched through to completion |
| **Angle & camera** | N/A — screen capture. Real time enough to be followable, per the bible's own note on this beat |
| **Audio to capture** | None live — silent capture. EP5's 05:00 beat has no scripted VO yet in storyboard.html (it's outline-only) — see Additional considerations |
| **Target length** | *(confirm before shoot day)* — an ESP32 build+flash typically runs a few minutes; time the real one before deciding how much to show unedited |

#### Script

> No live narration during capture — but note that EP5's 05:00 "Building and uploading" beat does not yet have a finished script in storyboard.html, only the shot direction (install PlatformIO, clone, set the upload port, run the upload command, watch it flash). Whoever records the voice-over for this beat will need to write that narration against the finished footage, not read anything fixed from this page.

#### Additional considerations

- Board must be connected via the J5 header adapter from shot C-09/D-01's sibling shot — confirm that rig is already built and tested before this session, not improvised live.
- Show the actual terminal output of a successful flash, not a mocked-up or trimmed version — if it fails first, that's fine, keep rolling per CLAUDE.md's general guidance on real failures.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-06 — The web configuration page
<p class="meta">Sequence 55 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP5 07:30 · 10:00</p>

| | |
|---|---|
| **Location** | Desk, screen recording (phone and desktop both) |
| **Setup** | The web config page, shown on a phone browser and a desktop browser. Every field filled in, then Submit pressed |
| **Angle & camera** | N/A for the screen capture itself, but also film the phone physically with a second camera so the edit can cut against the device rather than only a clean screen-record |
| **Audio to capture** | None live for the screen capture; if filming the phone with a second camera, capture ambient room sound only, no need for the TX here |
| **Target length** | A few minutes to fill in every field carefully and legibly; the edit will only need a fraction of it |

#### Script

> No live narration during capture. This shot is shared between two beats — EP5 07:30 (first boot / setup access point, outline-only in storyboard.html, not yet scripted) and EP5 10:00 (the "Every setting" reference segment, which has extensive real content — a full table and worked example already written). Whoever narrates this footage should be working from the EP5 10:00 material primarily, since that's where the actual finished script lives.

#### Additional considerations

- Every field needs to actually be correct for *this* machine when demonstrating the worked example (encoder PPR, stepper PPR, gearbox ratio, leadscrew pitch) — don't fill in placeholder numbers, since EP5's whole point is that these are measured, not guessed.
- Shoot this after the machine's real settings are finalized and confirmed working — not as a first draft.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-07 — Joining the setup Wi-Fi
<p class="meta">Sequence 56 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP5 07:30</p>

| | |
|---|---|
| **Location** | Desk or wherever the board is powered on for setup |
| **Setup** | Phone joining `ELS_Wifi`, captive portal appearing via the "sign in to network" sheet — the reliable path on Android per the bible |
| **Angle & camera** | Film the phone itself with a second camera as well as screen-recording it, so the edit can cut between the two |
| **Audio to capture** | None critical — this is a quiet, procedural shot. Ambient room sound is fine |
| **Target length** | Under 2 minutes per attempt, but expect to need several takes — captive portal behaviour can be inconsistent between phones/Android versions |

#### Script

> No live narration during capture. EP5's 07:30 beat is currently outline-only in storyboard.html (no fixed script) — narration gets written once the actual footage is in hand and it's clear which path (captive portal vs. manual) actually worked cleanly on camera.

#### Additional considerations

- Test this path once before the "real" take — captive portal detection is genuinely inconsistent across Android versions, and the bible calls this out as "the reliable path," implying other paths were tried and were less reliable. Know the fallback before rolling.
- If it fails to trigger the captive portal sheet, have a manual fallback (typing the AP's IP into a browser) ready to demonstrate instead, and note honestly on camera that this is the fallback if that's what actually happens.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### D-08 — OTA update and the GitHub release page
<p class="meta">Sequence 57 of 65 &nbsp;&middot;&nbsp; Phase 5: Desk afternoon (Setup D) &nbsp;&middot;&nbsp; EP5 19:00</p>

| | |
|---|---|
| **Location** | At the panel (for the macro half) and desk (for the GitHub page half) |
| **Setup** | An OTA update actually running on the device, screen-recorded from the panel side (this half is a macro shot, not a desktop capture) plus the GitHub release page it pulls from |
| **Angle & camera** | Macro on the panel's progress bar; separately, a clean screen capture of the GitHub Releases page |
| **Audio to capture** | Ambient for the macro half (TX nearby is fine, nothing critical is being said); none for the GitHub screen capture |
| **Target length** | *(confirm before shoot day)* — time a real OTA update once beforehand so the raw capture length is known rather than assumed |

#### Script

> No live narration during capture. This pairs with EP5's 19:00 beat, which already has a real script in storyboard.html ("Three things on this screen worth knowing before you need them...") — that VO gets recorded separately and laid over this footage.

#### Additional considerations

- Needs a real newer release available to update *to* — coordinate this so an actual release exists at recording time, not a simulated one.
- The debug-capture mention in the EP5 19:00 script doesn't need its own shot here — it's covered by the script alone, referring back to the Diagnostics screen already shown elsewhere.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

## Phase 6 — Pieces to camera, last (Setup E)

Shoot this phase only after every other phase's rushes have been reviewed — the bible is explicit that PTC wording should be finalized against what was actually captured, not written blind in advance. **E-S is the one exception: shoot it first within this phase**, even though the phase as a whole comes last.

<div class="pagebreak"></div>

### E-S — The safety card
<p class="meta">Sequence 58 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; Reused in EP1 20:30 · EP3 12:00 · before the first ENABLE in EP4 and EP5</p>

| | |
|---|---|
| **Location** | Wherever a static, flat-lit frame can be set up cleanly — doesn't need to be at the lathe |
| **Setup** | Static frame, flat light, no music, deliberately plain. Recorded **once** and reused verbatim across all five episodes — do not re-shoot this per episode |
| **Angle & camera** | Locked-off, straight to camera, no movement |
| **Audio to capture** | TX on Martin |
| **Target length** | ~100 words, roughly 35–40 seconds read at a measured, serious pace |

#### Script

> Before we go any further. Change gears are a mechanical guarantee: if the gears are meshed, the ratio is right, because metal doesn't have opinions. An electronic leadscrew throws that guarantee away and replaces it with software — software I wrote, on hardware you soldered, configured with numbers you typed in.
>
> A miswired encoder or one wrong setting can drive the carriage into a spinning chuck. Know where your stop is before you engage. Test with the leadscrew disconnected. And if you build one of these, you own the consequences of it — that's not me covering myself, that's just true.

#### Additional considerations

- This single take gates **EP1, 3, 4 and 5** per the bible's own gating notes — get it right here and it never needs revisiting.
- Deliberately flat and serious in tone — no music, no cutaways, the plainness is the point.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-L / EP1 — Lathe piece to camera, Episode 1
<p class="meta">Sequence 59 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP1 00:50</p>

| | |
|---|---|
| **Location** | At the lathe |
| **Setup** | Standing at the lathe, controller panel visible over your shoulder but **not explained yet** — do not demo the panel here, that reveal belongs to EP3 |
| **Angle & camera** | Mid shot, panel visible but out of focus/unexplained behind you |
| **Audio to capture** | TX on Martin |
| **Target length** | ~195 words, roughly 70–80 seconds raw read-through |

**Script — read through in this order**

**00:50 — Promise and contract**
> That, behind me, is an electronic leadscrew. It replaces the box of change gears on this lathe with a motor, a sensor, and about a year of my evenings.
>
> It's open source, and if you want to build one, I'm going to show you exactly how — board, wiring, all of it, later in this series. It didn't start with me. Two other people built theirs first, put the whole thing on YouTube, and made the code public. I'll name them properly in a few minutes, because none of this happens without that.
>
> This episode is the *why*. Not the how-to-use, not the how-to-build — those are coming. This one is just: what a leadscrew actually does, and why I decided the box of gears had to go.

#### Additional considerations

- Resist demonstrating the panel here — the shot direction is explicit that this spends EP3's reveal for nothing if done early.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-B / EP1 — Bench pieces to camera, Episode 1
<p class="meta">Sequence 60 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP1 13:30 · 16:30 · 22:20</p>

| | |
|---|---|
| **Location** | At the bench |
| **Setup** | Shoot all three beats in one continuous sitting, in this script order, so wardrobe and light match across the finished episode |
| **Angle & camera** | Mid shot at the bench; for 13:30, the archaeology row (C-12) should be visible/nearby to gesture at |
| **Audio to capture** | TX on Martin |
| **Target length** | ~370 words total, roughly 2.5–3 minutes raw read-through for all three beats |

**Script — read through in this order**

**13:30 — Getting it onto my lathe**
> This is what it actually looked like at the start. A Teensy 4.1, the first encoder mount, a motor bracket I'd machined and wasn't especially proud of. There was never a breadboard — it went straight onto the lathe, because that's the only way you find out if any of this is real.
>
> It ran on a Teensy, so the project was called TeensyELS. Seemed obvious at the time. It took about two years to work out that naming firmware after whatever chip happens to be in it is a mistake — because eventually the chip changes, and the name doesn't.
>
> The first real moment was the carriage moving under encoder control for the first time — turn the chuck by hand, and the carriage crawls along with it, no gears in between. That one worked.
>
> The second moment was asking it for a pitch and getting a different one. That's where episode two starts.

**16:30 — The tease: "it worked, at the defaults"**
> Here's the thing about a project that works. It works *on the machine it was written on*, with the numbers it was written with.
>
> Every configurable value in that firmware had a default. And every default was the value on somebody else's lathe. So the moment I put my own numbers in — different leadscrew pitch, different encoder, and a two-to-one *gear* reduction that the original author simply didn't have — things started coming out wrong. Not broken. *Wrong*, which is worse, because wrong looks like it's working right up until you measure it.
>
> I found four separate bugs that were only invisible because nobody had ever changed the settings. One of them had been in there since before Jack got involved. That's the next episode.

**22:20 — Outro**
> *(No fixed script in storyboard.html — direction only.)* Next time: the four bugs, why the ESP32, and the evening that cost me the spindle loop. Repo link, both channels linked again, and ask specifically for lathe make and model in the comments.

#### Additional considerations

- The 13:30 beat references the archaeology row (shot C-12) — have those objects physically on the bench, not just mentioned, so you can gesture at real hardware.
- 22:20 has no fixed script yet, only the beats to hit — treat the quoted line above as a starting point, not a verbatim read.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-B / EP2 — Bench pieces to camera, Episode 2
<p class="meta">Sequence 61 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP2 00:40 · 07:00 · 09:00 · 10:00 · 16:00 · 22:30 · 27:00 · 29:30</p>

| | |
|---|---|
| **Location** | At the bench |
| **Setup** | Eight beats, one continuous sitting, in this script order. **This is the longest single PTC session in the series** — see Additional considerations before assuming it fits in one take of one sitting |
| **Angle & camera** | Mid shot at the bench; laptop and board visible per the 00:40 beat's direction |
| **Audio to capture** | TX on Martin |
| **Target length** | ~2,000 words across all eight beats — roughly **12–15 minutes** of raw read-through, well beyond a single unbroken take. Budget the session accordingly |

**Script — read through in this order**

**00:40 — Thesis**
> Here's the argument for this whole episode, in one sentence: a default value is a lie that agrees with you.
>
> I found four separate bugs in this firmware. Every one of them was completely invisible at the settings it shipped with, and every one of them was fatal the moment I typed in numbers from my own machine.
>
> And here's why that's not just my problem. If you're watching this because you're about to build one — your lathe isn't my lathe. Different pitch, different encoder, different reduction. The moment you enter your own numbers, you are running code nobody has ever run with those numbers before. That's what this episode is actually about.

**07:00 — Bug 3, direction and end stops**
> ⚠️ **This script is a structural placeholder, not a sourced account — do not read it verbatim on camera.** Storyboard.html flags this explicitly: it's built only from commit-log memory nudges, which the document does not trust as a full explanation. Replace the bracketed sections below with what actually happened before recording.
>
> This one was a cluster, not a single bug — direction state and end-stop handling, tangled together, and it took more than one pass to actually close out.
>
> The way a direction bug shows up on a lathe is unmistakable once you've seen it: *[describe what you actually saw — carriage moving the wrong way at a specific moment, or refusing to stop at a configured end stop]*. The commit log shows this took at least three attempts on one day in March 2025 — fix, revert, fix again — which tells you it wasn't obvious even once you were looking straight at it.
>
> What actually closed it out: *[your memory of the real fix — was it direction state leaking between moves? A distance-from-endstop rule that was too aggressive, or not aggressive enough?]*.
>
> The stakes, plainly: a direction bug on a lathe is not cosmetic. The wrong direction at the wrong moment is the carriage heading for the chuck.

**09:00 — Bug 4, planted and deferred**
> *(No fixed blockquote — direction only.)* Say something to the effect of: "the fourth one isn't a typo, it's a design flaw, and it's the last thing in this episode." Plant it, walk away, come back at 22:30.

**10:00 — Why the ESP32, and why that meant forking**
> Then there's the awkward part. Supporting two completely different processors in one codebase means every piece of hardware-facing code exists twice, and both copies have to keep working. I did carry both for a while — you can see it in the history, files split into ESP and Teensy versions.
>
> And the order of events matters here, so let me be clear about it: I didn't go off and fork it. I brought the whole ESP32 port *upstream*, as pull request 28 — eighty-one commits — and Jack merged it in May 2025. For a while the project genuinely ran on both processors.
>
> But I was the only one using the ESP32 side, and Jack's machine ran a Teensy. At some point the honest thing is to stop pretending it's one project. So in January 2026 I deleted Teensy support outright, and *that* is the moment this became a fork — eight months after the merge, not instead of it.
>
> Which is exactly what open source is for. Nothing went wrong. It just went two ways.

**16:00 — The evening it dropped to 58 Hz**
> The motion loop runs at seventy-eight thousand iterations a second. One evening, while jogging, it was running at *fifty-eight*. Not fifty-eight thousand. Fifty-eight.
>
> The step pulses go out through a peripheral called the RMT, which takes an array of pulse definitions. And the call that hands it that array takes a count. I'd passed it `sizeof` — the size of the array in bytes. Which made it ninety-six, for an array of twenty-four. So it read seventy-two entries past the end of the array, and it also blew past the hardware buffer size, so the write *blocked* while the peripheral clocked out uninitialised heap.
>
> And here's the part that nearly broke me: only the first element of that array had ever been initialised. So what it was actually transmitting was whatever happened to be sitting in memory after it — which rearranges itself *every time you rebuild*. The same commit measured fast on one build and catastrophically slow on the next.

**22:30 — Bug 4, paid off: the sync start**
> Here's the problem. When you press ENABLE, the spindle is already turning and the leadscrew is stopped. You can't just start moving — a motor with a lathe carriage on the end of it has to accelerate.
>
> So the original code did the obvious thing: wait until the spindle comes round to the sync angle, and start there. Which means at the instant it starts, it is perfectly correct — and a fraction of a second later it's behind, because it spent that fraction of a second getting up to speed. Then it notices it's behind, so it runs *faster* than the thread to catch up. Then it overshoots.
>
> Let me put real numbers on it. Two-and-a-half millimetre pitch, eight hundred RPM. By the time it is up to speed it has fallen **3.7 millimetres behind** — and that is exactly the distance it takes to accelerate, which is not a coincidence, it is the same sum.
>
> Now. How do you close a gap like that? There is only one way. **You run faster than the thread.** There is no other mechanism available — the carriage has to overtake the helix to get back onto it.
>
> And on this machine it runs straight into the speed ceiling and sits there: **forty millimetres a second against a thread speed of thirty-three**. Twenty per cent over. Which sounds abstract until you convert it, because carriage speed divided by spindle speed *is* pitch — and forty millimetres a second at eight hundred RPM is **a three millimetre pitch**. On a two-and-a-half millimetre thread.
>
> It holds that for eight tenths of a second before it is back on the helix. Twenty-seven millimetres of travel. So every pass starts too fine, while it is accelerating, and then goes too coarse, while it catches up — and both of those are cut into the work.
>
> *(Beat, then the fix, replayed on the same animation.)*
>
> The fix isn't to accelerate harder. It's to *start earlier*.
>
> I know the acceleration rate, because it's a setting. I know the target speed, because I'm measuring the spindle continuously. So I can work out exactly how far the carriage travels while it's getting up to speed — it's just the area under that ramp. Half the speed difference, times the time it takes.
>
> So instead of asking "is the spindle at the sync angle *now*", it asks: "will the spindle be at the sync angle by the time I've *finished* accelerating?" Start the ramp there, and the carriage arrives at full speed exactly on the helix. No catch-up. No overshoot. It is in sync from the first correct millimetre.
>
> That, for my money, is the single best thing in this firmware, and it's the one bit that's properly mine. May 2025.

**27:00 — The AI segment, once, honestly, then done**
> I should be straight with you about something, because it's in the commit history and you'll find it.
>
> Everything I've just described — the ratio bugs, the processor port, the pulse generation, the core split, the sync-start fix — I did. On hardware, over about a year, mostly badly the first time. That's the engine, and it was running before any of this.
>
> From last August I started using AI as a tool on top of it. And I want to be precise about what it was actually good for, because it isn't what people assume. It was good for the tedious, high-value work I would otherwise never have got round to. This project went from fifty-four automated tests to four hundred and eighty-seven. It got a host-side renderer that draws all forty-one screens without a lathe. The user interface got rewritten to a written specification, test-first — failing tests, then the code.
>
> And those tests caught real bugs. Including one where a divisor went missing from that same steps-per-millimetre calculation for the second time in two years — except this time it never left the bench.
>
> It's a power tool. It didn't design the part. It removed the hours between deciding and knowing. And I read every line of it, because it's driving a lathe.

**29:30 — Chip tray + outro**
> *(No fixed blockquote — direction and two quoted lines only.)* Object: the Teensy, still in its anti-static bag, retired. "Nothing wrong with it. It just wasn't two processors." Then the hand-off: "That acceleration fix means the controller starts early. But it can only start early if there's somewhere to do it — and that changes how you have to machine the part. Next time: actually cutting threads."

#### Additional considerations

- **This session is long.** ~2,000 words is 12–15 minutes of continuous talking, before retakes — realistically this needs breaking into at least two sittings on the same day (same wardrobe, same light setup kept unchanged between them) rather than one unbroken take.
- **07:00 (Bug 3) needs Martin's real memory filled in before this session**, not worked out live on camera — the bracketed placeholders should be replaced with actual recollection well before recording, ideally scripted out on paper first.
- 09:00 and 29:30 have no fixed script — read naturally from the direction given, don't try to force a verbatim performance of the placeholder text.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-L / EP3 — Lathe pieces to camera, Episode 3
<p class="meta">Sequence 62 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP3 00:45 · 06:00 · 10:00 · 17:00 · 28:00</p>

| | |
|---|---|
| **Location** | At the lathe |
| **Setup** | Five beats across the episode, one continuous sitting in script order. Two of these (00:45 and 06:00) have no fixed script yet — see Additional considerations |
| **Angle & camera** | Mid shot at the lathe; 06:00 needs the guard open and the carriage visible; 17:00 needs graphics prepared to cut to (`g2c_workpiece`) |
| **Audio to capture** | TX on Martin |
| **Target length** | ~420 words of fixed script across three beats, plus two outline-only beats to speak extemporaneously — budget roughly 4–5 minutes raw including the unscripted portions |

**Script — read through in this order**

**00:45 — What this episode is**
> *(No fixed blockquote in storyboard.html — speak from this direction rather than a verbatim script.)* Set expectations: no code, no soldering; by the end you can cut a feed pass, a right-hand thread, a left-hand thread, and thread up to a shoulder; and you'll know the one thing about an ELS that will otherwise scrap your first part.

**06:00 — Jogging, and the two behaviours of an arrow key**
> *(No fixed blockquote in storyboard.html — speak from this direction rather than a verbatim script.)* The rule, said once and demonstrated twice: if there's no stop on that side, the arrow jogs while you hold it and decelerates when you let go. If there is a stop, a single click runs to it under power and holds. Press the same arrow again and it cancels. Then the jog speed picker on OK — percentages of the configured full speed, and why 1% exists (creeping up on a datum).

**10:00 — End stops, and the asymmetry that's on purpose**
> Setting a stop is a click. Clearing one is a hold — and clearing both is a hold with a confirmation bar that fills for a second before it does anything.
>
> That's deliberate, and it's not me being precious about interface design. Setting a stop in the wrong place costs you five seconds. Clearing one loses a position you might have spent ten minutes finding with a dial indicator. Those two things should not cost the same gesture.

**17:00 — The bit nobody tells you: run-in and run-out**
> Last episode I explained that the controller starts its acceleration ramp *early*, so that it arrives at full speed exactly on the helix. That's the good news. Here's the bill.
>
> It can only start early if there is somewhere to start. The carriage physically has to travel that run-up distance *before* the thread begins — so you have to start the pass beyond the end of the thread. Not level with it. Beyond it.
>
> And it's the same at the other end, for exactly the same reason in reverse. When it approaches the stop it has to decelerate, and during that deceleration it is no longer following the helix. So the last few millimetres before the stop are not a good thread either.
>
> Run-in and run-out are the same distance, and it's the same sum: speed squared, over twice the acceleration.

**28:00 — Chip tray + outro**
> *(No fixed blockquote — direction and one quoted line only.)* Object: a part threaded into its shoulder with no relief groove — the mangled last three millimetres, macro. "This is what the run-out looks like when you don't give it anywhere to go." Then: next two episodes are how to build one. Ask for lathe make and model again.

#### Additional considerations

- 00:45 and 06:00 are genuinely outline-only in storyboard.html — treat the bullet points as beats to hit, not lines to memorise, and don't force a rigid performance where none has been written yet.
- 17:00 needs the `g2c_workpiece` animation ready to cut to and the real numbers (150 mm/s² accel, 40 mm/s ceiling, 2.54 mm leadscrew) confirmed against the running configuration before recording, per the bible's own note.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-B / EP4 — Bench pieces to camera, Episode 4
<p class="meta">Sequence 63 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP4 00:40 · 13:00 · 17:00 · 27:30</p>

| | |
|---|---|
| **Location** | At the bench |
| **Setup** | Four beats, one continuous sitting. **This is the second-longest session in the series** — see Additional considerations |
| **Angle & camera** | Mid shot at the bench; 00:40 needs the full component layout visible (shares framing with shot C-02); 13:00 needs both stepper generations side by side (shares objects with shot C-07); 17:00 needs the encoder mount (B-12) and printed input-path table nearby to reference |
| **Audio to capture** | TX on Martin |
| **Target length** | ~800 words of fully-scripted material (00:40, 13:00, and two flagged panels from 17:00) plus a long stretch of dense reference material in 17:00 read from the bible directly — budget 8–10 minutes raw, likely two sittings |

**Script — read through in this order**

**00:40 — What you're committing to, honestly**
> Five populated boards from JLCPCB, this run: eighty-one dollars, all in — shipping and the import VAT collected at checkout, so there's no customs card through the letterbox three weeks later. Call it sixteen dollars a board if you're ordering for more than yourself.
>
> The stepper is a StepperOnline closed-loop kit — motor, driver, cables, one box — about fifty-four pounds. Microstepping is set on the driver itself, with DIP switches, so there's nothing extra to buy there.
>
> The encoder is about eight pounds. Buy the **NPN version**, not the voltage-output one I've got — it matches the board as shipped, and mine doesn't. That's the first of three things I got wrong building this, and I'll own up to the other two later.
>
> The enclosure and keycaps aren't a gap in this budget — I designed them for this board specifically, the STEP files are in the repo, and printing a set costs under five pounds of filament for a fit that actually suits the thing, not a generic box. What's still yours to add is the power supply and the connectors, and that's a real "depends what's in your drawer" number — I'm not going to pretend otherwise by inventing one.

**13:00 — Choosing a stepper: buy closed loop**
> If you take one purchasing decision from this whole series, take this one. Buy a *closed-loop* stepper.
>
> An open-loop stepper that gets overloaded doesn't stop and doesn't complain. It silently skips steps and carries on. Which on a 3D printer means a layer shift and a wasted print. On a lathe it means the controller's idea of where the carriage is — which is the *only* thing keeping the tool out of the chuck — is now quietly wrong, and it will stay wrong.
>
> A closed-loop drive has an encoder on the motor. It knows when it has fallen behind, it corrects it, and when it can't, it raises an alarm on a wire. And this board reads that wire — the firmware halts the machine, latches the fault, and makes you acknowledge it, precisely because a fault that came and went still moved your carriage.
>
> *(Second part, same beat.)*
>
> I want to be careful here, because "don't buy from AliExpress" is lazy advice and I buy plenty from there — the spindle encoder on this machine came from AliExpress and it has been faultless.
>
> But the first closed-loop stepper and driver I bought there, I could not get to turn at all. It alarmed out immediately, every time, and it took a genuinely embarrassing amount of manual persuasion before it would run — and even then the motion was never good. I replaced it, and the replacement worked correctly out of the box.
>
> So the recommendation is narrow and it is specific to this one component: for the motor and driver, buy something with a real datasheet, an alarm output that means what it says, and somebody to email. That is worth paying for on the part that is holding a tool against a spinning workpiece.

**17:00 — Choosing and mounting the spindle encoder**
> ⚠️ **This beat has no single flowing blockquote in storyboard.html** — it's built from several dense panels with tables (encoder spec, the gearing correction note, the two-input-path table, a bench-test procedure, and the R17/R18 erratum). Bring the bible itself to this session rather than relying on this page alone. The one panel explicitly flagged as camera-ready is reproduced below in full — treat the rest of the beat's panels as reference material to speak from, not verbatim script.
>
> **The recommendation to give on camera:**
> Buy the NPN / open-collector variant and fit nothing extra. It is the population the board ships with, it needs no DNP parts, everything stays at 3.3 volts, and there is no over-voltage question left to reason about. The same listing sells an NPN version of the identical encoder at the same 1200 PPR, and the 5 V lower limit on this part means J2's rail runs it directly.
>
> Keep the voltage-output route in the video as the documented alternative — fit Q1, Q8, R15, R16 and remove R13/R14 — because some people will already own a push-pull encoder. But lead with NPN. It is the cheaper decision and the one with no footnotes.

**27:30 — Chip tray + outro**
> *(No fixed blockquote — direction and one quoted line only.)* Object: an earlier board revision — the one with the wrong pin assignment, or the socketed version. One sentence on what it got wrong. Hand-off: "There's no USB socket on this board. Next time: how to get firmware onto it, and how to tell it the truth about your lathe — which is the part that decides whether any of this works."

#### Additional considerations

- 17:00 is genuinely dense — this may be the point in the session where it's worth stopping to reset rather than pushing through fatigue, since it also has the most technical content to get right on camera (encoder voltage compatibility has real safety/hardware-damage stakes if mis-stated).
- 00:40's cost figures should be re-confirmed against CLAUDE.md at recording time (currency conversion, any updated lead-time information) rather than read as fixed forever.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-B / EP5 — Bench piece to camera, Episode 5
<p class="meta">Sequence 64 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP5 00:35</p>

| | |
|---|---|
| **Location** | At the bench |
| **Setup** | Single beat, straightforward sitting |
| **Angle & camera** | Mid shot at the bench |
| **Audio to capture** | TX on Martin |
| **Target length** | ~110 words, roughly 40–45 seconds |

**Script — read through in this order**

**00:35 — The stakes of this episode**
> This is the episode where you tell the controller what your lathe actually is. And I want to be blunt about it: this is the part that decides whether the thing works.
>
> Every number on that configuration page ends up multiplied into every movement the machine makes. Get the encoder count wrong and every pitch is wrong. Get the direction wrong and the carriage runs the other way, which on a lathe is not a cosmetic problem. Nothing checks these for you, because nothing can — only you know what's bolted to your machine.

#### Additional considerations

- Short and blunt by design — don't pad it.

#### Production notes

<div class="notes-box"></div>

<div class="pagebreak"></div>

### E-L / EP5 — Lathe piece to camera, Episode 5 close
<p class="meta">Sequence 65 of 65 &nbsp;&middot;&nbsp; Phase 6: Pieces to camera, last (Setup E) &nbsp;&middot;&nbsp; EP5 23:00 — the closing beat of the entire series</p>

| | |
|---|---|
| **Location** | At the lathe |
| **Setup** | The final piece to camera in the whole series — treat it accordingly |
| **Angle & camera** | Mid shot at the lathe |
| **Audio to capture** | TX on Martin |
| **Target length** | ~185 words of fixed script, plus two genuine content gaps to fill before recording — see Additional considerations |

#### Script

> This is GPLv3. Fork it, change it, put it on a completely different lathe — that's the whole point of publishing it this way.
>
> ⚠️ If I started again, *[what you'd actually change — be specific]*. What's still on the list: the run-up calculation the panel should be doing for you instead of you doing arithmetic — that one's mentioned back in episode three — and *[anything else genuinely queued]*.
>
> Here's the actual ask. If you build one of these, tell me what lathe it went on, and tell me what broke. Every machine is a little different, and that's the only way this gets better for the next person who tries it.
>
> And one last credit, because it's earned one: Not An Engineer, and Jack From Scratch. Neither of them had to help a stranger with a video and a GitHub repo. Links are below both.

#### Additional considerations

- ⚠️ **The bracketed lines are genuine, unfilled gaps** — storyboard.html is explicit that "what I'd change" and "what's still on the list" aren't sourced anywhere in the document and need Martin's own answer before this is read on camera. Only the run-up-calculation item (from EP3's 17:00 beat) is a confirmed queued item; the rest is Martin's call, not something to improvise live.
- This is the last piece to camera shot in the entire five-episode series — worth treating as its own small occasion rather than squeezing it in at the end of a long day.

#### Production notes

<div class="notes-box"></div>
