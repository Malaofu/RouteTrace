# Project status

**Phase:** Workspace development

**Current PBI:** PBI-074 — Context actions and presentation settings

**Last updated:** 2026-08-16

## Current state

- PBI-000 through PBI-074 are complete.
- Explorer nodes expose a pointer-positioned context menu through right-click
  or Shift+F10, with outside-click dismissal and no persistent overflow button.
- Right-click selects an unselected target first; Ctrl-click adds or removes
  nodes so presentation commands can apply to the complete selected set.
- Document actions include activate, focus, visibility, colour, GPX download,
  and close; child nodes expose only applicable focus and presentation actions.
- Track, segment, route, waypoint-group, and waypoint presentation overrides
  inherit from parents; showing a parent clears hidden descendant overrides,
  and child colours can be reset to their effective parent colour.
- Explorer colour strips show each node's effective colour rather than always
  repeating the document colour; the redundant Select all action is removed.
- Info dialogs edit supported document, track, route, and waypoint text while
  preserving the remaining canonical GPX content and surviving reloads.
- Version 3 IndexedDB workspace records persist presentation overrides while
  retaining migration support for versions 1 and 2.
- GPX export keeps presentation settings out of canonical route data.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 62 passed, zero failed.
- Published Playwright tests: 14 passed, zero failed, including Info reload,
  inherited-colour reset, and keyboard context-menu coverage.
- Formatting and TypeScript checks: passed.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-074.
