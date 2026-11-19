"use strict";

// Display-only client: every metric shown comes from the server report; nothing here is authoritative.
(() => {
    const SVG_NAMESPACE = "http://www.w3.org/2000/svg";
    const PLOT_WIDTH = 640;
    const PLOT_HEIGHT = 420;
    const PLOT_MARGIN = 30;
    const POINT_RADIUS = 4;
    const LABEL_OFFSET = 5;
    const START_MARKER_SIZE = 10;
    const END_MARKER_RADIUS = 7;
    const DISPLAY_DECIMALS = 3;

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

    const state = { inputId: null, run: null, report: null };

    function getElement(id) {
        return document.getElementById(id);
    }

    function buildIdentityHeaders(extraHeaders) {
        return Object.assign({
            "X-Dev-Tenant": getElement("tenant").value.trim(),
            "X-Dev-User": getElement("user").value.trim(),
            "X-Dev-Role": getElement("role").value
        }, extraHeaders || {});
    }

    async function sendRequest(method, path, body, extraHeaders) {
        const init = { method, headers: buildIdentityHeaders(extraHeaders) };
        if (body instanceof FormData) {
            init.body = body;
        } else if (body !== undefined) {
            init.headers["Content-Type"] = "application/json";
            init.body = JSON.stringify(body);
        }

        const response = await fetch(path, init);
        if (!response.ok) {
            let problem = { title: response.statusText, detail: "", code: String(response.status) };
            try { problem = await response.json(); } catch { /* non-JSON error body */ }
            throw problem;
        }

        return response;
    }

    function showError(problem) {
        const errorBox = getElement("error");
        errorBox.hidden = false;
        errorBox.textContent = `${problem.code || problem.status}: ${problem.detail || problem.title}`;
    }

    function clearError() {
        const errorBox = getElement("error");
        errorBox.hidden = true;
        errorBox.textContent = "";
    }

    async function runWithErrorDisplay(action) {
        clearError();
        try {
            await action();
        } catch (problem) {
            showError(problem);
        }
    }

    async function refreshProjects(projectIdToSelect) {
        const projects = await (await sendRequest("GET", "/api/v1/projects")).json();
        const select = getElement("project");
        select.replaceChildren(...projects.map((project) => {
            const option = document.createElement("option");
            option.value = project.id;
            option.textContent = project.name;
            return option;
        }));
        if (projectIdToSelect) {
            select.value = projectIdToSelect;
        }
    }

    async function createProject() {
        const project = await (await sendRequest("POST", "/api/v1/projects", { name: getElement("project-name").value })).json();
        await refreshProjects(project.id);
    }

    async function uploadInput(pointsBlob, manifestBlob) {
        const projectId = getElement("project").value;
        if (!projectId) {
            throw { code: "NO_PROJECT", detail: "Create or select a project first." };
        }

        const form = new FormData();
        form.append("points", pointsBlob, "points.csv");
        form.append("manifest", manifestBlob, "job.json");
        const input = await (await sendRequest("POST", `/api/v1/projects/${projectId}/inputs`, form)).json();
        state.inputId = input.id;
        getElement("input-status").textContent = `Input accepted: ${input.point_count} points, sha256 ${input.input_sha256.slice(0, 16)}…`;
        getElement("run").disabled = false;
    }

    async function uploadSelectedFiles() {
        const pointsFile = getElement("points-file").files[0];
        const manifestFile = getElement("manifest-file").files[0];
        if (!pointsFile || !manifestFile) {
            throw { code: "NO_FILES", detail: "Choose both a points CSV and a manifest JSON." };
        }

        await uploadInput(pointsFile, manifestFile);
    }

    function uploadSample() {
        return uploadInput(new Blob([SAMPLE_CSV], { type: "text/csv" }), new Blob([SAMPLE_MANIFEST], { type: "application/json" }));
    }

    async function runAnalysis() {
        const projectId = getElement("project").value;
        getElement("run-status").textContent = "Running…";
        const run = await (await sendRequest("POST", `/api/v1/projects/${projectId}/analyses`, { input_id: state.inputId },
            { "Idempotency-Key": crypto.randomUUID() })).json();
        state.run = run;
        state.report = await (await sendRequest("GET", run.report_url)).json();
        getElement("run-status").textContent = `Run ${run.id} ${run.status}.`;
        renderReport(state.report);
    }

    function appendCell(row, text, tagName) {
        const cell = document.createElement(tagName);
        cell.textContent = text;
        row.appendChild(cell);
    }

    function appendHeaderCell(row, text) {
        appendCell(row, text, "th");
    }

    function appendDataCell(row, text) {
        appendCell(row, text, "td");
    }

    function formatMillimetres(value) {
        return value === null || value === undefined ? "—" : Number(value).toFixed(DISPLAY_DECIMALS);
    }

    function selectedOrder(report) {
        return report.selected_route === "candidate" ? report.candidate_order : report.baseline_order;
    }

    function renderReport(report) {
        getElement("results").hidden = false;
        const pointsById = new Map(report.canonical_job.points.map((point) => [point.point_id, point]));
        renderSummaryTable(report);
        renderOrderTable(selectedOrder(report), pointsById);
        drawRoutePlot(report, pointsById);
    }

    function renderSummaryTable(report) {
        const body = getElement("summary").tBodies[0];
        body.replaceChildren();
        const rows = [
            ["Selected route", `${report.selected_route} (${report.selection_reason})`],
            ["Baseline distance (mm)", formatMillimetres(report.baseline_distance_mm)],
            ["Candidate distance (mm)", formatMillimetres(report.candidate_distance_mm)],
            ["Delta (mm)", formatMillimetres(report.delta_distance_mm)],
            ["Delta (%)", formatMillimetres(report.delta_percent)],
            ["Baseline feasible", String(report.constraint_check.baseline.feasible)],
            ["Candidate feasible", String(report.constraint_check.candidate.feasible)],
            ["Search", `${report.search.termination}, ${report.search.passes} passes, ${report.search.accepted_moves} accepted moves`],
            ["Warnings", report.warnings.map((warning) => warning.code).join(", ") || "none"],
            ["Algorithm", `${report.algorithm_name} ${report.algorithm_version}`],
            ["Input SHA-256", report.input_sha256]
        ];
        for (const [label, value] of rows) {
            const row = body.insertRow();
            appendHeaderCell(row, label);
            appendDataCell(row, value);
        }
    }

    function renderOrderTable(orderedIds, pointsById) {
        const body = getElement("order").tBodies[0];
        body.replaceChildren();
        orderedIds.forEach((pointId, index) => {
            const point = pointsById.get(pointId);
            const row = body.insertRow();
            [String(index + 1), pointId, formatMillimetres(point.x_mm), formatMillimetres(point.y_mm), point.operation_group || "—"]
                .forEach((text) => appendDataCell(row, text));
        });
    }

    function createPlotProjection(positions) {
        const xs = positions.map((position) => position.x_mm);
        const ys = positions.map((position) => position.y_mm);
        const minX = Math.min(...xs), maxX = Math.max(...xs), minY = Math.min(...ys), maxY = Math.max(...ys);
        const scale = Math.min(
            (PLOT_WIDTH - 2 * PLOT_MARGIN) / Math.max(maxX - minX, Number.EPSILON),
            (PLOT_HEIGHT - 2 * PLOT_MARGIN) / Math.max(maxY - minY, Number.EPSILON));
        return {
            toScreenX: (xMm) => PLOT_MARGIN + (xMm - minX) * scale,
            toScreenY: (yMm) => PLOT_HEIGHT - PLOT_MARGIN - (yMm - minY) * scale
        };
    }

    function appendSvgElement(svg, name, attributes) {
        const svgElement = document.createElementNS(SVG_NAMESPACE, name);
        for (const [attributeName, attributeValue] of Object.entries(attributes)) {
            svgElement.setAttribute(attributeName, String(attributeValue));
        }

        svg.appendChild(svgElement);
        return svgElement;
    }

    function drawRoutePlot(report, pointsById) {
        const svg = getElement("plot");
        svg.replaceChildren(svg.querySelector("title"));
        const job = report.canonical_job;
        const start = { x_mm: job.start_x_mm, y_mm: job.start_y_mm };
        const end = job.end_x_mm === null ? null : { x_mm: job.end_x_mm, y_mm: job.end_y_mm };
        const projection = createPlotProjection([...pointsById.values(), start, ...(end ? [end] : [])]);

        drawRoute(svg, projection, [start, ...report.baseline_order.map((id) => pointsById.get(id)), ...(end ? [end] : [])],
            { stroke: "#9aa0a6", "stroke-width": 1, "stroke-dasharray": "4 4" });
        drawRoute(svg, projection, [start, ...selectedOrder(report).map((id) => pointsById.get(id)), ...(end ? [end] : [])],
            { stroke: "#464feb", "stroke-width": 2 });
        drawPoints(svg, projection, pointsById);
        drawStartMarker(svg, projection, start);
        if (end) {
            drawEndMarker(svg, projection, end);
        }
    }

    function drawRoute(svg, projection, positions, strokeAttributes) {
        const coordinates = positions.map((position) => `${projection.toScreenX(position.x_mm)},${projection.toScreenY(position.y_mm)}`).join(" ");
        appendSvgElement(svg, "polyline", Object.assign({ points: coordinates, fill: "none" }, strokeAttributes));
    }

    function drawPoints(svg, projection, pointsById) {
        for (const point of pointsById.values()) {
            const x = projection.toScreenX(point.x_mm);
            const y = projection.toScreenY(point.y_mm);
            appendSvgElement(svg, "circle", { cx: x, cy: y, r: POINT_RADIUS, fill: point.operation_group ? "#c27c0e" : "#1f2328" });
            const label = appendSvgElement(svg, "text", { x: x + LABEL_OFFSET, y: y - LABEL_OFFSET, "font-size": 10, fill: "#57606a" });
            label.textContent = point.point_id;
        }
    }

    function drawStartMarker(svg, projection, start) {
        const half = START_MARKER_SIZE / 2;
        appendSvgElement(svg, "rect", {
            x: projection.toScreenX(start.x_mm) - half,
            y: projection.toScreenY(start.y_mm) - half,
            width: START_MARKER_SIZE,
            height: START_MARKER_SIZE,
            fill: "#1a7f37"
        });
    }

    function drawEndMarker(svg, projection, end) {
        const x = projection.toScreenX(end.x_mm);
        const y = projection.toScreenY(end.y_mm);
        const radius = END_MARKER_RADIUS;
        appendSvgElement(svg, "polygon", {
            points: `${x},${y - radius} ${x + radius},${y} ${x},${y + radius} ${x - radius},${y}`,
            fill: "none",
            stroke: "#cf222e",
            "stroke-width": 2
        });
    }

    async function downloadReport(format) {
        const response = await sendRequest("GET", `${state.run.report_url}?format=${format}`);
        const url = URL.createObjectURL(await response.blob());
        const link = document.createElement("a");
        link.href = url;
        link.download = `run-${state.run.id}.${format}`;
        link.click();
        URL.revokeObjectURL(url);
    }

    function bindButton(id, action) {
        getElement(id).addEventListener("click", () => runWithErrorDisplay(action));
    }

    document.addEventListener("DOMContentLoaded", () => {
        bindButton("create-project", createProject);
        bindButton("refresh-projects", () => refreshProjects());
        bindButton("upload", uploadSelectedFiles);
        bindButton("upload-sample", uploadSample);
        bindButton("run", runAnalysis);
        bindButton("download-json", () => downloadReport("json"));
        bindButton("download-csv", () => downloadReport("csv"));
        runWithErrorDisplay(() => refreshProjects());
    });
})();
