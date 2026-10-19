from pathlib import Path
from docx import Document
from docx.shared import Inches,Pt,RGBColor
from PIL import Image,ImageDraw
import zipfile,os
from pptx import Presentation
from pptx.util import Inches as SlideInches
from openpyxl import Workbook
work=Path(os.environ.get('JAGO_TEST_WORK',str(Path(__file__).parent/'Run'))); fixtures=work/'Fixture';fixtures.mkdir(parents=True,exist_ok=True)
im=Image.new('RGB',(900,210),'#244a47');d=ImageDraw.Draw(im)
for x in range(30,870,80):d.ellipse((x,40,x+65,105),fill='#dfb653');d.line((x+32,108,x+32,190),fill='#eff0df',width=4)
im.save(fixtures/'banner.png')
doc=Document();section=doc.sections[0];section.header.paragraphs[0].text='JAGO Â· Creative project documents';section.footer.paragraphs[0].text='Document hub test Â· Original document retained'
doc.add_heading('Creative project brief',0);doc.add_picture(str(fixtures/'banner.png'),width=Inches(6))
doc.add_paragraph('A document preview should preserve the hierarchy, artwork and table below. This fixture deliberately includes two pages, a header and a footer.')
doc.add_heading('Deliverables & budget',1)
table=doc.add_table(rows=1,cols=3);table.style='Light Shading Accent 1'
for c,t in zip(table.rows[0].cells,['Deliverable','Format','Budget']):c.text=t
for vals in [('Illustrated poster','PDF / PNG','Â£240'),('Animated identity','GIF / WebM','Â£360'),('Sound cues','WAV','Â£180')]:
    for c,t in zip(table.add_row().cells,vals):c.text=t
p=doc.add_paragraph();r=p.add_run('Check typography: ');r.bold=True;r=p.add_run('italic and coloured text');r.italic=True;r.font.color.rgb=RGBColor.from_string('28766A')
doc.add_page_break();doc.add_heading('Page two: schedule',0)
for text in ['Gather assets in Library.','Review the artwork and typefaces.','Export a PDF proof.','Collect project files for handoff.']:doc.add_paragraph(text,style='List Bullet')
doc.add_paragraph('This second page must remain separate in the preview and exported PDF.');doc.save(fixtures/'Creative brief.docx')
long=Document();long.add_heading('Long flowing document',0)
for i in range(65):long.add_paragraph(f'{i+1}. A longer paragraph that tests automatic pagination. The page renderer should create multiple readable pages without any explicit page breaks. '*3)
long.save(fixtures/'Long document.docx')
deep=fixtures/'Project'/'Artwork'/'Print'/'Drafts'/'Version 1'/'Nested'/'Beyond old limit';deep.mkdir(parents=True,exist_ok=True);(deep/'notes.txt').write_text('Nested-folder navigation works beyond the former five-level limit.',encoding='utf-8')
(fixtures/'Project'/'README.txt').write_text('Project source fixture',encoding='utf-8')
(fixtures/'Sample.rtf').write_text(r'{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\f0\fs28\b Jago document hub\b0\par RTF conversion test.}',encoding='ascii')
# A malformed archive must be rejected before its document XML is rendered.
with zipfile.ZipFile(fixtures/'Malformed.docx','w') as z:z.writestr('word/document.xml','<broken')
print('Illustrated, flowing, nested-folder and malformed fixtures created.')

slides=Presentation()
for title,body in [('Creative project','Slide previews preserve positioning and artwork.'),('Production plan','Two slides should render as two PDF pages.')]:
    slide=slides.slides.add_slide(slides.slide_layouts[1]);slide.shapes.title.text=title;slide.placeholders[1].text=body
slides.slides[0].shapes.add_picture(str(fixtures/'banner.png'), SlideInches(1),Inches(3),width=SlideInches(6))
slides.save(fixtures/'Project slides.pptx')
book=Workbook();sheet=book.active;sheet.title='Budget';sheet.append(['Deliverable','Cost']);sheet.append(['Poster',240]);sheet.append(['Animation',360]);sheet.append(['Total','=SUM(B2:B3)']);sheet.column_dimensions['A'].width=35;sheet.column_dimensions['B'].width=15;sheet.print_options.horizontalCentered=True;sheet.print_area='A1:B4';book.save(fixtures/'Project budget.xlsx')
