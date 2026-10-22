// Wires up one _ViewerPanel partial instance: the 3D model viewer,
// the Three.js canvas (via cad-viewer.js), and the client-side PDF blob viewer.
// No server round-trip is used for either format, matching the original app.

import { CadViewer, parseSTL, parseOBJ, parseCadKernel, parseSceneFormat, placeholderGeometry } from "/js/cad-viewer.js?v=7";

const LIVE_RENDER_EXTENSIONS = new Set(["stl", "obj", "step", "stp", "iges", "igs", "brep", "brp", "ply", "glb", "gltf", "fbx", "dae", "3ds", "3mf"]);
const METADATA_ONLY_EXTENSIONS = new Set(["step", "stp", "iges", "igs", "sldprt", "sldasm", "dwg", "dxf"]);

function extOf(filename) {
    const idx = filename.lastIndexOf(".");
    return idx === -1 ? "" : filename.slice(idx + 1).toLowerCase();
}

export function initViewerPanel(root) {
    if (!root || root.dataset.viewerInitialized) return;
    root.dataset.viewerInitialized = "true";

    const category = root.dataset.category || "Valvetrain";
    const material = root.dataset.material || "-";

    // Kept as a function so callers can still make sure the viewer exists before loading a model.
    function activateTab() {
        ensureViewer();
    }

    /* ---------------- Full-screen popup ---------------- */

    const titleNode = root.querySelector("[data-cad-title]");

    function openOverlay(title) {
        if (titleNode) titleNode.textContent = title || "";
        root.classList.remove("hidden");
        root.classList.add("flex");
        document.body.style.overflow = "hidden";
        // Size the canvas once the popup is visible.
        requestAnimationFrame(() => { viewer?._resize(); apsViewer?.resize(); });
    }

    function closeOverlay() {
        root.classList.add("hidden");
        root.classList.remove("flex");
        document.body.style.overflow = "";
        hideAps();
    }

    root.querySelector("[data-zoom-in]")?.addEventListener("click", () => viewer?.zoom(0.8));
    root.querySelector("[data-zoom-out]")?.addEventListener("click", () => viewer?.zoom(1.25));

    root.querySelector("[data-viewer-close]")?.addEventListener("click", closeOverlay);
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && !root.classList.contains("hidden")) closeOverlay();
    });

    /* ---------------- 3D Model tab ---------------- */

    const canvasHost = root.querySelector("[data-cad-canvas]");
    const unsupportedPanel = root.querySelector("[data-cad-unsupported]");
    const propsList = root.querySelector("[data-cad-props]");
    const fileNameNode = root.querySelector("[data-cad-filename]");
    const wireframeToggle = root.querySelector("[data-cad-wireframe]");
    const autoRotateToggle = root.querySelector("[data-cad-autorotate]");

    let viewer = null;

    function ensureViewer() {
        if (viewer || !canvasHost) return;
        viewer = new CadViewer(canvasHost);
        loadPlaceholder();
    }

    function loadPlaceholder() {
        if (!viewer) return;
        viewer.setGeometry(placeholderGeometry(category), { format: "Procedural preview", material });
        renderProps();
        if (fileNameNode) fileNameNode.textContent = "(placeholder geometry — no file loaded)";
        unsupportedPanel?.classList.add("hidden");
        canvasHost.classList.remove("hidden");
    }

    function renderProps() {
        if (!propsList || !viewer?.properties) return;
        const p = viewer.properties;
        propsList.innerHTML = `
            <div class="flex justify-between py-1"><span class="text-slate-500">Format</span><span class="font-medium">${p.format}</span></div>
            <div class="flex justify-between py-1"><span class="text-slate-500">Material</span><span class="font-medium">${p.material}</span></div>
            <div class="flex justify-between py-1"><span class="text-slate-500">Vertices</span><span class="font-medium">${p.vertices}</span></div>
            <div class="flex justify-between py-1"><span class="text-slate-500">Triangles</span><span class="font-medium">${p.triangles}</span></div>
            <div class="flex justify-between py-1"><span class="text-slate-500">Bounding box</span><span class="font-medium">${p.bounds}</span></div>
        `;
    }

    root.querySelectorAll("[data-cad-preset]").forEach(btn => {
        btn.addEventListener("click", () => viewer?.setCameraPreset(btn.dataset.cadPreset));
    });

    wireframeToggle?.addEventListener("change", () => viewer?.setWireframe(wireframeToggle.checked));
    autoRotateToggle?.addEventListener("change", () => viewer?.setAutoRotate(autoRotateToggle.checked));

    const apsHost = root.querySelector("[data-aps-host]");
    let apsViewer = null;
    let apsScripts = null;
    let apsPoll = null;

    function hideAps() {
        apsHost?.classList.add("hidden");
        if (apsPoll) { clearTimeout(apsPoll); apsPoll = null; }
    }

    function loadApsScripts() {
        if (apsScripts) return apsScripts;
        apsScripts = new Promise((resolve, reject) => {
            const css = document.createElement("link");
            css.rel = "stylesheet";
            css.href = "https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/style.min.css";
            document.head.appendChild(css);
            const script = document.createElement("script");
            script.src = "https://developer.api.autodesk.com/modelderivative/v2/viewers/7.*/viewer3D.min.js";
            script.onload = resolve;
            script.onerror = () => { apsScripts = null; reject(new Error("Could not load the Autodesk viewer (needs internet access).")); };
            document.head.appendChild(script);
        });
        return apsScripts;
    }

    async function openAps(urn, name, size, ext) {
        try {
            await loadApsScripts();
            const A = window.Autodesk.Viewing;
            if (!apsViewer) {
                await new Promise(resolve => A.Initializer({
                    env: "AutodeskProduction2",
                    api: "streamingV2",
                    getAccessToken: async (cb) => {
                        const r = await fetch("/Viewer?handler=ApsToken");
                        const t = await r.json();
                        cb(t.accessToken, t.expiresIn);
                    },
                }, resolve));
                apsViewer = new A.GuiViewer3D(apsHost);
                apsViewer.start();
            }
            canvasHost.classList.add("hidden");
            unsupportedPanel?.classList.add("hidden");
            apsHost.classList.remove("hidden");
            if (fileNameNode) fileNameNode.textContent = name;
            apsViewer.resize();
            A.Document.load("urn:" + urn, (doc) => {
                apsViewer.loadDocumentNode(doc, doc.getRoot().getDefaultGeometry());
            }, (code, msg) => showUnsupported({ name, size }, ext, "Autodesk viewer error: " + msg));
        } catch (err) {
            showUnsupported({ name, size }, ext, err.message);
        }
    }

    // Poll a translation until it finishes, then open it.
    function waitForAps(btn) {
        const name = btn.dataset.cadName, ext = btn.dataset.cadExt, size = Number(btn.dataset.cadSize);
        showUnsupported({ name, size }, ext, "Autodesk is translating this file for preview... this can take a minute.");
        const tick = async () => {
            try {
                const res = await fetch(btn.dataset.apsStatusUrl);
                const j = await res.json();
                if (j.status === "success") { btn.dataset.apsStatus = "success"; openAps(btn.dataset.apsUrn, name, size, ext); return; }
                if (j.status === "failed" || j.status === "error") {
                    showUnsupported({ name, size }, ext, j.status === "failed" ? "Autodesk could not translate this file. Try exporting it as STEP or STL." : j.message);
                    return;
                }
            } catch (e) { /* keep polling */ }
            apsPoll = setTimeout(tick, 4000);
        };
        tick();
    }

    // Entry point for a click on a saved file.
    function openEntry(btn) {
        hideAps();
        if (btn.dataset.cadExt !== "pdf") openOverlay(btn.dataset.cadName);
        const ext = btn.dataset.cadExt;
        // PDFs have no in-page viewer; open them in the browser's own PDF viewer in a new tab.
        if (ext === "pdf") { window.open(btn.dataset.cadOpen + "&inline=true", "_blank"); return; }
        if (LIVE_RENDER_EXTENSIONS.has(ext)) return openSaved(btn.dataset.cadOpen, btn.dataset.cadName);
        const name = btn.dataset.cadName, size = Number(btn.dataset.cadSize);
        if (btn.dataset.apsUrn && btn.dataset.apsStatus === "success") return openAps(btn.dataset.apsUrn, name, size, ext);
        if (btn.dataset.apsUrn && btn.dataset.apsStatus === "pending") return waitForAps(btn);
        showUnsupported({ name, size }, ext);
    }

    function showUnsupported(file, ext, message) {
        hideAps();
        canvasHost.classList.add("hidden");
        if (fileNameNode) fileNameNode.textContent = file.name;
        if (!unsupportedPanel) return;
        unsupportedPanel.classList.remove("hidden");
        const proprietary = true;
        unsupportedPanel.querySelector("[data-unsupported-msg]").textContent = message
            ?? (proprietary
                ? "." + ext + " is a closed CAD format that browsers cannot read. The file is saved and shared. Use \"Preview via Autodesk\" in the file list (when Autodesk is configured), export it as STEP or STL and upload that too, or open it in its CAD application."
                : "This format cannot be previewed in the browser. Open the saved file in eDrawings desktop.");
        unsupportedPanel.querySelector("[data-unsupported-name]").textContent = file.name;
        unsupportedPanel.querySelector("[data-unsupported-size]").textContent = (file.size / 1024).toFixed(1) + " KB";
        unsupportedPanel.querySelector("[data-unsupported-ext]").textContent = ext.toUpperCase();
    }

    async function loadFile(file) {
        hideAps();
        const ext = extOf(file.name);
        activateTab();
        requestAnimationFrame(() => viewer?._resize());

        if (LIVE_RENDER_EXTENSIONS.has(ext)) {
            const buffer = await file.arrayBuffer();
            try {
                if (fileNameNode) fileNameNode.textContent = "Reading " + file.name + "...";
                const geometry = ext === "stl" ? parseSTL(buffer)
                    : ext === "obj" ? parseOBJ(new TextDecoder().decode(buffer))
                    : ["step", "stp", "iges", "igs", "brep", "brp"].includes(ext) ? await parseCadKernel(buffer, ext)
                    : await parseSceneFormat(buffer, ext);
                viewer.setGeometry(geometry, { format: ext.toUpperCase(), material });
                renderProps();
                if (fileNameNode) fileNameNode.textContent = file.name;
                unsupportedPanel?.classList.add("hidden");
                canvasHost.classList.remove("hidden");
            } catch (err) {
                console.error("Failed to parse mesh", err);
                showUnsupported(file, ext, "Could not read this file: " + err.message);
            }
        } else {
            showUnsupported(file, ext);
        }
    }

    async function openSaved(url, name) {
        const res = await fetch(url);
        if (!res.ok) { console.error("Could not load saved file", res.status); return; }
        await loadFile(new File([await res.blob()], name));
    }

    // Saved (shared) files: click a name in the list to open it here.
    document.querySelectorAll("[data-cad-open]").forEach(btn => {
        btn.addEventListener("click", () => openEntry(btn));
    });

    // Create the viewer, then open the file that was just uploaded, if any (PDFs are not auto-opened).
    const autoBtn = root.dataset.autoloadId && document.querySelector(`[data-file-id="${root.dataset.autoloadId}"]`);
    if (autoBtn && autoBtn.dataset.cadExt !== "pdf") autoBtn.click();
}

export function initAllViewerPanels() {
    document.querySelectorAll("[data-viewer-panel]").forEach(initViewerPanel);
}

document.addEventListener("DOMContentLoaded", initAllViewerPanels);
