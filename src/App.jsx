import React, { useState, useEffect, useMemo } from "react";

// ---- Reference data: the 59 line items from the paper QA 343-31 sheet ----
const MACHINES = [
  { id: 1, name: "Flexi Matrix in place & correct", loc: "Ph 1 & 3", sev: 1 },
  { id: 2, name: "NC cards full & good condition", loc: "Ph 1 & 3", sev: 1 },
  { id: 3, name: "Truebend F35 Fold", loc: "Ph3", sev: 1 },
  { id: 4, name: "Truebend F34 Fold", loc: "Ph3", sev: 1 },
  { id: 5, name: "Trumpf T5000 S15", loc: "Ph3", sev: 2 },
  { id: 6, name: "Truebend F33 Fold", loc: "Ph3", sev: 1 },
  { id: 7, name: "Trumpf T5000 S14", loc: "Ph3", sev: 1 },
  { id: 8, name: "Truebend F32 Fold", loc: "Ph3", sev: 1 },
  { id: 9, name: "Trumpf T5000 S13", loc: "Ph3", sev: 2 },
  { id: 10, name: "Tapping Cell", loc: "Ph3", sev: 1 },
  { id: 11, name: "Trumpf Laser", loc: "Ph3", sev: 1 },
  { id: 12, name: "Google Small Parts Weld Ph3", loc: "Ph3", sev: 2 },
  { id: 13, name: "Small Parts Weld Ph3", loc: "Ph3", sev: 2 },
  { id: 14, name: "Robot Weld R22", loc: "Ph3", sev: 3 },
  { id: 15, name: "Robot Weld R23", loc: "Ph3", sev: 3 },
  { id: 16, name: "Robot Weld R24", loc: "Ph3", sev: 1 },
  { id: 17, name: "Robot Weld R25", loc: "Ph3", sev: 1 },
  { id: 18, name: "Trumpf T7000", loc: "Ph1", sev: 3 },
  { id: 19, name: "Trumpf T500", loc: "Ph1", sev: 1 },
  { id: 20, name: "Trumpf Trumatic 5000R", loc: "Ph1", sev: 1 },
  { id: 21, name: "Trumpf T5000 S11", loc: "Ph1", sev: 3 },
  { id: 22, name: "Trumpf T5000 S10", loc: "Ph1", sev: 1 },
  { id: 23, name: "Bystronic Robot F31", loc: "Ph1", sev: 3 },
  { id: 24, name: "Bystronic F30", loc: "Ph1", sev: 2 },
  { id: 25, name: "Bystronic F29", loc: "Ph1", sev: 1 },
  { id: 26, name: "Bystronic Robot F28", loc: "Ph1", sev: 2 },
  { id: 27, name: "Trumpf F36", loc: "Ph1", sev: 2 },
  { id: 28, name: "Salvagnini 1", loc: "Ph1", sev: 3 },
  { id: 29, name: "Ideal Robot Salvi 1", loc: "Ph1", sev: 2 },
  { id: 30, name: "Salvagnini 3", loc: "Ph1", sev: 2 },
  { id: 31, name: "TS Heilbronn", loc: "Ph1", sev: 3 },
  { id: 32, name: "Edward Pearson PB F6", loc: "Ph1", sev: 2 },
  { id: 33, name: "Bystronic 250T", loc: "Ph1", sev: 1 },
  { id: 34, name: "Bystronic XACT 160", loc: "Ph1", sev: 1 },
  { id: 35, name: "Sciaky Proj Welder", loc: "Ph1", sev: 2 },
  { id: 36, name: "Sciaky Spot Welder", loc: "Ph1", sev: 2 },
  { id: 37, name: "Hand Stud", loc: "Ph1", sev: 2 },
  { id: 38, name: "Tucker Stud Robot", loc: "Ph1", sev: 1 },
  { id: 39, name: "Haeger Press", loc: "Ph1", sev: 1 },
  { id: 40, name: "Edward Pearson F17", loc: "Ph1", sev: 2 },
  { id: 41, name: "Trumpf F37", loc: "Ph1", sev: 2 },
  { id: 42, name: "Profile Roller", loc: "Ph1", sev: 1 },
  { id: 43, name: "German Jig 1", loc: "Ph1", sev: 3 },
  { id: 44, name: "Vertical Weld", loc: "Ph1", sev: 2 },
  { id: 45, name: "Trainee Weld Bay", loc: "Ph1", sev: 1 },
  { id: 46, name: "Small Parts Manual Weld 1", loc: "Ph1", sev: 1 },
  { id: 47, name: "Small Parts Manual Weld 2", loc: "Ph1", sev: 2 },
  { id: 48, name: "Manual Weld Double Bay", loc: "Ph1", sev: 2 },
  { id: 49, name: "Top + Bottom TBR01", loc: "Ph1", sev: 1 },
  { id: 50, name: "Top + Bottom TBR02", loc: "Ph1", sev: 1 },
  { id: 51, name: "Robot Weld R26", loc: "Ph1", sev: 1 },
  { id: 52, name: "Main Frame Weld", loc: "Ph1", sev: 3 },
  { id: 53, name: "HPE Weld", loc: "Ph1", sev: 3 },
  { id: 54, name: "MOR Small Weld", loc: "Ph1", sev: 3 },
  { id: 55, name: "Panasonic Meta Weld", loc: "Ph1", sev: 3, special: true },
  { id: 56, name: "Google Frame Weld", loc: "Ph1", sev: 3 },
  { id: 57, name: "German Jig 2", loc: "Ph1", sev: 3 },
  { id: 58, name: "Microsoft Frame Weld", loc: "Ph1", sev: 3 },
  { id: 59, name: "Grinding Booths", loc: "Ph1", sev: 1 },
];

const SHIFTS = ["Early", "Late", "Nights"];
const STORE_KEY = "sheetmetal-audits";
const todayISO = () => new Date().toISOString().slice(0, 10);
const uid = () => Date.now().toString(36) + Math.random().toString(36).slice(2, 6);

// ---- Storage helpers (persist across sessions; degrade gracefully) ----
async function loadAudits() {
  try {
    if (!window.storage) return { ok: false, list: [] };
    const res = await window.storage.get(STORE_KEY);
    return { ok: true, list: res && res.value ? JSON.parse(res.value) : [] };
  } catch (e) {
    return { ok: true, list: [] };
  }
}
async function persistAudits(list) {
  try {
    if (!window.storage) return false;
    await window.storage.set(STORE_KEY, JSON.stringify(list));
    return true;
  } catch (e) {
    return false;
  }
}

// ---- Classification logic ----
const rowIsAudited = (r) => r && r.result && r.result !== "";
const rowIsNA = (r) => r && r.result === "NA";
const rowIsNOK = (r) =>
  r && (r.result === "NOK" || r.plans === "NOK" || r.ndt === "NOK" || r.docs === "NOK");
const rowIsOK = (r) => rowIsAudited(r) && !rowIsNA(r) && !rowIsNOK(r);

const emptyRow = () => ({
  result: "", part: "", plans: "", ndt: "", docs: "", deviation: "", customer: "", action: "",
});

const RESULTS = [
  { v: "OK", label: "OK", tone: "green" },
  { v: "NOK", label: "NOK", tone: "red" },
  { v: "NA", label: "N/A", tone: "grey" },
];
const SUBS = [
  { v: "OK", label: "OK" },
  { v: "NOK", label: "NOK" },
  { v: "NA", label: "N/A" },
];

function Seg({ value, options, onChange, size = "md" }) {
  return (
    <div className={`seg seg-${size}`}>
      {options.map((o) => (
        <button
          key={o.v}
          type="button"
          className={`seg-btn ${value === o.v ? "on " + (o.tone || "blue") : ""}`}
          onClick={() => onChange(value === o.v ? "" : o.v)}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

function sevMeta(s) {
  if (s === 3) return { tone: "red", label: "3" };
  if (s === 2) return { tone: "amber", label: "2" };
  return { tone: "green", label: "1" };
}

// ---------------------------------------------------------------------------
export default function App() {
  const [view, setView] = useState("entry");
  const [audits, setAudits] = useState([]);
  const [loaded, setLoaded] = useState(false);

  const [header, setHeader] = useState({
    area: "Sheet Metal", date: todayISO(), shift: "Early", auditor: "",
  });
  const [rows, setRows] = useState(() => {
    const o = {}; MACHINES.forEach((m) => (o[m.id] = emptyRow())); return o;
  });
  const [other, setOther] = useState("");
  const [locFilter, setLocFilter] = useState("all");
  const [flag, setFlag] = useState("all"); // all | todo | nok | sev3
  const [expanded, setExpanded] = useState(null);
  const [toast, setToast] = useState("");

  useEffect(() => {
    loadAudits().then((r) => { setAudits(r.list); setLoaded(true); });
  }, []);

  const setRow = (id, patch) =>
    setRows((prev) => ({ ...prev, [id]: { ...prev[id], ...patch } }));

  const resetEntry = () => {
    const o = {}; MACHINES.forEach((m) => (o[m.id] = emptyRow()));
    setRows(o); setOther(""); setExpanded(null);
  };

  const visibleMachines = useMemo(() => {
    return MACHINES.filter((m) => {
      if (locFilter === "Ph1" && !m.loc.includes("1")) return false;
      if (locFilter === "Ph3" && !m.loc.includes("3")) return false;
      const r = rows[m.id];
      if (flag === "todo" && rowIsAudited(r)) return false;
      if (flag === "nok" && !rowIsNOK(r)) return false;
      if (flag === "sev3" && m.sev !== 3) return false;
      return true;
    });
  }, [locFilter, flag, rows]);

  const auditedCount = useMemo(
    () => MACHINES.filter((m) => rowIsAudited(rows[m.id])).length, [rows]
  );
  const nokCount = useMemo(
    () => MACHINES.filter((m) => rowIsNOK(rows[m.id])).length, [rows]
  );

  const markShownOK = () => {
    setRows((prev) => {
      const next = { ...prev };
      visibleMachines.forEach((m) => {
        if (!rowIsAudited(next[m.id])) next[m.id] = { ...next[m.id], result: "OK" };
      });
      return next;
    });
  };

  const submit = async () => {
    const session = {
      id: uid(), ...header, other, rows: JSON.parse(JSON.stringify(rows)),
      createdAt: new Date().toISOString(),
    };
    const next = [session, ...audits];
    setAudits(next);
    const ok = await persistAudits(next);
    setToast(ok ? "Shift audit saved" : "Saved for this session (storage unavailable)");
    setTimeout(() => setToast(""), 3200);
    resetEntry();
    setView("dashboard");
  };

  const clearAll = async () => {
    setAudits([]); await persistAudits([]);
  };

  return (
    <>
      <style>{CSS}</style>
      <div className="app">
        <header className="top">
          <div className="brand">
            <div className="mark" aria-hidden>
              <span style={{ background: "#c8102e" }} />
              <span style={{ background: "#2f6f9f" }} />
              <span style={{ background: "#15803d" }} />
            </div>
            <div>
              <div className="ttl">Sheet Metal Shift Audit</div>
              <div className="sub">QA 343-31 · digital line check &amp; dashboard</div>
            </div>
          </div>
          <nav className="tabs">
            <button className={view === "entry" ? "on" : ""} onClick={() => setView("entry")}>New audit</button>
            <button className={view === "dashboard" ? "on" : ""} onClick={() => setView("dashboard")}>
              Dashboard{audits.length ? <em>{audits.length}</em> : null}
            </button>
          </nav>
        </header>

        {view === "entry" ? (
          <Entry
            header={header} setHeader={setHeader}
            rows={rows} setRow={setRow}
            visibleMachines={visibleMachines}
            locFilter={locFilter} setLocFilter={setLocFilter}
            flag={flag} setFlag={setFlag}
            expanded={expanded} setExpanded={setExpanded}
            auditedCount={auditedCount} nokCount={nokCount}
            other={other} setOther={setOther}
            markShownOK={markShownOK} resetEntry={resetEntry} submit={submit}
          />
        ) : (
          <Dashboard audits={audits} loaded={loaded} clearAll={clearAll} goEntry={() => setView("entry")} />
        )}

        {toast ? <div className="toast">{toast}</div> : null}
      </div>
    </>
  );
}

// ---------------------------------------------------------------------------
function Entry(props) {
  const {
    header, setHeader, rows, setRow, visibleMachines, locFilter, setLocFilter,
    flag, setFlag, expanded, setExpanded, auditedCount, nokCount, other, setOther,
    markShownOK, resetEntry, submit,
  } = props;
  const pct = Math.round((auditedCount / MACHINES.length) * 100);

  return (
    <main className="pane">
      <section className="card head-card">
        <div className="head-grid">
          <Field label="Area">
            <input value={header.area} onChange={(e) => setHeader({ ...header, area: e.target.value })} />
          </Field>
          <Field label="Date">
            <input type="date" value={header.date} onChange={(e) => setHeader({ ...header, date: e.target.value })} />
          </Field>
          <Field label="Shift">
            <select value={header.shift} onChange={(e) => setHeader({ ...header, shift: e.target.value })}>
              {SHIFTS.map((s) => <option key={s}>{s}</option>)}
            </select>
          </Field>
          <Field label="Auditor">
            <input placeholder="Initials" value={header.auditor} onChange={(e) => setHeader({ ...header, auditor: e.target.value })} />
          </Field>
        </div>
      </section>

      <div className="toolbar">
        <div className="chips">
          {["all", "Ph1", "Ph3"].map((l) => (
            <button key={l} className={`chip ${locFilter === l ? "on" : ""}`} onClick={() => setLocFilter(l)}>
              {l === "all" ? "All areas" : l}
            </button>
          ))}
          <span className="chip-div" />
          {[
            ["all", "Everything"], ["todo", "Not checked"], ["nok", "Failures"], ["sev3", "Severity 3"],
          ].map(([k, lbl]) => (
            <button key={k} className={`chip ${flag === k ? "on" : ""}`} onClick={() => setFlag(k)}>{lbl}</button>
          ))}
        </div>
        <button className="ghost" onClick={markShownOK}>Mark shown OK</button>
      </div>

      <section className="rows">
        {visibleMachines.map((m) => {
          const r = rows[m.id];
          const sm = sevMeta(m.sev);
          const state = rowIsNOK(r) ? "nok" : rowIsOK(r) ? "ok" : rowIsNA(r) ? "na" : "todo";
          const open = expanded === m.id;
          return (
            <div key={m.id} className={`row state-${state}`}>
              <div className={`spine ${sm.tone}`} title={`Severity ${m.sev}`} />
              <div className="row-main">
                <div className="row-id">{m.id}</div>
                <div className="row-name">
                  <div className="nm">
                    {m.name}{m.special ? <span className="xx" title="Special measures">XX</span> : null}
                  </div>
                  <div className="meta">
                    <span className={`pill ${sm.tone}`}>SEV {sm.label}</span>
                    <span className="pill grey">{m.loc}</span>
                  </div>
                </div>
                <Seg value={r.result} options={RESULTS} onChange={(v) => setRow(m.id, { result: v })} />
                <button className={`disc ${open ? "on" : ""}`} onClick={() => setExpanded(open ? null : m.id)} aria-label="More">
                  {open ? "–" : "+"}
                </button>
              </div>
              {open ? (
                <div className="row-detail">
                  <Field label="Drawing / Part No.">
                    <input className="mono" value={r.part} onChange={(e) => setRow(m.id, { part: e.target.value })} />
                  </Field>
                  <div className="sub-checks">
                    <SubField label="Plans in place & used"><Seg size="sm" value={r.plans} options={SUBS} onChange={(v) => setRow(m.id, { plans: v })} /></SubField>
                    <SubField label="Destructive / NDT in place"><Seg size="sm" value={r.ndt} options={SUBS} onChange={(v) => setRow(m.id, { ndt: v })} /></SubField>
                    <SubField label="Area docs up to date"><Seg size="sm" value={r.docs} options={SUBS} onChange={(v) => setRow(m.id, { docs: v })} /></SubField>
                  </div>
                  <div className="detail-grid">
                    <Field label="Deviation from standard">
                      <textarea rows={2} value={r.deviation} onChange={(e) => setRow(m.id, { deviation: e.target.value })} />
                    </Field>
                    <Field label="Customer">
                      <input value={r.customer} onChange={(e) => setRow(m.id, { customer: e.target.value })} />
                    </Field>
                    <Field label="Action taken (Hold / Audit / QA Tracker)">
                      <input value={r.action} onChange={(e) => setRow(m.id, { action: e.target.value })} />
                    </Field>
                  </div>
                </div>
              ) : (
                (r.deviation || r.part) ? (
                  <div className="row-note">
                    {r.part ? <span className="mono note-part">{r.part}</span> : null}
                    {r.deviation ? <span className="note-dev">{r.deviation}</span> : null}
                    {r.customer ? <span className="pill grey">{r.customer}</span> : null}
                  </div>
                ) : null
              )}
            </div>
          );
        })}
      </section>

      <section className="card other-card">
        <Field label="Other daily issues & tasks">
          <textarea rows={2} value={other} onChange={(e) => setOther(e.target.value)} placeholder="Free notes for the shift" />
        </Field>
      </section>

      <div className="footbar">
        <div className="progress">
          <div className="pbar"><span style={{ width: pct + "%" }} /></div>
          <div className="pnum">
            <strong>{auditedCount}</strong>/{MACHINES.length} checked
            {nokCount ? <span className="fail-count">{nokCount} fail{nokCount > 1 ? "s" : ""}</span> : null}
          </div>
        </div>
        <div className="foot-actions">
          <button className="ghost" onClick={resetEntry}>Reset</button>
          <button className="primary" onClick={submit} disabled={auditedCount === 0}>Save shift audit</button>
        </div>
      </div>
    </main>
  );
}

function Field({ label, children }) {
  return <label className="field"><span>{label}</span>{children}</label>;
}
function SubField({ label, children }) {
  return <div className="subfield"><span>{label}</span>{children}</div>;
}

// ---------------------------------------------------------------------------
function Dashboard({ audits, loaded, clearAll, goEntry }) {
  const stats = useMemo(() => {
    let ok = 0, nok = 0, na = 0, audited = 0;
    const bySev = { 1: { ok: 0, nok: 0 }, 2: { ok: 0, nok: 0 }, 3: { ok: 0, nok: 0 } };
    const byLoc = { Ph1: { ok: 0, nok: 0 }, Ph3: { ok: 0, nok: 0 } };
    const failures = [];
    audits.forEach((s) => {
      MACHINES.forEach((m) => {
        const r = s.rows[m.id];
        if (!rowIsAudited(r)) return;
        audited++;
        if (rowIsNA(r)) { na++; return; }
        const fail = rowIsNOK(r);
        if (fail) { nok++; bySev[m.sev].nok++; } else { ok++; bySev[m.sev].ok++; }
        if (m.loc.includes("1")) fail ? byLoc.Ph1.nok++ : byLoc.Ph1.ok++;
        if (m.loc.includes("3")) fail ? byLoc.Ph3.nok++ : byLoc.Ph3.ok++;
        if (fail) failures.push({ m, r, s });
      });
    });
    failures.sort((a, b) => b.m.sev - a.m.sev || (a.s.date < b.s.date ? 1 : -1));
    const passRate = ok + nok ? Math.round((ok / (ok + nok)) * 100) : null;
    return { ok, nok, na, audited, bySev, byLoc, failures, passRate };
  }, [audits]);

  if (loaded && audits.length === 0) {
    return (
      <main className="pane">
        <div className="empty">
          <div className="empty-mark">▤</div>
          <h2>No shifts logged yet</h2>
          <p>Complete a shift audit and it lands here as live pass rates, a severity-ranked failure board, and area coverage.</p>
          <div className="empty-actions">
            <button className="primary" onClick={goEntry}>Start the first audit</button>
          </div>
        </div>
      </main>
    );
  }

  const kpi = [
    { label: "Shifts logged", val: audits.length },
    { label: "Line checks", val: stats.audited },
    { label: "Pass rate", val: stats.passRate === null ? "—" : stats.passRate + "%", tone: stats.passRate !== null && stats.passRate < 90 ? "red" : "green" },
    { label: "Open failures", val: stats.nok, tone: stats.nok ? "red" : "green" },
  ];

  return (
    <main className="pane">
      <div className="kpi-row">
        {kpi.map((k) => (
          <div key={k.label} className={`kpi ${k.tone || ""}`}>
            <div className="kpi-val">{k.val}</div>
            <div className="kpi-lbl">{k.label}</div>
          </div>
        ))}
      </div>

      <div className="dash-grid">
        <section className="card board">
          <div className="card-hd">
            <h3>Failure board</h3>
            <span className="hd-note">Severity 3 first</span>
          </div>
          {stats.failures.length === 0 ? (
            <div className="clean">No failures recorded. All checked line items passed.</div>
          ) : (
            <div className="fail-list">
              {stats.failures.map((f, i) => {
                const sm = sevMeta(f.m.sev);
                return (
                  <div key={i} className="fail">
                    <div className={`sev-tag ${sm.tone}`}>SEV {sm.label}</div>
                    <div className="fail-body">
                      <div className="fail-top">
                        <span className="fail-name">{f.m.name}</span>
                        <span className="fail-when">{f.s.date} · {f.s.shift}</span>
                      </div>
                      <div className="fail-dev">{f.r.deviation || "Marked NOK — no deviation note"}</div>
                      <div className="fail-tags">
                        {f.r.part ? <span className="mono tag">{f.r.part}</span> : null}
                        {f.r.customer ? <span className="tag">{f.r.customer}</span> : null}
                        {f.r.action ? <span className="tag act">{f.r.action}</span> : null}
                        {f.r.plans === "NOK" ? <span className="tag warn">Plans</span> : null}
                        {f.r.ndt === "NOK" ? <span className="tag warn">NDT</span> : null}
                        {f.r.docs === "NOK" ? <span className="tag warn">Docs</span> : null}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </section>

        <div className="side">
          <section className="card">
            <div className="card-hd"><h3>By area</h3></div>
            {["Ph1", "Ph3"].map((loc) => {
              const d = stats.byLoc[loc]; const tot = d.ok + d.nok;
              const rate = tot ? Math.round((d.ok / tot) * 100) : 0;
              return (
                <div key={loc} className="bar-row">
                  <div className="bar-lbl">{loc}<em>{tot} checks</em></div>
                  <div className="bar-track"><span className={rate < 90 ? "red" : "green"} style={{ width: (tot ? rate : 0) + "%" }} /></div>
                  <div className="bar-val">{tot ? rate + "%" : "—"}</div>
                </div>
              );
            })}
          </section>

          <section className="card">
            <div className="card-hd"><h3>Failures by severity</h3></div>
            {[3, 2, 1].map((s) => {
              const d = stats.bySev[s]; const tot = d.ok + d.nok; const sm = sevMeta(s);
              return (
                <div key={s} className="bar-row">
                  <div className="bar-lbl"><span className={`dot ${sm.tone}`} />Sev {s}<em>{d.nok} of {tot}</em></div>
                  <div className="bar-track"><span className={sm.tone} style={{ width: (tot ? (d.nok / tot) * 100 : 0) + "%" }} /></div>
                  <div className="bar-val">{d.nok}</div>
                </div>
              );
            })}
          </section>

          <section className="card recent">
            <div className="card-hd"><h3>Recent shifts</h3></div>
            {audits.slice(0, 6).map((s) => {
              let a = 0, f = 0;
              MACHINES.forEach((m) => { const r = s.rows[m.id]; if (rowIsAudited(r)) { a++; if (rowIsNOK(r)) f++; } });
              return (
                <div key={s.id} className="rec">
                  <div className="rec-main"><strong>{s.date}</strong> · {s.shift} {s.auditor ? "· " + s.auditor : ""}</div>
                  <div className="rec-stats">
                    <span>{a}/59</span>
                    <span className={f ? "bad" : "good"}>{f ? f + " fail" : "clean"}</span>
                  </div>
                </div>
              );
            })}
          </section>
        </div>
      </div>

      <div className="dash-foot">
        <button className="ghost danger" onClick={clearAll}>Clear all data</button>
      </div>
    </main>
  );
}

// ---------------------------------------------------------------------------
const CSS = `
:root{
  --bg:#eef1f5; --panel:#fff; --ink:#16202e; --muted:#647084; --faint:#8b97a8;
  --line:#dbe1ea; --steel:#1c3d5a; --steel2:#24506f; --blue:#2f6f9f;
  --green:#15803d; --green-bg:#dcfce7; --amber:#b45309; --amber-bg:#fdeecb;
  --red:#b91c1c; --red-bg:#fde4e4; --grey-bg:#eef1f5;
}
*{box-sizing:border-box}
.app{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",system-ui,sans-serif;
  color:var(--ink);background:var(--bg);min-height:100vh;-webkit-font-smoothing:antialiased;}
.mono{font-family:ui-monospace,"SF Mono","Cascadia Code",Menlo,Consolas,monospace;}

.top{position:sticky;top:0;z-index:20;display:flex;align-items:center;justify-content:space-between;
  gap:16px;padding:14px 20px;background:var(--steel);color:#fff;flex-wrap:wrap;
  box-shadow:0 1px 0 rgba(0,0,0,.15);}
.brand{display:flex;align-items:center;gap:12px}
.mark{display:flex;flex-direction:column;gap:3px}
.mark span{width:26px;height:6px;border-radius:1px;display:block}
.ttl{font-weight:700;font-size:16px;letter-spacing:.2px}
.sub{font-size:11.5px;color:#b7c6d6;margin-top:1px}
.tabs{display:flex;gap:4px;background:rgba(255,255,255,.08);padding:4px;border-radius:9px}
.tabs button{border:0;background:transparent;color:#cdd8e4;font:inherit;font-size:13px;font-weight:600;
  padding:7px 14px;border-radius:6px;cursor:pointer;display:flex;align-items:center;gap:7px}
.tabs button.on{background:#fff;color:var(--steel)}
.tabs em{font-style:normal;font-size:11px;background:var(--blue);color:#fff;border-radius:20px;
  padding:1px 7px;font-weight:700}
.tabs button.on em{background:var(--steel);color:#fff}

.pane{max-width:1080px;margin:0 auto;padding:20px 18px 120px}
.card{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px;
  box-shadow:0 1px 2px rgba(20,30,45,.04)}

.head-card{margin-bottom:14px}
.head-grid{display:grid;grid-template-columns:2fr 1fr 1fr 1fr;gap:12px}
.field{display:flex;flex-direction:column;gap:5px}
.field>span{font-size:11px;font-weight:700;letter-spacing:.5px;text-transform:uppercase;color:var(--muted)}
.field input,.field select,.field textarea,.subfield input{
  font:inherit;font-size:14px;padding:9px 10px;border:1px solid var(--line);border-radius:8px;
  background:#fbfcfe;color:var(--ink);width:100%}
.field textarea{resize:vertical}
.field input:focus,.field select:focus,.field textarea:focus{outline:2px solid var(--blue);outline-offset:0;border-color:transparent}

.toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px;margin:14px 0 10px;flex-wrap:wrap}
.chips{display:flex;gap:6px;flex-wrap:wrap;align-items:center}
.chip{border:1px solid var(--line);background:#fff;color:var(--muted);font:inherit;font-size:12.5px;font-weight:600;
  padding:6px 12px;border-radius:20px;cursor:pointer}
.chip.on{background:var(--steel);color:#fff;border-color:var(--steel)}
.chip-div{width:1px;height:20px;background:var(--line);margin:0 4px}
.ghost{border:1px solid var(--line);background:#fff;color:var(--ink);font:inherit;font-size:13px;font-weight:600;
  padding:8px 14px;border-radius:8px;cursor:pointer}
.ghost:hover{background:#f4f7fb}
.ghost.danger{color:var(--red);border-color:#f0c9c9}

.rows{display:flex;flex-direction:column;gap:7px}
.row{display:flex;background:#fff;border:1px solid var(--line);border-radius:10px;overflow:hidden;
  flex-wrap:wrap;transition:border-color .12s}
.row.state-nok{border-color:#e7a3a3;background:#fffafa}
.row.state-todo{background:#fcfdfe}
.spine{width:6px;flex-shrink:0}
.spine.green{background:var(--green)} .spine.amber{background:var(--amber)} .spine.red{background:var(--red)}
.row-main{display:flex;align-items:center;gap:12px;padding:11px 14px;flex:1;min-width:0}
.row-id{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--faint);width:26px;text-align:right;flex-shrink:0}
.row-name{flex:1;min-width:0}
.nm{font-size:14.5px;font-weight:600;line-height:1.2;display:flex;align-items:center;gap:8px}
.xx{font-size:10px;font-weight:800;color:#fff;background:var(--red);border-radius:4px;padding:1px 5px;letter-spacing:.5px}
.meta{display:flex;gap:6px;margin-top:5px}
.pill{font-size:10.5px;font-weight:700;letter-spacing:.4px;padding:2px 7px;border-radius:5px}
.pill.green{color:var(--green);background:var(--green-bg)}
.pill.amber{color:var(--amber);background:var(--amber-bg)}
.pill.red{color:var(--red);background:var(--red-bg)}
.pill.grey{color:var(--muted);background:var(--grey-bg)}

.seg{display:inline-flex;border:1px solid var(--line);border-radius:8px;overflow:hidden;background:#f4f7fb;flex-shrink:0}
.seg-btn{border:0;background:transparent;font:inherit;font-weight:700;color:var(--muted);cursor:pointer;
  border-right:1px solid var(--line)}
.seg-md .seg-btn{font-size:12.5px;padding:8px 13px}
.seg-sm .seg-btn{font-size:11px;padding:5px 9px}
.seg-btn:last-child{border-right:0}
.seg-btn.on.green{background:var(--green);color:#fff}
.seg-btn.on.red{background:var(--red);color:#fff}
.seg-btn.on.grey{background:#64748b;color:#fff}
.seg-btn.on.blue{background:var(--blue);color:#fff}
.disc{width:32px;height:32px;flex-shrink:0;border:1px solid var(--line);background:#fff;border-radius:8px;
  font-size:18px;font-weight:600;color:var(--muted);cursor:pointer;line-height:1}
.disc.on{background:var(--steel);color:#fff;border-color:var(--steel)}

.row-note{width:100%;padding:0 14px 11px 52px;display:flex;align-items:center;gap:9px;flex-wrap:wrap}
.note-part{font-size:12px;color:var(--blue)}
.note-dev{font-size:12.5px;color:var(--muted)}
.row-detail{width:100%;padding:4px 16px 16px 52px;border-top:1px dashed var(--line);
  display:flex;flex-direction:column;gap:12px;margin-top:2px}
.sub-checks{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}
.subfield{display:flex;flex-direction:column;gap:6px}
.subfield>span{font-size:11px;font-weight:600;color:var(--muted)}
.detail-grid{display:grid;grid-template-columns:2fr 1fr 1fr;gap:12px}

.other-card{margin-top:14px}

.footbar{position:fixed;bottom:0;left:0;right:0;z-index:15;background:#fff;border-top:1px solid var(--line);
  padding:12px 18px;display:flex;align-items:center;justify-content:space-between;gap:16px;
  box-shadow:0 -2px 10px rgba(20,30,45,.05)}
.progress{display:flex;align-items:center;gap:12px;flex:1;min-width:0}
.pbar{flex:1;max-width:340px;height:8px;background:#e6ebf1;border-radius:20px;overflow:hidden}
.pbar span{display:block;height:100%;background:var(--blue);border-radius:20px;transition:width .2s}
.pnum{font-size:13px;color:var(--muted);white-space:nowrap}
.pnum strong{color:var(--ink);font-size:15px}
.fail-count{color:var(--red);font-weight:700;margin-left:10px}
.foot-actions{display:flex;gap:8px}
.primary{border:0;background:var(--steel);color:#fff;font:inherit;font-size:14px;font-weight:700;
  padding:10px 20px;border-radius:8px;cursor:pointer}
.primary:hover{background:var(--steel2)}
.primary:disabled{opacity:.4;cursor:not-allowed}

.toast{position:fixed;bottom:78px;left:50%;transform:translateX(-50%);z-index:30;background:var(--ink);
  color:#fff;padding:11px 20px;border-radius:9px;font-size:13.5px;font-weight:600;box-shadow:0 8px 24px rgba(0,0,0,.2)}

/* dashboard */
.kpi-row{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:16px}
.kpi{background:#fff;border:1px solid var(--line);border-radius:12px;padding:16px 18px}
.kpi-val{font-size:30px;font-weight:800;letter-spacing:-.5px;font-family:ui-monospace,Menlo,monospace}
.kpi-lbl{font-size:12px;color:var(--muted);font-weight:600;margin-top:2px}
.kpi.red .kpi-val{color:var(--red)} .kpi.green .kpi-val{color:var(--green)}

.dash-grid{display:grid;grid-template-columns:1.5fr 1fr;gap:14px;align-items:start}
.card-hd{display:flex;align-items:baseline;justify-content:space-between;margin-bottom:12px}
.card-hd h3{margin:0;font-size:14px;font-weight:700}
.hd-note{font-size:11px;color:var(--faint);text-transform:uppercase;letter-spacing:.5px}
.side{display:flex;flex-direction:column;gap:14px}

.fail-list{display:flex;flex-direction:column;gap:9px}
.fail{display:flex;gap:11px;border:1px solid var(--line);border-radius:10px;padding:11px;background:#fffafa}
.sev-tag{flex-shrink:0;font-size:10.5px;font-weight:800;letter-spacing:.4px;color:#fff;border-radius:6px;
  padding:5px 8px;height:fit-content}
.sev-tag.red{background:var(--red)} .sev-tag.amber{background:var(--amber)} .sev-tag.green{background:var(--green)}
.fail-body{flex:1;min-width:0}
.fail-top{display:flex;justify-content:space-between;gap:10px;align-items:baseline}
.fail-name{font-weight:700;font-size:14px}
.fail-when{font-size:11.5px;color:var(--faint);white-space:nowrap;font-family:ui-monospace,Menlo,monospace}
.fail-dev{font-size:13px;color:var(--muted);margin:3px 0 7px}
.fail-tags{display:flex;gap:6px;flex-wrap:wrap}
.tag{font-size:11px;font-weight:600;color:var(--muted);background:var(--grey-bg);border-radius:5px;padding:2px 8px}
.tag.act{color:var(--steel);background:#e3edf5}
.tag.warn{color:var(--red);background:var(--red-bg)}
.clean,.empty p{color:var(--muted)}
.clean{padding:20px;text-align:center;font-size:14px}

.bar-row{display:flex;align-items:center;gap:10px;margin:9px 0}
.bar-lbl{width:120px;font-size:13px;font-weight:600;display:flex;align-items:center;gap:6px}
.bar-lbl em{font-style:normal;font-size:11px;color:var(--faint);font-weight:500;margin-left:2px}
.dot{width:9px;height:9px;border-radius:50%}
.dot.green{background:var(--green)} .dot.amber{background:var(--amber)} .dot.red{background:var(--red)}
.bar-track{flex:1;height:9px;background:#e9edf3;border-radius:20px;overflow:hidden}
.bar-track span{display:block;height:100%;border-radius:20px}
.bar-track span.green{background:var(--green)} .bar-track span.red{background:var(--red)} .bar-track span.amber{background:var(--amber)}
.bar-val{width:44px;text-align:right;font-size:13px;font-weight:700;font-family:ui-monospace,Menlo,monospace}

.rec{display:flex;justify-content:space-between;align-items:center;padding:9px 0;border-bottom:1px solid var(--line)}
.rec:last-child{border-bottom:0}
.rec-main{font-size:13px}
.rec-stats{display:flex;gap:10px;font-size:12px;font-family:ui-monospace,Menlo,monospace}
.rec-stats .good{color:var(--green)} .rec-stats .bad{color:var(--red);font-weight:700}

.empty{text-align:center;padding:60px 20px;max-width:460px;margin:0 auto}
.empty-mark{font-size:40px;color:var(--faint)}
.empty h2{margin:12px 0 6px;font-size:20px}
.empty-actions{display:flex;gap:10px;justify-content:center;margin-top:20px}
.dash-foot{display:flex;justify-content:flex-end;gap:10px;margin-top:16px}

@media (max-width:820px){
  .head-grid{grid-template-columns:1fr 1fr}
  .dash-grid{grid-template-columns:1fr}
  .kpi-row{grid-template-columns:repeat(2,1fr)}
  .detail-grid,.sub-checks{grid-template-columns:1fr}
  .row-detail,.row-note{padding-left:16px}
  .row-main{flex-wrap:wrap}
  .seg{order:3;width:100%}.seg-md .seg-btn{flex:1}
}
`;
