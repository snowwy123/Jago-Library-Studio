# SPDX-License-Identifier: AGPL-3.0-or-later
"""Page-positioned editable Word output for designed PDFs."""
import io,re
from statistics import median
import pymupdf
from docx import Document
from docx.shared import Pt
from docx.enum.section import WD_SECTION_START
from docx.oxml import parse_xml
from xml.sax.saxutils import escape
from pdf2docx.common.docx import add_float_image

def family(name):
    name=re.sub(r'^[A-Z]{6}\+','',name)
    for known,value in [('TimesNewRoman','Times New Roman'),('Arial','Arial'),('Cambria','Cambria'),('Calibri','Calibri'),('CourierNew','Courier New'),('Helvetica','Arial')]:
        if name.startswith(known):return value
    return re.sub(r'[-,]?(BoldItalic|BoldOblique|Bold|Italic|Oblique|Regular|PSMT|MT)$','',name)

def convert_positioned(source,destination):
    pdf=pymupdf.open(source)
    if pdf.needs_pass:raise ValueError('Unlock this PDF before converting it.')
    if len(pdf)>100:raise ValueError('Keep original positions supports up to 100 pages at once.')
    doc=Document();total=0
    doc.styles['Normal'].paragraph_format.space_after=Pt(0)
    fonts={}
    for number,page in enumerate(pdf):
        if page.rotation:page.remove_rotation()
        section=doc.sections[0] if number==0 else doc.add_section(WD_SECTION_START.NEW_PAGE)
        section.page_width=Pt(page.rect.width);section.page_height=Pt(page.rect.height)
        section.top_margin=section.bottom_margin=section.left_margin=section.right_margin=Pt(0)
        section.header_distance=section.footer_distance=Pt(0)
        p=doc.add_paragraph()
        p.paragraph_format.line_spacing=Pt(1);p.paragraph_format.space_after=Pt(0)
        for f in page.get_fonts():
            try:fonts[f[3].split('+')[-1]]=pymupdf.Font(fontbuffer=pdf.extract_font(f[0])[3])
            except Exception:pass
        raw=page.get_text('rawdict')
        lines=[line for b in raw['blocks'] if b['type']==0 for line in b['lines']]
        background=pymupdf.open();background.insert_pdf(pdf,from_page=number,to_page=number);bp=background[0]
        for line in lines:
            if line['dir']!=(1.0,0.0):continue
            for s in line['spans']:
                if ''.join(c['c'] for c in s['chars']).strip():bp.add_redact_annot(s['bbox'],fill=False)
        bp.apply_redactions(images=0,graphics=0,text=0)
        scale=min(2,((12000000/(page.rect.width*page.rect.height))**.5))
        pix=bp.get_pixmap(matrix=pymupdf.Matrix(scale,scale),alpha=False)
        add_float_image(p,io.BytesIO(pix.tobytes('png')),page.rect.width,pos_x=0,pos_y=0)
        background.close()
        for line in lines:
            if line['dir']!=(1.0,0.0):continue
            for s in line['spans']:
                text=''.join(c['c'] for c in s['chars'])
                if not text.strip():continue
                size=s['size'];x=s['bbox'][0];y=s['origin'][1]-size*.91
                width=max(2,s['bbox'][2]-x);spacing=0
                f=fonts.get(s['font'])
                if f and len(text)>1:
                    spacing=(width-f.text_length(text,fontsize=size))/(len(text)-1)
                    if abs(spacing)<.05:spacing=0
                    spacing=max(-2,min(8,spacing))
                color=f"{s['color']:06x}";flags=s['flags'];font=escape(family(s['font']),{'"':'&quot;'})
                props=f'<w:rFonts w:ascii="{font}" w:hAnsi="{font}"/><w:sz w:val="{round(size*2)}"/><w:color w:val="{color}"/><w:spacing w:val="{round(spacing*20)}"/>'
                if flags&16:props+='<w:b/>'
                if flags&2:props+='<w:i/>'
                xml=f'''<w:pict xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office"><v:shape id="JagoText{number}_{total}" type="#_x0000_t202" stroked="f" filled="f" style="position:absolute;margin-left:{x:.3f}pt;margin-top:{y:.3f}pt;width:{width+12:.3f}pt;height:{size*2:.3f}pt;z-index:251659264;mso-position-horizontal-relative:page;mso-position-vertical-relative:page"><v:textbox inset="0,0,0,0"><w:txbxContent><w:p><w:pPr><w:spacing w:before="0" w:after="0" w:line="{round(size*24)}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{props}</w:rPr><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p></w:txbxContent></v:textbox></v:shape></w:pict>'''
                p.add_run()._r.append(parse_xml(xml));total+=1
    doc.save(destination);pdf.close()
    return {'positionedTextBoxes':total,'layout':'positioned'}

