# Quality Audit – User Guide

A short guide to using the Quality Audit app (the digital QA 343-34 form). It works
on a laptop in the office and on an iPad on the shop floor.

> Screenshots: drop images into `docs/images/` and replace the *(screenshot: …)*
> lines below.

## Opening the app

Go to the app address in a browser (for example `http://csm-srv-16:5109`). No login
is needed at the moment.

Along the top there are five tabs: **New Audit**, **Dashboard**, **Coverage**,
**History**, **Admin**.

---

## New Audit

This is where you record a shift audit.

*(screenshot: the New Audit machine list)*

1. **Pick the area** – Sheet Metal or Assembly.
2. **Fill in the header** – Date, Shift (1st / 2nd / 3rd), Auditor (pick your name),
   and Area / Line.
3. **RAG guide** – the coloured box near the top explains what Green / Amber / Red
   mean and how often each must be checked. Tap **RAG guide** to collapse or show it.

### Working through the machines

Each machine is one row. The coloured bar on the left shows its severity for the week
(Green, Amber = orange, Red). The number on the left is its position on the sheet, so
repeated names (like "OEM product check EOL: Meta") stay separate.

For each machine, tap one of the four result buttons:

- **OK** – meets standard, no problem found. This also ticks all the sub-checks and
  check points on that row as OK, so they can be searched later. You can change any of
  them afterwards.
- **Not OK** – there is a deviation from standard.
- **No Time** – not audited because there was no time this shift.
- **Not Running** – not audited because the machine was not running.

**Most machines pass**, so use **Mark shown OK** to set every visible unchecked row to
OK in one go, then go back and change the few that need attention.

### Adding detail (tap the + on a row)

*(screenshot: an expanded row with sub-checks and photo)*

- **Drawing / Part No.**, **Customer**, **Action taken** and a free-text detail box.
- **Sub-checks**: Plans in place & used, Destructive / NDT (Sheet Metal only), Area
  docs up to date.
- **Check points**: the Level 1/2/3 checklist for that machine's severity. Higher
  severity shows more checks. Ones marked *(if applicable)* are optional.
- **Deviation** – **required** whenever the result or any sub-check is Not OK. You
  cannot save until it is filled in.
- **Photos** – tap **Add photo** to take a picture with the iPad camera. Thumbnails
  show on the row and on the dashboard failure board.

For a **Not OK**, also choose an Action taken (e.g. 4-D report, Hold report) – it is
recommended but not blocking.

For **No Time** or **Not Running**, only the reason is recorded. To set the reason on
lots of skipped rows at once, choose it in **Set reason for all Not Audited** and tap
the button.

### Filters

Use the chips to narrow the list: by severity, by area (Ph1 / Ph3), **Not checked**,
**Failures**, or **Not audited**.

### Saving

*(screenshot: the confirmation screen)*

- **Save draft** – keeps an unfinished audit you can come back to later in the shift.
- **Save & submit** – finishes the audit. You get a confirmation screen with the
  date, shift, auditor, how many were checked and the fail count.

If you close the tab by accident, the app offers to **restore** your unsaved work when
you come back. If an unfinished audit already exists for the same area, date and
shift, it offers to **resume** it.

---

## Dashboard

*(screenshot: the dashboard with the attainment gauge)*

Pick a **department** and a **week**, and an **attainment period** (this week, this
month, or rolling 12 months).

- **Attainment gauge** – the headline figure: audits completed against target
  (e.g. "3,253 of 4,500 audits – 72.3% against a 98% target"). Green at or above
  target, orange just below, red otherwise.
- **KPI tiles** – shifts logged, checks completed, pass rate, open failures.
- **Compliance against target** – actual vs expected checks for each machine this
  week; anything under target is highlighted.
- **This week vs last week** shown on the pass rate and failure tiles.
- **Failure board** – every Not OK, worst severity first, with the deviation,
  customer, action and any photos.
- **Charts** – failures by check point, failure rate by customer, pass rate by
  severity and by area. Click a slice or bar to filter the failure board.
- **Wall display** – hides the menus and enlarges everything for the QA office TV.

---

## Coverage

*(screenshot: the coverage grid)*

A grid of every machine down the side and the days of the audit week (Tuesday to
Monday) across the top. A tick/count shows where a check was done. Machines that are
**under target are listed first**. Use **Prev week / Next week** to move between weeks.

---

## History

*(screenshot: the history list)*

A list of past audits. Filter by department, shift and date range, or **search** the
deviation and action text (e.g. "torque", "4-D"). Click a row to open a read-only copy
of that audit. **12-month overview** shows the monthly pass/fail trend.

---

## Admin

Only for admin users. Type your name in **Signed in as** at the top.

- **Weekly severity review** – pick a week (defaults to next Tuesday), set each
  machine to Red / Amber / Green, and **Save all**. Changes from the previous week are
  shown. This is the Monday risk review; it takes effect the following Tuesday.
- **Machines / Users / Customers / Check points / Action types** – add, edit or retire
  (retire hides them without deleting history).
- **Severity levels** – edit how many checks per week each level needs, and its
  instruction text.
- **Department targets** – edit the target percentage the attainment gauge measures
  against.

---

## Good to know

- The audit week runs **Tuesday to Monday**.
- Changing this week's severity or a colour never changes past figures.
- Colours and text come from the database, so admins can change them without a new
  release.
