# A collected library you can carry

## References versus copies

References point at files on their original C/D/E drive, folder or USB. Disconnecting that drive makes those originals unavailable. Collected copies are independent files under your Library home. Collecting never deletes or changes the source. Tags, favourites and font categories copy to the collected entry; both entries remain in the catalog. Browse All collected files to avoid seeing both versions together.

Choosing a Library home changes Add files / Add folder to collected-copy mode. Change Add files as back to References when desired. Select a destination before adding. Keep source subfolders preserves the folders beneath a referenced root; unchecked places selected files directly into the destination. Unsupported formats are still cataloged as specialist files, with an honest missing-preview state.

Collection stops between files, not halfway through a file. Files already copied remain. Errors are reported. Identical source/destination is skipped; collisions receive numbered filenames. Collection/export uses ordinary background file copying rather than passing large file binaries through the web interface.

## Saving and moving

Save catalog writes .jago-library/catalog.jagolibrary beside collected files, preserving a previous catalog backup. Save project downloads a separate editable backup. Automatic browser recovery is limited; neither recovery nor merely indexing files replaces these explicit saves.

Portable ZIP saves the current catalog and includes currently indexed collected files plus the library identifier. Embedded artwork is in the catalog. It does not include external file references or automatically extract installed fonts. Collect referenced files you need before exporting. Respect the licences attached to your own assets when sharing a library.

Extract the ZIP and use Choose / connect library home on its Jago-Library folder. Its stable identifier reconnects collected paths even after the library moves to another drive. Open saved catalog restores designs, favourites and tags; Keep current collection merges its indexed files into your current project. Restoring references does not initiate collection.

Remove from library removes a catalog entry, not a physical file. Subfolder creation is supported; physical renaming, moving and deletion inside the app are not implemented. Use Windows Explorer to organise files, then reconnect/reindex the home. Missing-file repair and deduplication remain planned.

## Practical limits

- Up to 2,000 files per scan/collection, five nested folder levels; up to 200 subfolders appear in the destination list. Scans skip inaccessible entries, junctions and files larger than 2 GB. A capped scan is reported; split very large libraries into deliberate batches.
- ZIP includes the currently indexed collected files, not unindexed files beyond scan limits. Empty subfolders are not guaranteed to survive ZIP export.
- Browser imports: 8 MB per file, 30 MB embedded source data; project JSON: 45 MB. Windows catalogs can reference larger files without embedding them.
- Native preview/production source reads are capped at 20 MB, font reads at 8 MB. Larger documents can be cataloged, copied, exported and opened in their original app; large-file preview coverage varies.
- The managed home is a standard filesystem folder. Choose a durable destination with enough free space, rather than a removable drive you intend to disconnect.

See FILE-SUPPORT.md for preview behaviour and QUALITY.md for checks.
