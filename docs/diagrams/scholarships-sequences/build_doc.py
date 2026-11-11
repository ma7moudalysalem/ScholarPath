#!/usr/bin/env python3
"""Assemble the essential Scholarships-module sequence diagrams into one Word file.
Each diagram on its own page, oriented to the image and sized to fill it."""
import os
from docx import Document
from docx.shared import Pt, Inches, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_ORIENT, WD_SECTION
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from PIL import Image
Image.MAX_IMAGE_PIXELS = None

HERE = r"D:/Projects/ScholarPath/docs/diagrams/scholarships-sequences"
PNG = os.path.join(HERE, "png")
NAVY = RGBColor(0x1F, 0x38, 0x64)

# code -> (title, one-line description: actor / trigger / correct-flow essence)
DESCS = {
 "SCH-SEQ-01": ("Browse, Search and Filter Scholarships",
   "Student → ScholarshipsPage → scholarshipsApi.search → ScholarshipsController → GetScholarshipsQuery. Defaults to Open scholarships; empty-state branch when nothing matches."),
 "SCH-SEQ-02": ("View Scholarship Details",
   "Student → ScholarshipDetail → scholarshipsApi.getById → GetScholarshipByIdQuery, returning the full listing and the Apply Now entry point."),
 "SCH-SEQ-03": ("Bookmark / Unbookmark Scholarship",
   "Student toggles a bookmark through BookmarkToggleCommand; the SavedScholarship row is created or removed (not a Bookmark entity)."),
 "SCH-SEQ-04": ("Apply Now — Start Local Draft Application",
   "Student Apply Now → StartApplicationCommand: validates Open status, deadline and profile eligibility, then resumes an existing draft or creates a new ApplicationTracker(Draft, InApp)."),
 "SCH-SEQ-05": ("Save Application Draft",
   "Student saves partial form data / documents / notes via SaveApplicationDraftCommand; guarded by ownership and Draft status."),
 "SCH-SEQ-06": ("Submit Application",
   "SubmitApplicationCommand drives Draft → Pending through ApplicationStateMachine after validating required data, then raises submission and status-changed events."),
 "SCH-SEQ-07": ("Withdraw Application",
   "WithdrawApplicationCommand moves an active application to Withdrawn through ApplicationStateMachine; blocked for final states (Accepted / Rejected)."),
 "SCH-SEQ-08": ("Track External Application",
   "ExternalIntentCommand records an off-platform application as an ApplicationTracker(Intending), for either a linked ExternalUrl listing or a pure off-platform entry."),
 "SCH-SEQ-09": ("Provider Creates Scholarship Listing",
   "CreateScholarshipCommand starts Provider listings in UnderReview and Admin listings directly in Open; ExternalUrl is Admin-only."),
 "SCH-SEQ-10": ("Admin Approves Scholarship",
   "ApproveScholarshipCommand opens the listing and notifies the provider; KnowledgeBaseIndexer is deliberately NOT invoked here."),
 "SCH-SEQ-11": ("Admin Rejects Scholarship",
   "RejectScholarshipCommand returns the listing to Draft with a mandatory RejectionReason and notifies the provider."),
 "SCH-SEQ-12": ("Provider Reviews Student Application",
   "Provider lists owned-scholarship applications, then ReviewApplicationCommand transitions status (In Assessment / Shortlisted / Accepted / Rejected) via ApplicationStateMachine and notifies the student."),
 "SCH-SEQ-13": ("Student Rates Scholarship Provider",
   "SubmitScholarshipProviderRatingCommand records one review per finalized application, updates the provider rating snapshot, and flags a low rating when the latest-20 average drops below 2.5."),
 "SCH-SEQ-14": ("Auto-Close Scholarship on Deadline",
   "ScholarshipAutoCloseJob closes only Open scholarships whose Deadline has passed."),
}
ORDER = [f"SCH-SEQ-{i:02d}" for i in range(1, 15)]


def _field(run, instr):
    b = OxmlElement('w:fldChar'); b.set(qn('w:fldCharType'), 'begin')
    it = OxmlElement('w:instrText'); it.set(qn('xml:space'), 'preserve'); it.text = instr
    e = OxmlElement('w:fldChar'); e.set(qn('w:fldCharType'), 'end')
    run._r.append(b); run._r.append(it); run._r.append(e)

def footer_page(section):
    p = section.footer.paragraphs[0]; p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    _field(p.add_run(), 'PAGE')

def add_section(doc, landscape):
    sec = doc.add_section(WD_SECTION.NEW_PAGE)
    for e in sec._sectPr.findall(qn('w:pgNumType')):
        sec._sectPr.remove(e)
    if landscape:
        sec.orientation = WD_ORIENT.LANDSCAPE; sec.page_width = Inches(11); sec.page_height = Inches(8.5)
    else:
        sec.orientation = WD_ORIENT.PORTRAIT; sec.page_width = Inches(8.5); sec.page_height = Inches(11)
    sec.left_margin = sec.right_margin = sec.top_margin = sec.bottom_margin = Inches(0.6)
    return sec


def build():
    doc = Document()
    n = doc.styles['Normal']; n.font.name = "Segoe UI"; n.font.size = Pt(11)
    for lvl, sz in [("Heading 1", 20), ("Heading 2", 14)]:
        s = doc.styles[lvl]; s.font.name = "Segoe UI"; s.font.size = Pt(sz)
        s.font.color.rgb = NAVY; s.font.bold = True
    s0 = doc.sections[0]
    s0.left_margin = Inches(1); s0.right_margin = Inches(1)
    footer_page(s0)

    # title
    for _ in range(3):
        doc.add_paragraph()
    t = doc.add_paragraph(); t.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = t.add_run("ScholarPath"); r.bold = True; r.font.size = Pt(30); r.font.color.rgb = NAVY
    st = doc.add_paragraph(); st.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = st.add_run("Scholarships Module — Essential Sequence Diagrams"); r.font.size = Pt(18)
    st2 = doc.add_paragraph(); st2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = st2.add_run("Verified against the current codebase (controllers, commands, queries, entities). "
                    "Payment objects are out of scope (free-mode).")
    r.font.size = Pt(11); r.italic = True; r.font.color.rgb = RGBColor(0x55, 0x55, 0x55)
    doc.add_paragraph()
    intro = doc.add_paragraph(); intro.alignment = WD_ALIGN_PARAGRAPH.CENTER
    intro.add_run("Fourteen core flows covering scholarship discovery, application lifecycle, "
                  "provider listing management, admin moderation, provider review, and rating.").font.size = Pt(11)

    # one diagram per page, oriented to the image
    for code in ORDER:
        img = os.path.join(PNG, f"{code}.png")
        if not os.path.exists(img):
            continue
        w, h = Image.open(img).size
        landscape = (w / h) > 1.15
        add_section(doc, landscape)
        pw = 11 if landscape else 8.5
        ph = 8.5 if landscape else 11
        title, desc = DESCS[code]
        head = doc.add_heading(f"{code} — {title}", level=2)
        head.paragraph_format.space_after = Pt(4)
        dp = doc.add_paragraph(); dr = dp.add_run(desc)
        dr.font.size = Pt(10); dr.italic = True; dr.font.color.rgb = RGBColor(0x44, 0x44, 0x44)
        dp.paragraph_format.space_after = Pt(6)
        avail_w = pw - 1.2
        avail_h = ph - 1.2 - 1.4   # room for heading + description + caption
        iw = Inches(avail_w); ih = iw * h / w
        if ih > Inches(avail_h):
            ih = Inches(avail_h); iw = ih * w / h
        p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.add_run().add_picture(img, width=iw)
        cap = doc.add_paragraph(); cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        cr = cap.add_run(f"Figure: {code} — {title} sequence diagram")
        cr.font.size = Pt(9); cr.italic = True; cr.font.color.rgb = RGBColor(0x66, 0x66, 0x66)

    out = os.path.join(HERE, "ScholarPath-Scholarships-Sequence-Diagrams.docx")
    doc.save(out)
    print("wrote", out)


if __name__ == "__main__":
    build()
