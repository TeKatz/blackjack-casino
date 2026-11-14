# Nine Lives — source artwork shared with React

The eight original full-resolution 1024 × 1024 JPG slot symbols are retained unchanged in this folder:

| Filename | NineLivesSymbol |
| --- | --- |
| black-cat.jpg | BlackCat |
| white-cat.jpg | WhiteCat |
| tabby-cat.jpg | TabbyCat |
| fishbone.jpg | Fishbone |
| yarn.jpg | Yarn |
| bell.jpg | Bell |
| wild.jpg | Wild |
| bonus.jpg | Bonus |

The NativeShell project copies these to `ModernFrontend/nine-lives-art/` when building or publishing, so the packaged React WebView2 cabinet serves them offline under the exact virtual host. No duplicated source binary art is required in `frontend/`.

The retired WPF Nine Lives window and its WPF-only emergency-lobby preview assets are not part of the current NativeShell. React owns the visual cabinet, while C# remains authoritative for each spin, outcome, payout, bonus and persisted statistics.
