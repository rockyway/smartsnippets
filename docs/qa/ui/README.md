# UI QA screenshot index

All images are synthetic fixture data. Paths below are run directories under this folder. Historical superseded runs were pruned from the current tree; their original commits remain in git history. The DPI-cropped `ed8ec46e19ef4ae28b51e82226b016d0` run was removed and is not evidence.

## Last complete pre-DPI-fix matrix

These runs used the presenter build before the monitor-DPI correction. Requested 900×600 clamped to 960×720; they do **not** show 900×600. Each contains Snippets, Folders, Settings and popup filled/search/empty at 1280×800 and 960×720 (popup itself is 640×480).

| Run | Theme | Text size |
|---|---|---|
| `2792f312c46e487bbad3bd17e5062860` | Light | Default |
| `872bdea640574e4f8828fc3b374f591f` | Dark | Default |
| `7da3318226bb4a409ba9423c6171db36` | Light | Custom 10 |
| `f9b96ff389b34d48962c30fc91d43bf9` | Dark | Custom 10 |
| `ae8bc93e8cd842c185dc2bf4ca25504e` | Light | Custom 32 |
| `fd787e0b7bc14554afc77d3880835c69` | Dark | Custom 32 |

## Latest DPI-fix smoke and minimum

- `be71038f9ea045f2968e3adf7f1842df` — light Snippets 1280×800 and Snippets/Folders/Settings at queried minimum 640×480; a resize request of 100×100 clamped to 640×480 at 96 DPI.
- `8c96193a912241999c56a9ff4f9a059e` — dark Folders at requested 900×600, dark filled popup, and all three pages at 640×480; below-minimum clamp also verified.

## Retained key before/after evidence

- `fa61080307504c77be600de2d04ca451` — before Folders Create clipping fix, including 900×600.
- `741306280fad4ad98f2c6dba84802be9` — after Folders Create clipping fix, including 900×600.
- `c99a529829854285bdbdad03061120d5` — before popup presenter fix (native title bar), after background and alignment fixes.
- `b293c12ed4134f9988f43eac18945e1e` — first partial presenter recheck; borderless popup at 1280×800; initially aborted on 900×600 clamp.

Context menus, real hotkey/insertion and native picker workflows are not covered by these screenshots.
