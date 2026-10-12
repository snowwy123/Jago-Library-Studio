# Offline reconstruction engine

This is a separate local executable, not a hosted conversion service. The wrapper is AGPL-3.0-or-later. Its dependencies retain their own licences, including PyMuPDF and MuPDF under AGPL.

On Windows x64 with Python 3.12, create a virtual environment in this folder, install requirements.txt, and run PyInstaller with Jago-PDF-Engine.spec from this folder. Copy the contents of dist/Jago-PDF-Engine into the studio's Tools/PDF folder. The whole directory is required, including _internal; users do not need Python installed.

The source archives in Upstream include the corresponding PyMuPDF, MuPDF and pdf2docx sources. The original build references are retained there. Pinned dependency versions and their licence notices accompany this release. The main Windows app sends a temporary PDF to the helper and receives a Word document; no network operation is involved.

OCR is provided separately by the pinned browser worker files in Tools/OCR. English language data is included. Upstream package archives and Apache licence notices accompany those files.
