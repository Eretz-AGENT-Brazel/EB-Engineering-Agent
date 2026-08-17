'use strict';
/* EB AI — Mission Control. Talks to the local JSON API in server.py. */

const API = {
  async all(){ return (await fetch('/api')).json(); },
  async get(c){ return (await fetch('/api/'+c)).json(); },
  async add(c, item){ return (await fetch('/api/'+c,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(item)})).json(); },
  async patch(c, id, fields){ return (await fetch('/api/'+c+'/'+id,{method:'PATCH',headers:{'Content-Type':'application/json'},body:JSON.stringify(fields)})).json(); },
  async del(c, id){ return (await fetch('/api/'+c+'/'+id,{method:'DELETE'})).json(); },
  async raw(path){ const r=await fetch('/raw?path='+encodeURIComponent(path)); return r.ok ? r.text() : '*Could not load '+path+'*'; }
};

let DB = {};           // cached collections
let TRACKS = {};       // id -> {name,color}
const $ = s => document.querySelector(s);
const esc = s => (s==null?'':String(s)).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));

const NAV = [
  {id:'dashboard', label:'Dashboard', ic:'◈'},
  {id:'roadmap',   label:'Roadmap',   ic:'≣'},
  {id:'sessions',  label:'Sessions',  ic:'❡'},
  {id:'inbox',     label:'Inbox',     ic:'✉'},
  {id:'decisions', label:'Decisions', ic:'✓'},
  {id:'tasks',     label:'Tasks',     ic:'☑'},
  {id:'knowledge', label:'Knowledge', ic:'❑'},
  {id:'projects',  label:'Projects',  ic:'▣'},
];

const STATUS_CLASS = {done:'st-done',active:'st-active',todo:'st-todo',blocked:'st-blocked',doing:'st-doing',
  open:'st-open',accepted:'st-accepted',deferred:'st-deferred'};
const statusChip = s => `<span class="chip ${STATUS_CLASS[s]||'st-todo'}">${esc(s)}</span>`;
const trackChip = t => { const k=TRACKS[t]||{name:t,color:'#888'}; return `<span class="chip" style="background:${k.color}1a;color:${k.color}"><span class="dot" style="background:${k.color}"></span>${esc(k.name)}</span>`; };

async function boot(){
  DB = await API.all();
  (DB.program.tracks||[]).forEach(t=>TRACKS[t.id]=t);
  $('#phase-chip').textContent = DB.program.currentPhase || '';
  $('#updated').textContent = 'updated ' + (DB.program.updated||'');
  buildNav();
  $('#search').addEventListener('input', e=>{ if(e.target.value.trim()) renderSearch(e.target.value.trim()); else go(current); });
  $('#modal-close').onclick = closeModal;
  $('#modal-backdrop').onclick = e=>{ if(e.target.id==='modal-backdrop') closeModal(); };
  window.addEventListener('hashchange', ()=>go(location.hash.slice(1)||'dashboard'));
  go(location.hash.slice(1)||'dashboard');
}

let current='dashboard';
function buildNav(){
  const counts = {
    inbox: (DB.suggestions||[]).filter(s=>s.status==='open').length,
    tasks: (DB.tasks||[]).filter(t=>t.status!=='done').length,
  };
  $('#nav').innerHTML = NAV.map(n=>{
    const b = counts[n.id] ? `<span class="badge">${counts[n.id]}</span>`:'';
    return `<button class="nav-item ${n.id===current?'active':''}" data-go="${n.id}"><span class="ic">${n.ic}</span>${n.label}${b}</button>`;
  }).join('');
  $('#nav').querySelectorAll('[data-go]').forEach(b=>b.onclick=()=>{location.hash=b.dataset.go;});
}

async function refresh(){ DB = await API.all(); (DB.program.tracks||[]).forEach(t=>TRACKS[t.id]=t); buildNav(); }
async function go(view){
  current = view; if(!NAV.find(n=>n.id===view)) current='dashboard';
  $('#search').value='';
  $('#page-title').textContent = (NAV.find(n=>n.id===current)||{}).label;
  $('#nav').querySelectorAll('.nav-item').forEach(b=>b.classList.toggle('active',b.dataset.go===current));
  await refresh();
  RENDER[current]();
}

/* ---------------- views ---------------- */
const RENDER = {};

RENDER.dashboard = ()=>{
  const q = (DB.questions||[]).filter(x=>!x.answer);
  const next = (DB.tasks||[]).filter(t=>t.next && t.status!=='done');
  const recent = [...(DB.sessions||[])].sort((a,b)=>(b.date+b.id).localeCompare(a.date+a.id)).slice(0,4);
  // track progress
  const tracks = Object.values(TRACKS);
  const prog = tracks.map(t=>{
    const items=(DB.roadmap||[]).filter(r=>r.track===t.id);
    const done=items.filter(r=>r.status==='done').length;
    const pct=items.length?Math.round(done/items.length*100):0;
    return {t,done,total:items.length,pct};
  });

  $('#view').innerHTML = `
    <div class="grid cols-2">
      <div class="card" style="border-left:4px solid var(--amber)">
        <h2>Waiting on you</h2>
        ${q.length? q.map(x=>`
          <div class="row"><div class="grow">
            <div class="title">${esc(x.question)}</div>
            <div class="meta">${esc(x.context||'')}</div>
          </div><button class="btn sm" data-answer="${x.id}">Answer</button></div>`).join('')
          : '<div class="empty">Nothing waiting — all clear.</div>'}
      </div>
      <div class="card">
        <h2>Next actions</h2>
        ${next.length? next.map(t=>`
          <div class="row">
            <span class="dot" style="background:${(TRACKS[t.track]||{}).color||'#888'};margin-top:6px"></span>
            <div class="grow"><div class="title">${esc(t.title)}</div>
              <div class="meta">${trackChip(t.track)} · ${esc(t.owner)} ${statusChip(t.status)}</div></div>
          </div>`).join('') : '<div class="empty">No flagged actions.</div>'}
      </div>
    </div>

    <div class="section-title">Track progress</div>
    <div class="grid cols-3">
      ${prog.map(p=>`
        <div class="card">
          <div class="kpi" style="color:${p.t.color}">${p.pct}%</div>
          <div style="font-weight:700;margin-top:4px">${esc(p.t.name)}</div>
          <div class="meta">${p.done} / ${p.total} phases done</div>
          <div class="prog"><i style="width:${p.pct}%;background:${p.t.color}"></i></div>
        </div>`).join('')}
    </div>

    <div class="section-title">Recent sessions</div>
    <div class="card">
      ${recent.map(s=>`
        <div class="row"><div class="grow">
          <div class="title">${esc(s.title)}</div>
          <div class="meta">${esc(s.date)} · ${trackChip(s.track)}</div>
        </div><button class="btn sm" data-session="${s.id}">Open</button></div>`).join('')}
    </div>`;

  $('#view').querySelectorAll('[data-answer]').forEach(b=>b.onclick=()=>answerQuestion(b.dataset.answer));
  $('#view').querySelectorAll('[data-session]').forEach(b=>b.onclick=()=>{location.hash='sessions';});
};

RENDER.roadmap = ()=>{
  const tracks = Object.values(TRACKS);
  $('#view').innerHTML = tracks.map(t=>{
    const items=(DB.roadmap||[]).filter(r=>r.track===t.id);
    return `<div class="section-title">${esc(t.name)}</div>
      <div class="card">${items.map(r=>`
        <div class="row">
          <span class="dot" style="background:${t.color};margin-top:6px"></span>
          <div class="grow"><div class="title">${esc(r.title)}</div><div class="meta">${esc(r.detail||'')}</div></div>
          <select class="right" data-rstatus="${r.id}">
            ${['todo','active','blocked','done'].map(s=>`<option value="${s}" ${s===r.status?'selected':''}>${s}</option>`).join('')}
          </select>
        </div>`).join('')}</div>`;
  }).join('');
  $('#view').querySelectorAll('[data-rstatus]').forEach(sel=>sel.onchange=async()=>{
    await API.patch('roadmap', sel.dataset.rstatus, {status:sel.value}); go('roadmap');
  });
};

RENDER.sessions = ()=>{
  const list=[...(DB.sessions||[])].sort((a,b)=>(b.date+b.id).localeCompare(a.date+a.id));
  $('#view').innerHTML = `
    <div class="add-bar"><button class="btn primary" id="add-session">+ Log a session</button></div>
    ${list.map(s=>`
      <div class="card" style="margin-top:14px">
        <div style="display:flex;align-items:center;gap:10px">
          <strong style="font-size:15px">${esc(s.title)}</strong>
          <span class="right meta">${esc(s.date)} ${trackChip(s.track)}</span>
        </div>
        <p style="margin:8px 0 6px">${esc(s.summary||'')}</p>
        ${(s.changes&&s.changes.length)?`<ul class="muted" style="margin:6px 0">${s.changes.map(c=>`<li>${esc(c)}</li>`).join('')}</ul>`:''}
        ${(s.files&&s.files.length)?`<div class="meta">files: ${s.files.map(f=>`<code>${esc(f)}</code>`).join(' ')}</div>`:''}
      </div>`).join('') || '<div class="empty">No sessions yet.</div>'}`;
  $('#add-session').onclick=()=>formModal('Log a session','sessions',[
    {k:'title',label:'Title'},{k:'track',label:'Track',type:'track'},
    {k:'summary',label:'Summary',type:'textarea'},
  ]);
};

RENDER.inbox = ()=>{
  const list=DB.suggestions||[];
  const order={open:0,accepted:1,deferred:2,done:3};
  list.sort((a,b)=>(order[a.status]??9)-(order[b.status]??9));
  $('#view').innerHTML = `
    <div class="add-bar"><button class="btn primary" id="add-sug">+ Add suggestion / idea</button></div>
    <div class="card" style="margin-top:14px">
      ${list.length? list.map(s=>`
        <div class="row"><div class="grow">
          <div class="title">${esc(s.text)}</div>
          <div class="meta">${esc(s.date)} · from ${esc(s.from)} · ${trackChip(s.project)} ${statusChip(s.status)}
            ${s.notes?'· '+esc(s.notes):''}</div>
          </div>
          <div class="actions">
            <select data-sstatus="${s.id}">
              ${['open','accepted','deferred','done'].map(x=>`<option ${x===s.status?'selected':''}>${x}</option>`).join('')}
            </select>
            <button class="icon" data-del-sug="${s.id}" title="delete">🗑</button>
          </div>
        </div>`).join('') : '<div class="empty">Inbox is empty.</div>'}
    </div>`;
  $('#add-sug').onclick=()=>formModal('Add suggestion / idea','suggestions',[
    {k:'text',label:'Idea',type:'textarea'},
    {k:'project',label:'Track',type:'track'},
    {k:'from',label:'From',type:'select',opts:['amir','claude'],def:'amir'},
    {k:'status',label:'Status',type:'select',opts:['open','accepted','deferred','done'],def:'open'},
  ]);
  $('#view').querySelectorAll('[data-sstatus]').forEach(sel=>sel.onchange=async()=>{await API.patch('suggestions',sel.dataset.sstatus,{status:sel.value});go('inbox');});
  $('#view').querySelectorAll('[data-del-sug]').forEach(b=>b.onclick=async()=>{if(confirm('Delete this item?')){await API.del('suggestions',b.dataset.delSug);go('inbox');}});
};

RENDER.decisions = ()=>{
  const list=[...(DB.decisions||[])].sort((a,b)=>(b.date).localeCompare(a.date));
  $('#view').innerHTML = `
    <div class="add-bar"><button class="btn primary" id="add-dec">+ Record a decision</button></div>
    <div class="card" style="margin-top:14px">
      ${list.map(d=>`
        <div class="row"><div class="grow">
          <div class="title">${esc(d.decision)}</div>
          <div class="meta"><strong>Why:</strong> ${esc(d.why)}</div>
          <div class="meta">${esc(d.date)} · ${trackChip(d.track)}</div>
        </div></div>`).join('')}
    </div>`;
  $('#add-dec').onclick=()=>formModal('Record a decision','decisions',[
    {k:'decision',label:'Decision'},{k:'why',label:'Why',type:'textarea'},{k:'track',label:'Track',type:'track'},
  ]);
};

RENDER.tasks = ()=>{
  const list=DB.tasks||[];
  const order={blocked:0,doing:1,todo:2,done:3};
  list.sort((a,b)=>(order[a.status]??9)-(order[b.status]??9));
  $('#view').innerHTML = `
    <div class="add-bar"><button class="btn primary" id="add-task">+ Add task</button></div>
    <div class="card" style="margin-top:14px">
      ${list.map(t=>`
        <div class="row">
          <input type="checkbox" data-check="${t.id}" ${t.status==='done'?'checked':''} style="margin-top:4px;width:17px;height:17px">
          <div class="grow">
            <div class="title" style="${t.status==='done'?'text-decoration:line-through;color:var(--muted)':''}">${esc(t.title)}</div>
            <div class="meta">${trackChip(t.track)} · ${esc(t.owner)} ${t.next?'· ⭐ next':''}</div>
          </div>
          <div class="actions">
            <select data-tstatus="${t.id}">
              ${['todo','doing','blocked','done'].map(s=>`<option ${s===t.status?'selected':''}>${s}</option>`).join('')}
            </select>
            <button class="icon" data-del-task="${t.id}" title="delete">🗑</button>
          </div>
        </div>`).join('')}
    </div>`;
  $('#add-task').onclick=()=>formModal('Add task','tasks',[
    {k:'title',label:'Task'},{k:'track',label:'Track',type:'track'},
    {k:'owner',label:'Owner',type:'select',opts:['amir','claude'],def:'amir'},
    {k:'status',label:'Status',type:'select',opts:['todo','doing','blocked','done'],def:'todo'},
  ]);
  const upd=async(id,fields)=>{await API.patch('tasks',id,fields);go('tasks');};
  $('#view').querySelectorAll('[data-check]').forEach(c=>c.onchange=()=>upd(c.dataset.check,{status:c.checked?'done':'todo'}));
  $('#view').querySelectorAll('[data-tstatus]').forEach(s=>s.onchange=()=>upd(s.dataset.tstatus,{status:s.value}));
  $('#view').querySelectorAll('[data-del-task]').forEach(b=>b.onclick=async()=>{if(confirm('Delete task?')){await API.del('tasks',b.dataset.delTask);go('tasks');}});
};

const DOCS = [
  {label:'Running Plan', path:'web/RUNNING-PLAN.md'},
  {label:'Knowledge index', path:'web/knowledge/README.md'},
  {label:'Materials', path:'web/knowledge/materials.md'},
  {label:'Drawing conventions', path:'web/knowledge/drawing-conventions.md'},
  {label:'Example #10,015', path:'web/knowledge/examples/tank-10015.md'},
  {label:'Caps', path:'web/knowledge/elements/caps.md'},
  {label:'Shells', path:'web/knowledge/elements/shells.md'},
  {label:'Standoffs', path:'web/knowledge/elements/standoffs.md'},
  {label:'Manholes & nozzles', path:'web/knowledge/elements/manholes-nozzles.md'},
  {label:'UL 142', path:'web/standards/UL142.md'},
  {label:'UL 58', path:'web/standards/UL58.md'},
];
RENDER.knowledge = async ()=>{
  $('#view').innerHTML = `<div class="doc-tabs">${DOCS.map((d,i)=>`<span class="doc-tab ${i===0?'active':''}" data-doc="${i}">${esc(d.label)}</span>`).join('')}</div>
    <div class="card md" id="doc-body">Loading…</div>`;
  const showDoc = async i=>{
    $('#view').querySelectorAll('.doc-tab').forEach((t,j)=>t.classList.toggle('active',i===j));
    $('#doc-body').innerHTML = mdToHtml(await API.raw(DOCS[i].path));
  };
  $('#view').querySelectorAll('[data-doc]').forEach(t=>t.onclick=()=>showDoc(+t.dataset.doc));
  showDoc(0);
};

RENDER.projects = ()=>{
  $('#view').innerHTML = `<div class="grid cols-2">${(DB.projects||[]).map(p=>`
    <div class="card">
      <div style="display:flex;align-items:center;gap:8px">
        <strong style="font-size:15px">${esc(p.name)}</strong>
        <span class="right">${statusChip((p.status||'').toLowerCase().includes('done')?'done':'active')}</span>
      </div>
      <div class="meta" style="margin:4px 0 8px">${trackChip(p.track)} · ${esc(p.status)}</div>
      <p>${esc(p.summary||'')}</p>
      <div class="meta"><code>${esc(p.path)}</code></div>
    </div>`).join('')}</div>`;
};

/* ---------------- search ---------------- */
function renderSearch(q){
  $('#page-title').textContent='Search: '+q;
  const ql=q.toLowerCase();
  const hit=o=>JSON.stringify(o).toLowerCase().includes(ql);
  const blocks=[
    ['Tasks', (DB.tasks||[]).filter(hit), t=>`${esc(t.title)} ${statusChip(t.status)}`],
    ['Suggestions', (DB.suggestions||[]).filter(hit), s=>`${esc(s.text)} ${statusChip(s.status)}`],
    ['Decisions', (DB.decisions||[]).filter(hit), d=>`${esc(d.decision)}`],
    ['Sessions', (DB.sessions||[]).filter(hit), s=>`${esc(s.title)} — ${esc(s.summary||'')}`],
    ['Roadmap', (DB.roadmap||[]).filter(hit), r=>`${esc(r.title)} ${statusChip(r.status)}`],
    ['Questions', (DB.questions||[]).filter(hit), x=>`${esc(x.question)}`],
  ].filter(b=>b[1].length);
  $('#view').innerHTML = blocks.length? blocks.map(([t,arr,fn])=>`
    <div class="section-title">${t} (${arr.length})</div>
    <div class="card">${arr.map(o=>`<div class="row"><div class="grow">${fn(o)}</div></div>`).join('')}</div>`).join('')
    : '<div class="empty">No matches.</div>';
}

/* ---------------- modal / forms ---------------- */
function formModal(title, collection, fields){
  $('#modal-title').textContent=title;
  $('#modal-body').innerHTML = fields.map(f=>{
    if(f.type==='textarea') return `<div class="field"><label>${f.label}</label><textarea data-k="${f.k}"></textarea></div>`;
    if(f.type==='track'){ const o=Object.values(TRACKS).map(t=>`<option value="${t.id}">${esc(t.name)}</option>`).join(''); return `<div class="field"><label>${f.label}</label><select data-k="${f.k}">${o}</select></div>`; }
    if(f.type==='select'){ const o=f.opts.map(v=>`<option ${v===f.def?'selected':''}>${v}</option>`).join(''); return `<div class="field"><label>${f.label}</label><select data-k="${f.k}">${o}</select></div>`; }
    return `<div class="field"><label>${f.label}</label><input data-k="${f.k}"></div>`;
  }).join('') + `<div style="display:flex;gap:8px;justify-content:flex-end;margin-top:6px">
      <button class="btn ghost" id="m-cancel">Cancel</button>
      <button class="btn primary" id="m-save">Save</button></div>`;
  openModal();
  $('#m-cancel').onclick=closeModal;
  $('#m-save').onclick=async()=>{
    const item={};
    $('#modal-body').querySelectorAll('[data-k]').forEach(el=>item[el.dataset.k]=el.value.trim());
    await API.add(collection,item); closeModal(); go(current);
  };
}
async function answerQuestion(id){
  const qn=(DB.questions||[]).find(x=>x.id===id);
  $('#modal-title').textContent='Answer';
  $('#modal-body').innerHTML=`<p style="margin-top:0"><strong>${esc(qn.question)}</strong></p>
    <div class="field"><label>Your answer</label><textarea id="ans"></textarea></div>
    <div style="display:flex;gap:8px;justify-content:flex-end"><button class="btn ghost" id="m-cancel">Cancel</button><button class="btn primary" id="m-save">Save</button></div>`;
  openModal();
  $('#m-cancel').onclick=closeModal;
  $('#m-save').onclick=async()=>{await API.patch('questions',id,{answer:$('#ans').value.trim()});closeModal();go(current);};
}
function openModal(){ $('#modal-backdrop').classList.remove('hidden'); }
function closeModal(){ $('#modal-backdrop').classList.add('hidden'); }

/* ---------------- tiny markdown renderer ---------------- */
function mdInline(s){
  return esc(s)
    .replace(/`([^`]+)`/g,'<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g,'<strong>$1</strong>')
    .replace(/(^|[^*])\*([^*]+)\*/g,'$1<em>$2</em>')
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g,'<a href="$2" target="_blank">$1</a>');
}
function mdToHtml(md){
  const lines=md.replace(/\r/g,'').split('\n'); let html=''; let i=0;
  const flushTable=(rows)=>{
    if(rows.length<2) return rows.map(r=>`<p>${mdInline(r)}</p>`).join('');
    const cells=r=>r.replace(/^\||\|$/g,'').split('|').map(c=>c.trim());
    const head=cells(rows[0]); const body=rows.slice(2).map(cells);
    return `<table><thead><tr>${head.map(h=>`<th>${mdInline(h)}</th>`).join('')}</tr></thead><tbody>${
      body.map(r=>`<tr>${r.map(c=>`<td>${mdInline(c)}</td>`).join('')}</tr>`).join('')}</tbody></table>`;
  };
  while(i<lines.length){
    let ln=lines[i];
    if(/^```/.test(ln)){ let buf=[]; i++; while(i<lines.length && !/^```/.test(lines[i])){buf.push(lines[i]);i++;} i++; html+=`<pre><code>${esc(buf.join('\n'))}</code></pre>`; continue; }
    if(/^\s*\|.*\|\s*$/.test(ln)){ let rows=[]; while(i<lines.length && /^\s*\|.*\|\s*$/.test(lines[i])){rows.push(lines[i].trim());i++;} html+=flushTable(rows); continue; }
    const h=ln.match(/^(#{1,6})\s+(.*)/); if(h){ html+=`<h${h[1].length}>${mdInline(h[2])}</h${h[1].length}>`; i++; continue; }
    if(/^\s*>\s?/.test(ln)){ let buf=[]; while(i<lines.length && /^\s*>\s?/.test(lines[i])){buf.push(lines[i].replace(/^\s*>\s?/,''));i++;} html+=`<blockquote>${mdInline(buf.join(' '))}</blockquote>`; continue; }
    if(/^(\s*[-*]\s+)/.test(ln)){ let buf=[]; while(i<lines.length && /^(\s*[-*]\s+)/.test(lines[i])){buf.push(lines[i].replace(/^\s*[-*]\s+/,''));i++;} html+=`<ul>${buf.map(b=>`<li>${mdInline(b)}</li>`).join('')}</ul>`; continue; }
    if(/^\s*\d+\.\s+/.test(ln)){ let buf=[]; while(i<lines.length && /^\s*\d+\.\s+/.test(lines[i])){buf.push(lines[i].replace(/^\s*\d+\.\s+/,''));i++;} html+=`<ol>${buf.map(b=>`<li>${mdInline(b)}</li>`).join('')}</ol>`; continue; }
    if(/^\s*(-{3,}|\*{3,})\s*$/.test(ln)){ html+='<hr>'; i++; continue; }
    if(ln.trim()===''){ i++; continue; }
    let buf=[]; while(i<lines.length && lines[i].trim()!=='' && !/^(#{1,6}\s|\s*[-*]\s|\s*\d+\.\s|\s*\||```|\s*>)/.test(lines[i])){buf.push(lines[i]);i++;}
    html+=`<p>${mdInline(buf.join(' '))}</p>`;
  }
  return html;
}

boot();
