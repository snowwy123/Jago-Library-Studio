# Preview and file support · 0.2.0

| Files | Behaviour |
| --- | --- |
| PNG, JPEG, WebP, GIF, BMP | Thumbnails and production artwork. Animated images export as a still frame. |
| SVG | Image preview and raster production export. External resources may not work offline. |
| TTF, OTF, WOFF, WOFF2 | FontFace preview without installation; up to 64 imported faces per session. Variable axes inspected from supported SFNT/WOFF tables. WOFF2/container metadata can remain unknown rather than being mislabelled static. Production retains weight; other axes are preview-only. |
| Installed fonts | Windows family list, machine/user registry origin where available, available styles and variable metadata where the font file can be matched. Machine-installed does not mean Windows-bundled. Browser offers common-family previews. Bold/italic can be synthetic. |
| Audio / video | Browser playback where the codec is supported; no editing/transcoding. Native source reads limited to 20 MB. |
| PDF | On-demand browser document viewer; no page import/editing. |
| AI | Embedded PDF-compatible data recognised when present; otherwise catalog/sidecar/Windows thumbnail or manual attached preview. No Illustrator object/layer editing. |
| PSD / PSB | Embedded RGB JPEG resource thumbnail when supported; sidecar/Windows thumbnail or attached preview otherwise. No Photoshop layer parsing. |
| Affinity formats | Bounded search for a public embedded PNG in the first/last 8 MB, then Windows thumbnail provider where available. This succeeds for some documents, including a tested large local .afphoto, but is not a native-format decoder or a guarantee for every document/version. |
| OpenRaster, Krita, Procreate ZIP containers | Supported stored/deflated preview/merged-image entries where present, with an 8 MB extraction bound. Not every Procreate archive has a compatible preview. |
| Other specialist / unknown files | Metadata, tags/favourites, associated-app opening, Windows document thumbnail where installed, sidecar or manually attached rendered image. No fake document preview using a file icon. Preview artwork can enter Production. |

Windows indexes up to 2,000 ordinary files per root and five nested levels, skipping inaccessible files, reparse points and files above 2 GB. Unknown formats remain catalog entries. Native source reads for actual browser playback/production are limited to 20 MB; native font reads to 8 MB. Larger files can still be collected and exported.

Browser imports allow 8 MB per file and 30 MB total embedded data. Manual preview attachments allow 2 MB. Recovery allows 4.5 MB JSON; save projects/catalogs explicitly. Project JSON limit: 45 MB. Layout sides: 64–4,096 px. Production refuses images above 40 megapixels.

Native thumbnails use two background workers and a 320 px raster target. Embedded specialist thumbnails are cached against path/modification time. Library pages show at most 48 cards; font picker at most 60 matching faces. Resolved native data and production images use bounded caches. Production preview fits within 900 px while exports render full document dimensions. This does not promise instant scanning or decoding on every drive.

Preview research and primary references are recorded in PREVIEW-RESEARCH.md. Managed-library storage/export is explained in LIBRARY-HOME.md.
