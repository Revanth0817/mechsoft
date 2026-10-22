// Vanilla ES-module port of the original React app's ThreeCanvas / CadViewer.
// Supports client-side STL (binary + ASCII) and OBJ parsing, procedural
// placeholder geometry per part category, drag-to-rotate, auto-rotate,
// wireframe toggle, zoom and iso/front/top/right camera presets.

import * as THREE from "https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js";

/* ------------------------------------------------------------------ */
/* Mesh parsers                                                        */
/* ------------------------------------------------------------------ */

export function parseSTL(arrayBuffer) {
    const view = new DataView(arrayBuffer);
    const isAscii = (() => {
        if (arrayBuffer.byteLength < 84) return true;
        const declaredTriCount = view.getUint32(80, true);
        const expectedLen = 84 + declaredTriCount * 50;
        if (expectedLen === arrayBuffer.byteLength) return false;
        const head = new TextDecoder().decode(new Uint8Array(arrayBuffer, 0, Math.min(5, arrayBuffer.byteLength)));
        return head.trim().toLowerCase().startsWith("solid");
    })();

    return isAscii ? parseSTLAscii(arrayBuffer) : parseSTLBinary(arrayBuffer, view);
}

function parseSTLBinary(arrayBuffer, view) {
    const triCount = view.getUint32(80, true);
    const positions = new Float32Array(triCount * 9);
    const normals = new Float32Array(triCount * 9);
    let offset = 84;
    for (let i = 0; i < triCount; i++) {
        const nx = view.getFloat32(offset, true);
        const ny = view.getFloat32(offset + 4, true);
        const nz = view.getFloat32(offset + 8, true);
        offset += 12;
        for (let v = 0; v < 3; v++) {
            const base = i * 9 + v * 3;
            positions[base] = view.getFloat32(offset, true);
            positions[base + 1] = view.getFloat32(offset + 4, true);
            positions[base + 2] = view.getFloat32(offset + 8, true);
            normals[base] = nx; normals[base + 1] = ny; normals[base + 2] = nz;
            offset += 12;
        }
        offset += 2; // attribute byte count
    }
    return buildGeometry(positions, normals);
}

function parseSTLAscii(arrayBuffer) {
    const text = new TextDecoder().decode(arrayBuffer);
    const positions = [];
    const vertexRe = /vertex\s+([\-0-9.eE]+)\s+([\-0-9.eE]+)\s+([\-0-9.eE]+)/g;
    let m;
    while ((m = vertexRe.exec(text)) !== null) {
        positions.push(parseFloat(m[1]), parseFloat(m[2]), parseFloat(m[3]));
    }
    return buildGeometry(new Float32Array(positions), null);
}

function buildGeometry(positions, normals) {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute("position", new THREE.BufferAttribute(positions, 3));
    if (normals) {
        geometry.setAttribute("normal", new THREE.BufferAttribute(normals, 3));
    } else {
        geometry.computeVertexNormals();
    }
    geometry.computeBoundingBox();
    return geometry;
}

const OCCT_BASE = "https://cdn.jsdelivr.net/npm/occt-import-js@0.0.23/dist/";
let occtPromise = null;

function loadOcct() {
    if (occtPromise) return occtPromise;
    occtPromise = new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = OCCT_BASE + "occt-import-js.js";
        script.onload = () => window.occtimportjs({ locateFile: (f) => OCCT_BASE + f }).then(resolve, reject);
        script.onerror = () => reject(new Error("Could not load the STEP/IGES reader"));
        document.head.appendChild(script);
    });
    occtPromise.catch(() => { occtPromise = null; });
    return occtPromise;
}

// STEP / IGES via OpenCascade (WebAssembly): tessellates B-rep solids into one mesh.
export async function parseCadKernel(arrayBuffer, ext) {
    const occt = await loadOcct();
    const bytes = new Uint8Array(arrayBuffer);
    const result = (ext === "iges" || ext === "igs") ? occt.ReadIgesFile(bytes, null)
        : (ext === "brep" || ext === "brp") ? occt.ReadBrepFile(bytes, null)
        : occt.ReadStepFile(bytes, null);
    if (!result.success || !result.meshes.length) throw new Error("No geometry found in file");

    const positions = [];
    for (const m of result.meshes) {
        const pos = m.attributes.position.array;
        const idx = m.index.array;
        for (let i = 0; i < idx.length; i++) {
            const v = idx[i] * 3;
            positions.push(pos[v], pos[v + 1], pos[v + 2]);
        }
    }
    return buildGeometry(new Float32Array(positions), null);
}

const JSM = "https://cdn.jsdelivr.net/npm/three@0.160.0/examples/jsm/loaders/";

// Flattens every mesh of a loaded scene graph into one world-space geometry.
function objectToGeometry(root) {
    root.updateMatrixWorld(true);
    const positions = [];
    root.traverse((node) => {
        if (!node.isMesh || !node.geometry?.attributes?.position) return;
        let g = node.geometry.clone();
        if (g.index) g = g.toNonIndexed();
        g.applyMatrix4(node.matrixWorld);
        positions.push(...g.attributes.position.array);
    });
    if (!positions.length) throw new Error("No geometry found in file");
    return buildGeometry(new Float32Array(positions), null);
}

// Other open mesh/scene formats via the three.js loaders (loaded on demand).
export async function parseSceneFormat(arrayBuffer, ext) {
    switch (ext) {
        case "ply": {
            const { PLYLoader } = await import(JSM + "PLYLoader.js");
            const g = new PLYLoader().parse(arrayBuffer);
            g.computeBoundingBox();
            if (!g.attributes.normal) g.computeVertexNormals();
            return g.index ? g.toNonIndexed() : g;
        }
        case "glb":
        case "gltf": {
            const { GLTFLoader } = await import(JSM + "GLTFLoader.js");
            const gltf = await new Promise((res, rej) => new GLTFLoader().parse(arrayBuffer, "", res, rej));
            return objectToGeometry(gltf.scene);
        }
        case "fbx": {
            const { FBXLoader } = await import(JSM + "FBXLoader.js");
            return objectToGeometry(new FBXLoader().parse(arrayBuffer, ""));
        }
        case "dae": {
            const { ColladaLoader } = await import(JSM + "ColladaLoader.js");
            return objectToGeometry(new ColladaLoader().parse(new TextDecoder().decode(arrayBuffer), "").scene);
        }
        case "3ds": {
            const { TDSLoader } = await import(JSM + "TDSLoader.js");
            return objectToGeometry(new TDSLoader().parse(arrayBuffer, ""));
        }
        case "3mf": {
            const { ThreeMFLoader } = await import(JSM + "3MFLoader.js");
            return objectToGeometry(new ThreeMFLoader().parse(arrayBuffer));
        }
        default:
            throw new Error("Unsupported format");
    }
}

export function parseOBJ(text) {
    const verts = [];
    const positions = [];
    const lines = text.split("\n");
    for (const raw of lines) {
        const line = raw.trim();
        if (line.startsWith("v ")) {
            const parts = line.split(/\s+/);
            verts.push([parseFloat(parts[1]), parseFloat(parts[2]), parseFloat(parts[3])]);
        } else if (line.startsWith("f ")) {
            const parts = line.split(/\s+/).slice(1).map(p => parseInt(p.split("/")[0], 10) - 1);
            // Fan triangulation for polygons with more than 3 vertices.
            for (let i = 1; i < parts.length - 1; i++) {
                const a = verts[parts[0]], b = verts[parts[i]], c = verts[parts[i + 1]];
                if (a && b && c) positions.push(...a, ...b, ...c);
            }
        }
    }
    return buildGeometry(new Float32Array(positions), null);
}

/* ------------------------------------------------------------------ */
/* Procedural placeholder geometry per category                       */
/* ------------------------------------------------------------------ */

export function placeholderGeometry(category) {
    switch (category) {
        case "Hydraulics":
            return new THREE.CylinderGeometry(0.6, 0.6, 2.2, 32);
        case "Actuation":
            return new THREE.CylinderGeometry(0.25, 0.25, 2.6, 24);
        case "Motion":
            return new THREE.TorusGeometry(0.9, 0.32, 20, 48);
        case "Transmission":
            return new THREE.CylinderGeometry(1, 1, 1.4, 6);
        case "Drive":
            return new THREE.CylinderGeometry(0.7, 0.7, 1.8, 32);
        case "Sensors":
            return new THREE.SphereGeometry(0.9, 32, 24);
        case "Valvetrain":
        default:
            return new THREE.BoxGeometry(1.6, 1, 1.6);
    }
}

/* ------------------------------------------------------------------ */
/* Viewer controller                                                   */
/* ------------------------------------------------------------------ */

export class CadViewer {
    constructor(canvasHost) {
        this.host = canvasHost;
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0xf1f5f9);

        this.camera = new THREE.PerspectiveCamera(45, 1, 0.1, 1000);
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setPixelRatio(window.devicePixelRatio || 1);
        canvasHost.appendChild(this.renderer.domElement);

        this._addLights();

        this.mesh = null;
        this.autoRotate = false;
        this.wireframe = false;

        this._resize();
        window.addEventListener("resize", () => this._resize());

        this._bindDrag();
        // Mouse wheel / trackpad scroll zooms toward the model.
        this.host.addEventListener("wheel", (e) => {
            e.preventDefault();
            this.zoom(e.deltaY < 0 ? 0.9 : 1.1);
        }, { passive: false });
        this.setCameraPreset("iso");
        this._animate();
    }

    _addLights() {
        const ambient = new THREE.AmbientLight(0xffffff, 0.6);
        const d1 = new THREE.DirectionalLight(0xffffff, 0.8);
        d1.position.set(5, 8, 5);
        const d2 = new THREE.DirectionalLight(0xffffff, 0.4);
        d2.position.set(-5, -3, -5);
        this.scene.add(ambient, d1, d2);
    }

    _resize() {
        const w = this.host.clientWidth || 400;
        const h = this.host.clientHeight || 400;
        this.renderer.setSize(w, h);
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
    }

    _bindDrag() {
        let dragging = false, lastX = 0, lastY = 0;
        this.rotation = { x: -0.4, y: 0.6 };
        this.host.addEventListener("pointerdown", (e) => { dragging = true; lastX = e.clientX; lastY = e.clientY; });
        window.addEventListener("pointerup", () => dragging = false);
        window.addEventListener("pointermove", (e) => {
            if (!dragging || !this.mesh) return;
            const dx = e.clientX - lastX, dy = e.clientY - lastY;
            lastX = e.clientX; lastY = e.clientY;
            this.rotation.y += dx * 0.008;
            this.rotation.x += dy * 0.008;
            this.mesh.rotation.set(this.rotation.x, this.rotation.y, 0);
        });
    }

    setGeometry(geometry, opts) {
        if (this.mesh) {
            this.scene.remove(this.mesh);
            this.mesh.geometry.dispose();
            this.mesh.material.dispose();
        }
        geometry.computeBoundingBox();
        geometry.center();

        const material = new THREE.MeshStandardMaterial({
            color: 0x0284c7,
            metalness: 0.35,
            roughness: 0.55,
            wireframe: this.wireframe,
        });
        this.mesh = new THREE.Mesh(geometry, material);

        // Scale to fit a ~2.5 unit viewing box.
        const box = geometry.boundingBox;
        const size = new THREE.Vector3();
        box.getSize(size);
        const maxDim = Math.max(size.x, size.y, size.z, 1e-6);
        const scale = 2.5 / maxDim;
        this.mesh.scale.setScalar(scale);
        this.mesh.rotation.set(this.rotation.x, this.rotation.y, 0);

        this.scene.add(this.mesh);

        this.properties = {
            format: opts?.format ?? "Procedural",
            material: opts?.material ?? "-",
            vertices: geometry.attributes.position.count,
            triangles: Math.floor(geometry.attributes.position.count / 3),
            bounds: `${size.x.toFixed(2)} x ${size.y.toFixed(2)} x ${size.z.toFixed(2)}`,
        };
    }

    setWireframe(on) {
        this.wireframe = on;
        if (this.mesh) this.mesh.material.wireframe = on;
    }

    setAutoRotate(on) {
        this.autoRotate = on;
    }

    // factor < 1 moves the camera closer (zoom in), > 1 moves it away (zoom out).
    zoom(factor) {
        const min = 2, max = 25;
        const dist = this.camera.position.length();
        const next = Math.min(max, Math.max(min, dist * factor));
        this.camera.position.multiplyScalar(next / dist);
        this.camera.lookAt(0, 0, 0);
    }

    setCameraPreset(preset) {
        const d = 4.2;
        const positions = {
            iso: [d, d, d],
            front: [0, 0, d],
            top: [0, d, 0.01],
            right: [d, 0, 0],
        };
        const [x, y, z] = positions[preset] || positions.iso;
        this.camera.position.set(x, y, z);
        this.camera.lookAt(0, 0, 0);
    }

    _animate() {
        requestAnimationFrame(() => this._animate());
        if (this.autoRotate && this.mesh) {
            this.mesh.rotation.y += 0.006;
        }
        this.renderer.render(this.scene, this.camera);
    }

    dispose() {
        this.renderer.dispose();
    }
}
