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

The desktop project copies these to `ModernFrontend/nine-lives-art/` when building or publishing, so the packaged React WebView2 cabinet serves them offline under the exact virtual host. No duplicated source binary art is required in `frontend/`.

The WPF source for the retired Nine Lives game window and its reel/canvas effects has been removed. `NineLivesArtwork` and the encoded `NineLivesArtData.Background*` remain **only** to render the WPF emergency lobby's existing Nine Lives preview; they are not part of the playable modern game and may be removed when the lobby fallback is retired.
