# Icon sources

`..\PCUserDetection.ico` is a binary, and this folder is what it was built
from, so it can be changed rather than only replaced.

## The four variants

The mark is a face inside camera framing corners, drawn once and coloured four
ways. Each colour is a state the app already has a palette entry for, so the
icon and the window agree on what a colour means:

| Variant     | Colour    | Matches                        |
| ----------- | --------- | ------------------------------ |
| `idle`      | `#2B5CE2` | `Theme.Accent`, light theme    |
| `verified`  | `#0D7A4F` | `Theme.Success`, light theme   |
| `anonymous` | `#BF2C30` | `Theme.Danger`, light theme    |
| `offline`   | `#8A93A3` | `Theme.TextMuted`, dark theme  |

All four are exact matches. The grey is the odd one out in coming from the
dark palette; the light theme's `TextMuted` is `#677080`, which is too dark to
read as "switched off" next to the other three.

**`idle` is the app icon.** The other three are drawn but unused: nothing in
the app shows a status glyph today, and they are kept here so that whatever
does show one later does not have to invent its own artwork.

## What is in here

- `svg\` — the vector masters, all four variants. Each has two drawings:
  `-256` for large sizes, and `-small`, which thickens the strokes and enlarges
  the face so the shape survives being 16 pixels wide. Editing starts here.
- `png\` — `idle` rasterised at the eight sizes the `.ico` carries. These are
  the exact inputs the committed icon was built from.
- `make-ico.ps1` — packs `png\` into the `.ico`.

## Rebuilding the icon

From this folder:

```powershell
powershell -ExecutionPolicy Bypass -File make-ico.ps1
```

That overwrites `..\PCUserDetection.ico` from `png\`, and needs nothing
installed. Rebuild the project afterwards; the icon is embedded at compile
time, so an edited file alone changes nothing.

Note the gap: nothing here rasterises SVG to PNG, because no such tool can be
assumed present. Changing the artwork means editing the SVG, exporting the
eight PNGs with whatever tool you have (Inkscape, a browser, an online
converter), and then running the script. The eight sizes are 16, 20, 24, 32,
48, 64, 128 and 256, named `pcud-idle-<size>.png`.

## How it reaches the screen

Two separate paths, both in `PCUserDetection.csproj`:

- `ApplicationIcon` puts it in the executable's Win32 resources, which is what
  Explorer, a shortcut, and the taskbar read before the window exists.
- `EmbeddedResource` puts the same file in the assembly, where `AppIcon` reads
  it as a stream and hands it to the window. This path keeps every size, so the
  title bar and the taskbar button each get one drawn at their size.
