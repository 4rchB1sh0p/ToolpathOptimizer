"use strict";

// Display-only client: every metric shown comes from the server report; nothing here is authoritative.
(() => {
    const SAMPLE_CSV = [
        "point_id,x_mm,y_mm,sequence,operation_group",
        "P01,0,0,1,", "P02,200,90,2,", "P03,40,30,3,", "P04,160,0,4,",
        "P05,80,60,5,TAP", "P06,120,60,6,TAP", "P07,160,60,7,TAP", "P08,0,90,8,",
        "P09,200,0,9,", "P10,40,90,10,", "P11,120,0,11,", "P12,0,30,12,",
        "P13,200,60,13,", "P14,80,0,14,", "P15,160,30,15,", "P16,40,0,16,",
        "P17,120,90,17,", "P18,0,60,18,", "P19,200,30,19,", "P20,80,90,20,",
        "P21,40,60,21,", "P22,120,30,22,", "P23,160,90,23,", "P24,80,30,24,"
    ].join("\n") + "\n";

    const SAMPLE_MANIFEST = JSON.stringify({
        schema_version: "1.0",
        job_id: "plate-demo",
        coordinate_system: "XY_CARTESIAN",
        units: "mm",
        start_x_mm: -20, start_y_mm: -20, end_x_mm: -20, end_y_mm: -20,
        constraints: {
            precedence: [{ before: "P01", after: "P20" }, { before: "P10", after: "P03" }],
            locked_groups: ["TAP"]
        },
        algorithm: { name: "nearest-neighbor-2opt", seed: 0, max_passes: 50 }
    });

    const $ = (id) => document.getElementById(id);
    const state = { inputId: null, run: null, report: null };

    function headers(extra) {
        return Object.assign({
            "X-Dev-Tenant": $("tenant").value.trim(),
            "X-Dev-User": $("user").value.trim(),
            "X-Dev-Role": $("role").value
        }, extra || {});
    }

    async function api(method, path, body, extraHeaders) {
        const init = { method, headers: headers(extraHeaders) };
        if (body instanceof FormData) {
            init.body = body;
        } else if (body !== undefined) {
            init.headers["Content-Type"] = "application/json";
            init.body = JSON.stringify(body);
        }
        const response = await fetch(path, init);
        if (!response.ok) {
            let problem = { title: response.statusText, detail: "", code: String(response.status) };
            try { problem = await response.json(); } catch { /* non-JSON error */ }
            throw problem;
        }
        return response;
    }

    function showError(problem) {
        const box = $("error");
        box.hidden = false;
        box.textContent = `${problem.code || problem.status}: ${problem.detail || problem.title}`;
    }

    function clearError() {
        $("error").hidden = true;
        $("error").textContent = "";
    }

    async function guarded(action) {
        clearError();
        try { await action(); } catch (problem) { showError(problem); }
    }

    async function refreshProjects(selectId) {
        const projects = await (await api("GET", "/api/v1/projects")).json();
        const select = $("project");
        select.replaceChildren(...projects.map((p) => {
            const option = document.createElement("option");
            option.value = p.id;
            option.textContent = p.name;
            return option;
        }));
        if (selectId) { select.value = selectId; }
    }

    async function createProject() {
        const project = await (await api("POST", "/api/v1/projects", { name: $("project-name").value })).json();
        await refreshProjects(project.id);
    }

    async function upload(pointsBlob, manifestBlob) {
        const projectId = $("project").value;
        if (!projectId) { throw { code: "NO_PROJECT", detail: "Create or select a project first." }; }
        const form = new FormData();
        form.append("points", pointsBlob, "points.csv");
        form.append("manifest", manifestBlob, "job.json");
        const input = await (await api("POST", `/api/v1/projects/${projectId}/inputs`, form)).json();
        state.inputId = input.id;
        $("input-status").textContent = `Input accepted: ${input.point_count} points, sha256 ${input.input_sha256.slice(0, 16)}…`;
        $("run").disabled = false;
    }

    async function uploadSelected() {
        const points = $("points-file").files[0];
        const manifest = $("manifest-file").files[0];
        if (!points || !manifest) { throw { code: "NO_FILES", detail: "Choose both a points CSV and a manifest JSON." }; }
        await upload(points, manifest);
    }

    async function runAnalysis() {
        const projectId = $("project").value;
        $("run-status").textContent = "Running…";
        const run = await (await api("POST", `/api/v1/projects/${projectId}/analyses`, { input_id: state.inputId },
            { "Idempotency-Key": crypto.randomUUID() })).json();
        state.run = run;
        state.report = await (await api("GET", run.report_url)).json();
        $("run-status").textContent = `Run ${run.id} ${run.status}.`;
        render(state.report);
    }

    function cell(row, text, header) {
        const element = document.createElement(header ? "th" : "td");
        element.textContent = text;
        row.appendChild(element);
    }

    function fmt(value) {
        return value === null || value === undefined ? "—" : Number(value).toFixed(3);
    }

    function render(report) {
        $("results").hidden = false;
        const summary = $("summary").tBodies[0];
        summary.replaceChildren();
        const rows = [
            ["Selected route", `${report.selected_route} (${report.selection_reason})`],
            ["Baseline distance (mm)", fmt(report.baseline_distance_mm)],
            ["Candidate distance (mm)", fmt(report.candidate_distance_mm)],
            ["Delta (mm)", fmt(report.delta_distance_mm)],
            ["Delta (%)", fmt(report.delta_percent)],
            ["Baseline feasible", String(report.constraint_check.baseline.feasible)],
            ["Candidate feasible", String(report.constraint_check.candidate.feasible)],
            ["Search", `${report.search.termination}, ${report.search.passes} passes, ${report.search.accepted_moves} accepted moves`],
            ["Warnings", report.warnings.map((w) => w.code).join(", ") || "none"],
            ["Algorithm", `${report.algorithm_name} ${report.algorithm_version}`],
            ["Input SHA-256", report.input_sha256]
        ];
        for (const [label, value] of rows) {
            const row = summary.insertRow();
            cell(row, label, true);
            cell(row, value, false);
        }

        const points = new Map(report.canonical_job.points.map((p) => [p.point_id, p]));
        const selected = report.selected_route === "candidate" ? report.candidate_order : report.baseline_order;
        const order = $("order").tBodies[0];
        order.replaceChildren();
        selected.forEach((id, index) => {
            const p = points.get(id);
            const row = order.insertRow();
            [String(index + 1), id, fmt(p.x_mm), fmt(p.y_mm), p.operation_group || "—"].forEach((t) => cell(row, t, false));
        });

        drawPlot(report, points, selected);
    }

    function drawPlot(report, points, selected) {
        const NS = "http://www.w3.org/2000/svg";
        const svg = $("plot");
        const title = svg.querySelector("title");
        svg.replaceChildren(title);
        const job = report.canonical_job;
        const start = { x_mm: job.start_x_mm, y_mm: job.start_y_mm };
        const end = job.end_x_mm === null ? null : { x_mm: job.end_x_mm, y_mm: job.end_y_mm };
        const all = [...points.values(), start, ...(end ? [end] : [])];
        const xs = all.map((p) => p.x_mm), ys = all.map((p) => p.y_mm);
        const minX = Math.min(...xs), maxX = Math.max(...xs), minY = Math.min(...ys), maxY = Math.max(...ys);
        const width = 640, height = 420, margin = 30;
        const scale = Math.min((width - 2 * margin) / Math.max(maxX - minX, 1e-9), (height - 2 * margin) / Math.max(maxY - minY, 1e-9));
        const sx = (x) => margin + (x - minX) * scale;
        const sy = (y) => height - margin - (y - minY) * scale;

        const element = (name, attrs) => {
            const e = document.createElementNS(NS, name);
            for (const [k, v] of Object.entries(attrs)) { e.setAttribute(k, String(v)); }
            svg.appendChild(e);
            return e;
        };
        const path = (ids, attrs) => {
            const coords = [start, ...ids.map((id) => points.get(id)), ...(end ? [end] : [])];
            element("polyline", Object.assign({ points: coords.map((p) => `${sx(p.x_mm)},${sy(p.y_mm)}`).join(" "), fill: "none" }, attrs));
        };

        path(report.baseline_order, { stroke: "#9aa0a6", "stroke-width": 1, "stroke-dasharray": "4 4" });
        path(selected, { stroke: "#464feb", "stroke-width": 2 });
        for (const p of points.values()) {
            element("circle", { cx: sx(p.x_mm), cy: sy(p.y_mm), r: 4, fill: p.operation_group ? "#c27c0e" : "#1f2328" });
            const label = element("text", { x: sx(p.x_mm) + 5, y: sy(p.y_mm) - 5, "font-size": 10, fill: "#57606a" });
            label.textContent = p.point_id;
        }
        element("rect", { x: sx(start.x_mm) - 5, y: sy(start.y_mm) - 5, width: 10, height: 10, fill: "#1a7f37" });
        if (end) {
            element("polygon", { points: `${sx(end.x_mm)},${sy(end.y_mm) - 7} ${sx(end.x_mm) + 7},${sy(end.y_mm)} ${sx(end.x_mm)},${sy(end.y_mm) + 7} ${sx(end.x_mm) - 7},${sy(end.y_mm)}`, fill: "none", stroke: "#cf222e", "stroke-width": 2 });
        }
    }

    async function download(format) {
        const response = await api("GET", `${state.run.report_url}?format=${format}`);
        const url = URL.createObjectURL(await response.blob());
        const link = document.createElement("a");
        link.href = url;
        link.download = `run-${state.run.id}.${format}`;
        link.click();
        URL.revokeObjectURL(url);
    }

    document.addEventListener("DOMContentLoaded", () => {
        $("create-project").addEventListener("click", () => guarded(createProject));
        $("refresh-projects").addEventListener("click", () => guarded(() => refreshProjects()));
        $("upload").addEventListener("click", () => guarded(uploadSelected));
        $("upload-sample").addEventListener("click", () => guarded(() =>
            upload(new Blob([SAMPLE_CSV], { type: "text/csv" }), new Blob([SAMPLE_MANIFEST], { type: "application/json" }))));
        $("run").addEventListener("click", () => guarded(runAnalysis));
        $("download-json").addEventListener("click", () => guarded(() => download("json")));
        $("download-csv").addEventListener("click", () => guarded(() => download("csv")));
        guarded(() => refreshProjects());
    });
})();
