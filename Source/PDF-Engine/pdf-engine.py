# SPDX-License-Identifier: AGPL-3.0-or-later
"""Offline Jago PDF conversion helper. No network or document uploads."""
import argparse
import json
import sys
from pathlib import Path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('source')
    parser.add_argument('destination')
    parser.add_argument('--layout',choices=['flow','positioned'],default='flow')
    args = parser.parse_args()
    from pdf2docx import Converter
    source = Path(args.source)
    destination = Path(args.destination)
    if source.stat().st_size > 64_000_000:
        raise ValueError('Convert a PDF under 64 MB.')
    extra={}
    if args.layout=='positioned':
        from positioned_word import convert_positioned
        extra=convert_positioned(source,destination)
    else:
        converter = Converter(str(source))
        try:
            converter.convert(str(destination), multi_processing=False)
        finally:
            converter.close()
    from docx import Document
    doc = Document(str(destination))
    summary = {'paragraphs': len(doc.paragraphs), 'tables': len(doc.tables),
               'images': len(doc.inline_shapes), 'bytes': destination.stat().st_size}
    summary.update(extra)
    print(json.dumps(summary))

if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
