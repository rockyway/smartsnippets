# UI QA screenshot index

All images are synthetic fixture data. Paths below are run directories under this folder. Historical superseded runs were pruned from the current tree; their original commits remain in git history. The DPI-cropped `ed8ec46e19ef4ae28b51e82226b016d0` run was removed and is not evidence.

## Latest review-fixes verification (build uiqa-fixes-06)

All PNGs in these runs were read. Default-theme runs contain Snippets/Folders/Settings and popup filled/search/empty at 1280×800, 900×600, and 640×480. Custom-size runs contain Snippets at those sizes. Window dimensions in filenames identify the requested main-window iteration; popup images are 640×480.

| Run | Theme | Text size | Scope |
|---|---|---|---|
| `7b7fbe2d2e2540e28c09fac4f0eb0202` | Light | Default | Pages and popup states |
| `c5202fff83a74947851331b183e61861` | Dark | Default | Pages and popup states |
| `559b579bb93f4cbbba6bdfee3814d322` | Light | 10 | Toolbar / Snippets |
| `6919483b5df745ac9f1268113cc04d52` | Light | 32 | Toolbar / Snippets |
| `848a156e1b3c4d6588f5d39c2b1245ac` | Dark | 10 | Toolbar / Snippets |
| `5b532c7bd8be4ababb32dceb5aedad10` | Dark | 32 | Toolbar / Snippets |
| `dc207f62d37b48bda278e8e8a121e640` | Light | Default | Below-minimum clamp and minimum pages |

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
