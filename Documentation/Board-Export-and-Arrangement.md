# Board export and arrangement

Library 0.12.0 development preview · Jago Board

## Export at higher resolution without adding a square

1. Choose **Export image…** or press **Ctrl+E**.
2. Choose **All references · tightly cropped**, **Selected references**, **Artboard**, or **Current view**.
3. Choose **2048**, **4096**, or **Largest safe size**. These describe the longest edge, not a square canvas.
4. Save the image. The button confirms the exact pixel dimensions and format.

A wide board stays wide. For example, a 960 × 213 reference arrangement exports at approximately **4096 × 910**, rather than inside a 4096-pixel square. Whole-pixel rounding can leave a very thin fractional-pixel edge.

**Keep area proportions** starts enabled. Edit width or height and the other dimension updates. To deliberately fit the references inside a different shape, turn this off and enter both dimensions. A custom canvas fits the artwork without stretching and may include empty space.

**Outer margin** adds a percentage of the area’s shorter edge. Zero gives a tight export. The preview has the same proportions as the output and excludes selection handles, the view grid and artboard guide. Unloaded thumbnails may appear as blocks in the layout preview; export renders source artwork and reports missing or undecodable sources.

PNG and WebP support transparency. JPEG uses the board background. Quality is shown only for JPEG/WebP. Format, margin and transparency are remembered for the board; export area starts at all references so a previous selection or viewport does not silently limit the next export.

The largest output is bounded by **8192 pixels per edge and 32 million pixels total**. Higher output resolution does not add detail to a small source. The current image loader limits raster decoding to 4096 pixels per edge; a larger composition can still benefit from multiple image sources and sharp rendered text.

## Use an artboard when the frame matters

Choose **More → Artboard frame…** in focused Board, or the Artboard action in full tools. Fit the frame to your references, choose a square or 16:9 shape, or enter position and dimensions. These are board coordinates, separate from export resolution.

The dashed guide shows the frame on the board. References can stay outside it. Choose **Artboard** in export to crop to it; an outer margin stays empty rather than revealing references outside the frame. The frame is saved with the project and can be removed or undone.

**Current view** captures the visible board area at the time the export dialog opens. **Selected references** recalculates the tight bounds of the selection. Ctrl+Shift+E opens that selection route directly.

## Arrange with less guesswork

Choose **Arrange…**. Its preview does not move the live board. Choose scope, layout, reference sizes, ordering and spacing; **Apply arrangement** makes one undoable change. Close the dialog to cancel.

Default spacing is **8 board pixels**. Quick choices offer touching (0), close (4), small (8) and airy (16). The board remembers these choices for later arrangements and incoming image batches. Locked references stay put; arranged items move beside other items when needed to avoid overlap.

Matching width, height or visual area preserves image proportions. Compact pack, square, wide, tall, row, column and grid serve different layouts. These arrangement shapes do not force the export to that shape.

Image items no longer reserve an invisible border. Zero-spacing image frames can meet directly; whitespace inside the original artwork remains part of that artwork.

## Refine without surprises

- Double-click a reference to focus it. Double-click empty space or press F to fit the board. Shift+F focuses the current selection.
- **Keep image proportions** in the selection inspector keeps width and height linked. Turn it off only when intentional stretching is wanted. Drag-resizing keeps proportions unless Shift is held.
- Selecting an item does not silently raise its stacking order. Use **Bring to front / Send to back**, the right-click menu, or ] / [. These edits are undoable.
- Drag on empty space for rectangle selection; Shift adds. Space+drag or middle mouse pans. Existing edge/centre snapping and Alt to bypass it remain available.
- Save project keeps the editable board, artboard frame, layout preferences and assets. Automatic recovery remains a separate local safety copy.

Closing an export before it finishes cancels the pending save. Changing project or board during rendering also rejects the stale result. Busy settings are disabled while rendering.

## Research references

[PureRef export](https://www.pureref.com/handbook/2.0/saveload/) distinguishes scene and selected-image exports. [PureRef arrangement](https://www.pureref.com/handbook/2.0/images/organize/) distinguishes arrangement, normalization and stacking. [BeeRef](https://beeref.org/) provides a useful reference for a minimal, unobtrusive reference-viewing workflow. These informed the interaction review; no third-party source or artwork was incorporated.


## Arrangement preview and navigation
Before/After compares the current and proposed layout without changing the board. Thumbnail previews load at most four small images at once; unavailable images remain position blocks. Apply is one undoable change. Match average width, height or visual area independently of layout. Compact pack is a best-fit heuristic, not a guarantee of mathematically minimal area.

Manage boards next to the picker supports name search and multiple removal, with confirmation. It removes board references only, retaining Library assets and originals. Panning, zooming and fitting save a small local view record rather than marking artwork edited. Recovery of artwork waits while a gesture is active, then resumes when idle. Save project remains the portable copy.


## Browse boards
Library’s Browse boards and Board’s Manage boards share the same locally cached thumbnails. Open board is separate from selection for removal. Editing board artwork invalidates its preview; panning and zooming do not. Previews load as cards come into view, one board at a time, with a bounded local cache. Unavailable source images may appear as blocks; Open board still works. Thumbnails are local convenience data, not new assets or individual Explorer files. Save project while in Board to embed the current Board cover in the saved .jagolibrary file. Explorer thumbnails require the suite’s optional Explorer thumbnail provider.

PDF previews now scroll through consecutive pages. Page jumps, search, copy/select text, zoom and rotation remain available. Single-page view opens the previous focused reader. Full-size page canvases load lazily and are bounded to six retained pages.
