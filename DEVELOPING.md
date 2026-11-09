# Building Jago Library

Use Windows x64 and Microsoft Edge WebView2 Runtime. Install Node.js for the browser build and the .NET Framework C# compiler for the Windows host.

From this repository, run **Build.ps1** in PowerShell. The wrapper uses the included WebView2 SDK. Edit the files under **Source**, then rebuild; generated HTML is build output.

Keep licence notices with redistributed code and binaries.

The PDF helper has separate build instructions in **Source/PDF-Engine/BUILD.md**. Keep its complete Tools/PDF runtime beside the released app. The helper and bundled components retain their individual licences. Upstream source archives use Git LFS; use a Git client with LFS to publish this repository.


## Document hub development

Runtime binaries under Tools/Documents are optional build dependencies, not application source. Put an unmodified LibreOffice administrative image at `Tools/Documents/LibreOffice`, retaining its original licences, or connect an installed renderer in the app. Document rendering runs locally and only when requested.

Run `node --test Source/Tests/source-model.test.cjs` for reconciliation regressions.

The browser checks use Playwright and Microsoft Edge. Set `PLAYWRIGHT_MODULE` if Playwright is not installed locally. The fixture generator needs Python packages python-docx, python-pptx, openpyxl and Pillow. Set `JAGO_TEST_WORK` to an empty test output directory, generate fixtures, then run the browser or Windows probe scripts under Source/Tests. Native probes run the app with `--test` and isolated profiles, and exercise real filesystem changes in that test directory.

LibreOffice reference: https://help.libreoffice.org/latest/en-GB/text/shared/guide/start_parameters.html
DOCX preview reference: https://github.com/VolodymyrBaydalka/docxjs
Pinned dependency versions, original licences and download checksums are recorded under Notices/Documents.

## Project hub checks

Run `node --test Source/Tests/project-model.test.cjs Source/Tests/source-model.test.cjs` for collection, dependency, relocation and source reconciliation regressions.

`Source/Tests/browser-projects.cjs` exercises the Project page, notes, pins, covers, nested collection undo, package roundtrip and compact layout. Set `PLAYWRIGHT_MODULE`, `JAGO_TEST_WORK` (the document fixture directory's parent) and `JAGO_PROJECT_TEST_WORK` (output directory) as needed. `Source/Tests/native-projects.py` uses the Windows app's isolated test profile to check persistent working copies, larger original files, soundtrack dependencies, extraction, rescanning and document search. Generate document fixtures using the existing fixture script first.

Project organisation is separate from the design undo stack. Portable packages contain copied originals and an integrity manifest; native imports extract only declared assets into a new owned project directory. Runtime document dependencies remain outside Git.

Project review checks: `node Source/Tests/browser-review.cjs` and `python Source/Tests/native-review.py`. Set `JAGO_PROJECT_TEST_WORK` for isolated evidence and `JAGO_TEST_WORK` to the document fixture parent. `JAGO_FONT_PROJECT` optionally tests an existing project; otherwise a three-font fixture is used. Windows tests launch hidden isolated profiles.

## Creative workflow checks

Run `node --test Source/Tests/workflow-model.test.cjs` for replacement, usage, verified consolidation and bounded lookalike matching. Playwright checks are `browser-workflow.cjs`, `browser-background.cjs` and `browser-performance.cjs`. The performance fixture contains 60,000 synthetic entries; it measures grid overhead and preview scheduling, not real image decode speed.

`native-workflow.py` checks folder watching, template metadata, verified delivery reopening and queued Office rendering. Set `JAGO_DOCUMENT_ENGINE` to a LibreOffice soffice.exe path for the isolated test profile. Compile `source-scanner-test.cs` with `Source/Windows/SourceScanner.cs` for cancellation checks; pass a fresh fixture directory to the resulting executable.

## Creative hub checks

Run `node --test Source/Tests/creative-model.test.cjs` for dependency, planning, arrangement and preflight rules. Browser interaction checks are `Source/Tests/browser-creative-hub.cjs` and `Source/Tests/browser-reference-tools.cjs`; they require Playwright and Edge. `Source/Tests/native-creative-hub.py` verifies Windows shelf packaging, brand storage and backup receipts using an isolated test profile.

`Source/Tests/native-library-home.py` verifies editable folder setup, remembered homes, streaming copy verification, identical-file reuse, collection nesting and project-reference updates. Set `JAGO_HOME_TEST_WORK` to an isolated test directory.

## Native image previews

The Windows build compiles and embeds ImagePreviewWorker.cs automatically; no separate image tool installation is needed. Raster display copies are decoded only on request in a short-lived process, with a 1 GiB memory limit and 20-second timeout. Originals are referenced unchanged. Cached PNG display copies use 640, 2048 or 4096 pixels on the longest edge and a 256 MiB disk budget. Source limits are 512 MiB and 256 megapixels for this path. Browser embedded limits and non-image read limits are separate. Export continues to use bounded working images, not unrestricted original-resolution raster rendering.

Run `python Source/Tests/native-smart-previews.py` after building on Windows. Requires Pillow and NumPy; generates a real 51.2MP / 153MB PNG and small alpha, EXIF and rejection fixtures. Tests native file references, preview sizes, cached-file regeneration, exportability, unchanged originals, alpha and upright photo dimensions. Set JAGO_IMAGE_TEST_WORK to an existing output parent and JAGO_NATIVE_APP to test a packaged executable. Temporary test output is retained at the reported path for inspection.


Board gallery / continuous PDF checks: `node Source/Tests/browser-board-gallery-pdf.cjs` and `python Source/Tests/native-board-gallery-pdf.py`. Set `JAGO_PROJECT_TEST_WORK` to an evidence folder. Fixtures are generated locally; the native probe uses a separate profile. Checks include persistent thumbnails, artwork invalidation, direct open, page scrolling/jumps/search, bounded renders and text selection. Existing Explorer thumbnail setup is separate; the save cover remains embedded PNG.
