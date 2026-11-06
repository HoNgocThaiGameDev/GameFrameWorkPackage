"""Build the Vietnamese framework guide from its checked Markdown source."""
from __future__ import annotations

import argparse
import html
import io
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, Flowable, Frame, PageBreak, PageTemplate,
    Paragraph, Spacer, Table, TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.graphics import renderPDF
from svglib.svglib import svg2rlg
from svglib.fonts import register_font as register_svg_font

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "docs/framework-developer-guide.md"
OUTPUT = ROOT / "output/pdf/GameFrameWorkPackage_Developer_Guide_MVC.pdf"
TMP = ROOT / "tmp/pdfs"
INK = colors.HexColor("#1F2E40")
NAVY = colors.HexColor("#142D4E")
TEAL = colors.HexColor("#087D8B")
MUTED = colors.HexColor("#536779")
PALE = colors.HexColor("#F0F5F8")
LINE = colors.HexColor("#D4DEE6")
PAGE_W, PAGE_H = A4
MARGIN = 45
CONTENT_W = PAGE_W - 2 * MARGIN


def fonts():
    folder = Path("C:/Windows/Fonts")
    for name, filename in [("GuideSans", "arial.ttf"),
                           ("GuideBold", "arialbd.ttf"),
                           ("GuideItalic", "ariali.ttf"),
                           ("GuideMono", "consola.ttf")]:
        pdfmetrics.registerFont(TTFont(name, str(folder / filename)))
    pdfmetrics.registerFontFamily("GuideSans", normal="GuideSans",
                                 bold="GuideBold", italic="GuideItalic",
                                 boldItalic="GuideBold")
    # SVG diagrams use Arial; alias the actual Windows fonts to preserve Vietnamese.
    for name, filename in [("Arial", "arial.ttf"), ("Arial-Bold", "arialbd.ttf"),
                           ("Arial-Italic", "ariali.ttf"), ("Arial-BoldItalic", "arialbi.ttf")]:
        pdfmetrics.registerFont(TTFont(name, str(folder / filename)))
    pdfmetrics.registerFontFamily("Arial", normal="Arial", bold="Arial-Bold",
                                 italic="Arial-Italic", boldItalic="Arial-BoldItalic")
    # svglib needs a font path for custom TTF mappings, even if ReportLab
    # already registered that font. Without it, italic/bold silently fall back.
    for weight, style, name, filename in [
            ("normal", "normal", "Arial", "arial.ttf"),
            ("bold", "normal", "Arial-Bold", "arialbd.ttf"),
            ("normal", "italic", "Arial-Italic", "ariali.ttf"),
            ("bold", "italic", "Arial-BoldItalic", "arialbi.ttf")]:
        for family in ("Arial", "sans-serif"):
            _, success = register_svg_font(family, str(folder / filename),
                                           weight=weight, style=style,
                                           rlgFontName=name)
            if not success:
                raise ValueError("Cannot register SVG font: " + name)
    register_svg_font("monospace", str(folder / "consola.ttf"),
                      rlgFontName="GuideMono")


def load_uml_svg(path):
    """Preserve UML static underlines that svglib does not render itself."""
    tree = ET.parse(path)
    svg_ns = "{http://www.w3.org/2000/svg}"
    for parent in tree.iter():
        for node in list(parent):
            if (node.tag != svg_ns + "text" or
                    node.get("text-decoration") != "underline"):
                continue
            x, y = float(node.get("x")), float(node.get("y"))
            width = float(node.get("textLength"))
            parent.append(ET.Element(svg_ns + "line", {
                "x1": str(x), "x2": str(x + width),
                "y1": str(y + 2), "y2": str(y + 2),
                "stroke": node.get("fill", "#000000"),
                "stroke-width": "0.7",
            }))
    return svg2rlg(io.BytesIO(ET.tostring(tree.getroot(), encoding="utf-8")))


def inline(text):
    parts = re.split(r"(`[^`]+`)", text)
    result = []
    for part in parts:
        if part.startswith("`") and part.endswith("`"):
            result.append('<font name="GuideMono" size="9">' +
                          html.escape(part[1:-1]) + "</font>")
        else:
            value = html.escape(part)
            value = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", value)
            result.append(value)
    return "".join(result)


class CodeBlock(Flowable):
    def __init__(self, lines, label="C#", continuation=False):
        Flowable.__init__(self)
        self.lines = lines
        self.label = label + (" / tiếp" if continuation else "")
        self.leading = 10.9
        self.font_size = 8.45
        self.width = CONTENT_W
        self.height = 31 + len(lines) * self.leading
        self.spaceBefore = 5
        self.spaceAfter = 10

    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        for line in self.lines:
            if pdfmetrics.stringWidth(line, "GuideMono", self.font_size) > avail_width - 20:
                raise ValueError("Code line exceeds page width: " + line)
        return self.width, self.height

    def split(self, avail_width, avail_height):
        count = int((avail_height - 31) // self.leading)
        if count < 5:
            return []
        if count >= len(self.lines):
            return [self]
        if len(self.lines) - count < 3:
            count = max(5, count - 3)
        return [CodeBlock(self.lines[:count], self.label),
                CodeBlock(self.lines[count:], self.label.split(" / ")[0], True)]

    def draw(self):
        c = self.canv
        c.setFillColor(PALE)
        c.setStrokeColor(LINE)
        c.roundRect(0, 0, self.width, self.height, 5, fill=1, stroke=1)
        c.setFillColor(TEAL)
        c.setFont("GuideBold", 8)
        c.drawString(10, self.height - 15, self.label)
        c.setFillColor(INK)
        c.setFont("GuideMono", self.font_size)
        y = self.height - 30
        for line in self.lines:
            c.drawString(10, y, line)
            y -= self.leading


class UMLDiagram(Flowable):
    """Embed checked UML vector output without inventing graph semantics."""
    def __init__(self, name):
        Flowable.__init__(self)
        self.name = name
        folder = ROOT / "docs/diagrams/uml"
        catalog = json.loads((folder / "catalog.json").read_text(encoding="utf-8"))
        self.config = catalog[name]
        self.drawing = load_uml_svg(folder / (name + ".svg"))
        if self.drawing is None:
            raise ValueError("Missing UML SVG: " + name)
        self.spaceBefore = 7
        self.spaceAfter = 10
        self.caption_style = ParagraphStyle("UMLCaption", fontName="GuideItalic",
                                            fontSize=8.9, leading=11.8, textColor=MUTED)

    def wrap(self, avail_width, avail_height):
        self.width = avail_width
        self.scale = min(1.0, avail_width / self.drawing.width,
                         self.config["max_height"] / self.drawing.height)
        self.figure_h = self.drawing.height * self.scale
        self.figure_w = self.drawing.width * self.scale
        self.caption = Paragraph(inline(self.config["caption"]), self.caption_style)
        _, self.caption_h = self.caption.wrap(avail_width, 100)
        self.height = self.figure_h + self.caption_h + 8
        if self.scale * 16 < 7.5:
            raise ValueError("UML labels too small; relayout source: " + self.name)
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.saveState()
        c.translate((self.width - self.figure_w) / 2, self.caption_h + 8)
        c.scale(self.scale, self.scale)
        renderPDF.draw(self.drawing, c, 0, 0)
        c.restoreState()
        self.caption.drawOn(c, 0, 0)


class GuideDocument(BaseDocTemplate):
    def __init__(self, filename):
        BaseDocTemplate.__init__(self, str(filename), pagesize=A4,
                                 leftMargin=MARGIN, rightMargin=MARGIN,
                                 topMargin=58, bottomMargin=48,
                                 title="GameFrameWorkPackage - Cách dùng, scene và module",
                                 author="GameFrameWorkPackage Team")
        frame = Frame(MARGIN, 48, CONTENT_W, PAGE_H - 106,
                      leftPadding=0, rightPadding=0,
                      topPadding=0, bottomPadding=0)
        self.addPageTemplates(PageTemplate(id="Guide", frames=frame,
                                          onPage=self.decorations))
        self.chapter_serial = 0

    def decorations(self, canvas, doc):
        canvas.saveState()
        if doc.page > 1:
            canvas.setFillColor(TEAL)
            canvas.rect(MARGIN, PAGE_H - 36, 19, 3, fill=1, stroke=0)
            canvas.setFont("GuideBold", 8)
            canvas.setFillColor(MUTED)
            canvas.drawString(MARGIN + 26, PAGE_H - 36,
                              "GAMEFRAMEWORKPACKAGE / DEVELOPER GUIDE")
            canvas.setStrokeColor(LINE)
            canvas.line(MARGIN, 35, PAGE_W - MARGIN, 35)
            canvas.setFont("GuideSans", 8)
            canvas.drawString(MARGIN, 23, "11/10/2026 · Framework gốc / InitModule")
            canvas.drawRightString(PAGE_W - MARGIN, 23, "Trang " + str(doc.page))
        canvas.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == "Chapter":
            text = flowable.getPlainText()
            key = "chapter_" + re.match(r"\d+", text).group()
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(text, key, 0, False)
            self.notify("TOCEntry", (0, text, self.page, key))


def styles():
    base = dict(fontName="GuideSans", textColor=INK, alignment=TA_LEFT)
    return {
        "body": ParagraphStyle("Body", fontSize=10.1, leading=14.2,
                               spaceAfter=6, **base),
        "chapter": ParagraphStyle("Chapter", fontName="GuideBold", textColor=NAVY,
                                  fontSize=19, leading=25, spaceAfter=16,
                                  keepWithNext=True),
        "sub": ParagraphStyle("Subheading", fontName="GuideBold", textColor=TEAL,
                              fontSize=12, leading=17, spaceBefore=9,
                              spaceAfter=7, keepWithNext=True),
        "cell": ParagraphStyle("Cell", fontSize=8.8, leading=12.0, **base),
        "diagram": ParagraphStyle("Diagram", fontSize=9.2, leading=12,
                                  alignment=1, fontName="GuideSans", textColor=NAVY),
        "toc": ParagraphStyle("TOC", fontName="GuideSans", textColor=INK,
                              fontSize=10.0, leading=12.0, spaceBefore=0),
    }


def md_table(lines, st):
    rows = [[s.strip() for s in row.strip().strip("|").split("|")]
            for row in lines]
    rows = [rows[0]] + [row for row in rows[1:]
                        if not all(re.fullmatch(r":?-+:?", cell.replace(" ", ""))
                                   for cell in row)]
    count = len(rows[0])
    compact = count == 4 and rows[0][0] == "Feature"
    cell_style = (ParagraphStyle("MatrixCell", parent=st["cell"], leading=11.0)
                  if compact else st["cell"])
    widths = ([0.39, 0.61] if count == 2 else
              [0.26, 0.34, 0.40] if count == 3 else [0.18, 0.29, 0.27, 0.26])
    cells = []
    for index, row in enumerate(rows):
        if len(row) != count:
            raise ValueError("Bad table column count: " + str(row))
        cells.append([Paragraph(("<b>" + inline(cell) + "</b>")
                                if index == 0 else inline(cell), cell_style)
                      for cell in row])
    table = Table(cells, colWidths=[CONTENT_W * w for w in widths],
                  repeatRows=1, hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#DDECF0")),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE]),
        ("LINEBELOW", (0, 0), (-1, 0), 0.6, TEAL),
        ("LINEBELOW", (0, 1), (-1, -1), 0.35, LINE),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 4.5 if compact else 5.5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4.5 if compact else 5.5),
    ]))
    table.spaceAfter = 12
    return table


def build():
    fonts()
    st = styles()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    TMP.mkdir(parents=True, exist_ok=True)
    text = SOURCE.read_text(encoding="utf-8")
    # Export complete code samples for an independent compiler check.
    snippets = re.findall(r"```csharp\n(.*?)\n```", text, re.S)
    example_dir = TMP / "examples"
    example_dir.mkdir(exist_ok=True)
    for index, snippet in enumerate(snippets, 1):
        (example_dir / f"Example_{index:02}.cs").write_text(snippet + "\n", encoding="utf-8")

    story = [Spacer(1, 60)]
    cover_brand = ParagraphStyle("Brand", fontName="GuideBold", textColor=TEAL,
                                 fontSize=11, leading=16, spaceAfter=25)
    cover_title = ParagraphStyle("Cover", fontName="GuideBold", textColor=NAVY,
                                 fontSize=31, leading=40, spaceAfter=16)
    cover_sub = ParagraphStyle("CoverSub", fontName="GuideSans", textColor=INK,
                               fontSize=19, leading=28, spaceAfter=26)
    story.append(Paragraph("UNITY MOBILE FRAMEWORK / TÀI LIỆU KỸ THUẬT", cover_brand))
    story.append(Paragraph("GameFrameWorkPackage", cover_title))
    story.append(Paragraph("Cách dùng framework<br/>Scene, module và controller", cover_sub))
    story.append(Paragraph("Bắt đầu với ba scene Init, Menu, Game.<br/>"
                           "UML class/sequence/activity, hierarchy, Inspector và ví dụ C#.", st["body"]))
    story.append(Spacer(1, 30))
    story.append(md_table([
        "| Mốc tài liệu | Cấu hình đối chiếu |",
        "| Ngày cập nhật | 11/10/2026 |",
        "| Unity / package | 6000.3.12f1 / 1.2.2 |",
        "| Runtime flow | Init > Menu > Game |",
        "| Ads tích hợp | Ads Kit 2.3.0 / AdMob 11.0.0 / EDM4U 1.2.187 |",
        "| Kiến trúc | InitModule registry, không dùng DI container |",
    ], st))
    story.append(Spacer(1, 20))
    story.append(Paragraph("Nguồn: README và source hiện tại, cùng tài liệu Ads, IAP, Save. "
                           "Các kiểu Guide là ví dụ trong tài liệu; game mới cần triển khai "
                           "board, level content và policy sản phẩm.", st["body"]))
    story.append(PageBreak())
    story.append(Paragraph("Mục lục", st["sub"]))
    story.append(Paragraph("Đọc chương 1-4 để dùng framework và cấu hình ba scene. "
                           "Chương 5-8 giải thích kiến trúc; chương 9-25 tra cứu module; "
                           "chương 26-27 tích hợp gameplay; chương 28-29 tổng hợp và kiểm tra.", st["body"]))
    toc = TableOfContents()
    toc.levelStyles = [st["toc"]]
    story.append(toc)

    lines = text[text.index("## 1."):].splitlines()
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        if not line:
            i += 1
            continue
        if line == "<!-- pagebreak -->":
            story.append(PageBreak())
            i += 1
        elif line.startswith("## "):
            story.append(PageBreak())
            story.append(Paragraph(inline(line[3:]), st["chapter"]))
            i += 1
        elif line.startswith("### "):
            story.append(Paragraph(inline(line[4:]), st["sub"]))
            i += 1
        elif line.startswith("```"):
            kind = line[3:]
            block = []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                block.append(lines[i].rstrip())
                i += 1
            i += 1
            if kind == "uml":
                if len(block) != 1:
                    raise ValueError("UML block must reference one diagram ID")
                story.append(UMLDiagram(block[0].strip()))
            elif kind in ("diagram", "scene", "sequence"):
                raise ValueError("Legacy non-UML diagram remains: " + kind)
            else:
                story.append(CodeBlock(block, "C#" if kind == "csharp" else "CẤU TRÚC"))
        elif line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                rows.append(lines[i].strip())
                i += 1
            story.append(md_table(rows, st))
        elif re.match(r"^\d+\. ", line):
            story.append(Paragraph(inline(line), st["body"]))
            i += 1
        else:
            paragraph = [line]
            i += 1
            while i < len(lines) and lines[i].strip() and not re.match(r"^(#|\||```|\d+\. )", lines[i]):
                paragraph.append(lines[i].strip())
                i += 1
            story.append(Paragraph(inline(" ".join(paragraph)), st["body"]))

    GuideDocument(OUTPUT).multiBuild(story)
    from pypdf import PdfReader
    reader = PdfReader(OUTPUT)
    report = {"source": str(SOURCE), "pdf": str(OUTPUT),
              "pages": len(reader.pages), "code_samples": len(snippets),
              "chapters": len(re.findall(r"^## \d+\.", text, re.M))}
    (TMP / "build-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, help="PDF output path; overrides default artifact")
    args = parser.parse_args()
    if args.output is not None:
        OUTPUT = args.output.resolve()
    build()
