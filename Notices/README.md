# Credits and licences

The original Jago Library application source is MIT licensed. Assets you import keep their own licences. The three bundled fonts use the included SIL Open Font Licence. Microsoft WebView2 hosts the Windows edition; its licence and notices are embedded in the application.

PDF rendering uses Mozilla PDF.js (Apache 2.0); PDF assembly uses pdf-lib (MIT). Their notices are in Source/Vendor.

The separate **Tools/PDF** process is the offline reconstruction engine. Its Jago wrapper is AGPL-3.0-or-later and it includes pdf2docx 0.5.13 (MIT), PyMuPDF / MuPDF 1.28.2 (AGPL), python-docx, NumPy, OpenCV, fontTools, lxml and the Python runtime. Individual licence texts are under PDF-Engine. Corresponding PyMuPDF, MuPDF and pdf2docx source archives are included under Source/PDF-Engine/Upstream, alongside the wrapper and build instructions. Redistribution of the engine must retain these notices and corresponding source availability.

Offline scan recognition uses Tesseract.js 6.0.1, Tesseract.js-core 6.1.2 and English tessdata_fast, under their included Apache 2.0 licences. It is entirely local; no remote OCR service is used. Source package archives are included under Source/PDF-Engine/Upstream.

These software licences do not claim ownership of your creations or converted documents. Imported fonts, images and documents retain their original rights.
