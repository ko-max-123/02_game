import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const sourceDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(sourceDir, "..");
const modelDir = path.join(rootDir, "Models");
const materialDir = path.join(rootDir, "Materials");
const previewDir = path.join(rootDir, "Previews");

for (const dir of [modelDir, materialDir, previewDir]) {
  fs.mkdirSync(dir, { recursive: true });
}

const MATERIALS = {
  K1_WhiteGray: { color: "#C8CDD0", kd: [0.65, 0.68, 0.70], ks: [0.18, 0.20, 0.22], ns: 45 },
  K1_LightPanel: { color: "#E8E7E0", kd: [0.82, 0.81, 0.77], ks: [0.12, 0.12, 0.12], ns: 25 },
  K1_DarkSteel: { color: "#333B40", kd: [0.10, 0.12, 0.13], ks: [0.24, 0.26, 0.27], ns: 70 },
  K1_JointRubber: { color: "#1B2024", kd: [0.035, 0.045, 0.05], ks: [0.05, 0.05, 0.05], ns: 8 },
  YTC_Orange: { color: "#F28C28", kd: [0.86, 0.34, 0.055], ks: [0.20, 0.12, 0.04], ns: 35 },
  K1_VisorBlue: { color: "#3AA8C7", kd: [0.035, 0.26, 0.36], ks: [0.50, 0.72, 0.80], ns: 120 },
  LARK_Mint: { color: "#BDECCB", kd: [0.44, 0.78, 0.56], ks: [0.38, 0.60, 0.45], ns: 100 },
  Repair_Dark: { color: "#55585A", kd: [0.16, 0.17, 0.17], ks: [0.08, 0.08, 0.08], ns: 15 },
  Field_Walkable: { color: "#D7D9D5", kd: [0.70, 0.72, 0.70], ks: [0.08, 0.08, 0.08], ns: 12 },
  Field_WhitePanel: { color: "#E5E4DD", kd: [0.79, 0.78, 0.74], ks: [0.10, 0.10, 0.10], ns: 18 },
  Field_EdgeDark: { color: "#252D32", kd: [0.075, 0.095, 0.11], ks: [0.10, 0.11, 0.12], ns: 22 },
  Field_HazardYellow: { color: "#E3B341", kd: [0.78, 0.52, 0.08], ks: [0.08, 0.06, 0.02], ns: 12 },
  Field_DangerRed: { color: "#D94343", kd: [0.72, 0.08, 0.08], ks: [0.10, 0.04, 0.04], ns: 18 },
  Field_InteractiveBlue: { color: "#39A9DB", kd: [0.05, 0.42, 0.64], ks: [0.18, 0.35, 0.45], ns: 45 },
  Field_BackgroundGray: { color: "#657079", kd: [0.22, 0.26, 0.29], ks: [0.04, 0.04, 0.04], ns: 8 },
  Field_BackgroundDark: { color: "#3C464C", kd: [0.12, 0.15, 0.17], ks: [0.03, 0.03, 0.03], ns: 6 },
  Field_WindowWarm: { color: "#F4B45A", kd: [0.78, 0.40, 0.10], ks: [0.20, 0.12, 0.03], ns: 30 },
  Field_Glass: { color: "#6DADB8", kd: [0.12, 0.35, 0.40], ks: [0.40, 0.55, 0.58], ns: 90 },
  Field_Sunset: { color: "#D88762", kd: [0.55, 0.23, 0.13], ks: [0.02, 0.02, 0.02], ns: 2 },
};

const add = (a, b) => a.map((v, i) => v + b[i]);
const sub = (a, b) => a.map((v, i) => v - b[i]);
const mul = (a, s) => a.map((v) => v * s);
const dot = (a, b) => a.reduce((sum, v, i) => sum + v * b[i], 0);
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const len = (a) => Math.sqrt(dot(a, a));
const norm = (a) => {
  const l = len(a);
  return l > 1e-9 ? mul(a, 1 / l) : [0, 1, 0];
};

function rotatePoint(p, rot) {
  let [x, y, z] = p;
  const [rx, ry, rz] = rot;
  let c = Math.cos(rx), s = Math.sin(rx);
  [y, z] = [y * c - z * s, y * s + z * c];
  c = Math.cos(ry); s = Math.sin(ry);
  [x, z] = [x * c + z * s, -x * s + z * c];
  c = Math.cos(rz); s = Math.sin(rz);
  [x, y] = [x * c - y * s, x * s + y * c];
  return [x, y, z];
}

class Mesh {
  constructor(name) {
    this.name = name;
    this.parts = [];
  }
  addPart(name, material, vertices, faces) {
    this.parts.push({ name, material, vertices, faces });
  }
  allVertices() {
    return this.parts.flatMap((p) => p.vertices);
  }
}

function box(mesh, name, center, size, material, rot = [0, 0, 0]) {
  const [hx, hy, hz] = size.map((v) => v / 2);
  const local = [
    [-hx, -hy, -hz], [hx, -hy, -hz], [hx, hy, -hz], [-hx, hy, -hz],
    [-hx, -hy, hz], [hx, -hy, hz], [hx, hy, hz], [-hx, hy, hz],
  ];
  const vertices = local.map((p) => add(rotatePoint(p, rot), center));
  const faces = [
    [0, 2, 1], [0, 3, 2], [4, 5, 6], [4, 6, 7],
    [0, 1, 5], [0, 5, 4], [3, 7, 6], [3, 6, 2],
    [0, 4, 7], [0, 7, 3], [1, 2, 6], [1, 6, 5],
  ];
  mesh.addPart(name, material, vertices, faces);
}

function frustum(mesh, name, center, height, bottomXZ, topXZ, material, rot = [0, 0, 0]) {
  const [bx, bz] = bottomXZ.map((v) => v / 2);
  const [tx, tz] = topXZ.map((v) => v / 2);
  const hy = height / 2;
  const local = [
    [-bx, -hy, -bz], [bx, -hy, -bz], [bx, -hy, bz], [-bx, -hy, bz],
    [-tx, hy, -tz], [tx, hy, -tz], [tx, hy, tz], [-tx, hy, tz],
  ];
  const vertices = local.map((p) => add(rotatePoint(p, rot), center));
  const faces = [
    [0, 2, 1], [0, 3, 2], [4, 5, 6], [4, 6, 7],
    [0, 1, 5], [0, 5, 4], [1, 2, 6], [1, 6, 5],
    [2, 3, 7], [2, 7, 6], [3, 0, 4], [3, 4, 7],
  ];
  mesh.addPart(name, material, vertices, faces);
}

function cylinderBetween(mesh, name, p1, p2, radius1, radius2, sides, material) {
  const axis = norm(sub(p2, p1));
  const ref = Math.abs(axis[1]) < 0.9 ? [0, 1, 0] : [1, 0, 0];
  const u = norm(cross(axis, ref));
  const v = norm(cross(axis, u));
  const vertices = [p1, p2];
  for (let i = 0; i < sides; i += 1) {
    const a = (Math.PI * 2 * i) / sides;
    const radial = add(mul(u, Math.cos(a)), mul(v, Math.sin(a)));
    vertices.push(add(p1, mul(radial, radius1)));
    vertices.push(add(p2, mul(radial, radius2)));
  }
  const faces = [];
  for (let i = 0; i < sides; i += 1) {
    const ni = (i + 1) % sides;
    const b0 = 2 + i * 2;
    const t0 = b0 + 1;
    const b1 = 2 + ni * 2;
    const t1 = b1 + 1;
    faces.push([b0, b1, t1], [b0, t1, t0]);
    faces.push([0, b1, b0], [1, t0, t1]);
  }
  mesh.addPart(name, material, vertices, faces);
}

function ring(mesh, name, center, outerRadius, innerRadius, thickness, sides, material) {
  const y0 = center[1];
  const y1 = center[1] + thickness;
  const vertices = [];
  for (let i = 0; i < sides; i += 1) {
    const a = (Math.PI * 2 * i) / sides;
    const c = Math.cos(a), s = Math.sin(a);
    vertices.push([center[0] + c * outerRadius, y0, center[2] + s * outerRadius]);
    vertices.push([center[0] + c * innerRadius, y0, center[2] + s * innerRadius]);
    vertices.push([center[0] + c * outerRadius, y1, center[2] + s * outerRadius]);
    vertices.push([center[0] + c * innerRadius, y1, center[2] + s * innerRadius]);
  }
  const faces = [];
  for (let i = 0; i < sides; i += 1) {
    const n = (i + 1) % sides;
    const a = i * 4, b = n * 4;
    faces.push([a, a + 2, b + 2], [a, b + 2, b]);
    faces.push([a + 3, a + 1, b + 1], [a + 3, b + 1, b + 3]);
    faces.push([a + 2, a + 3, b + 3], [a + 2, b + 3, b + 2]);
    faces.push([a + 1, a, b], [a + 1, b, b + 1]);
  }
  mesh.addPart(name, material, vertices, faces);
}

function ramp(mesh, name, x0, x1, z0, z1, baseY, topY0, topY1, material) {
  const vertices = [
    [x0, baseY, z0], [x1, baseY, z0], [x1, baseY, z1], [x0, baseY, z1],
    [x0, topY0, z0], [x1, topY1, z0], [x1, topY1, z1], [x0, topY0, z1],
  ];
  const faces = [
    [0, 2, 1], [0, 3, 2], [4, 5, 6], [4, 6, 7],
    [0, 1, 5], [0, 5, 4], [1, 2, 6], [1, 6, 5],
    [2, 3, 7], [2, 7, 6], [3, 0, 4], [3, 4, 7],
  ];
  mesh.addPart(name, material, vertices, faces);
}

function triangularPrism(mesh, name, centerX, baseY, widthX, heightY, z0, z1, material) {
  const x0 = centerX - widthX / 2;
  const x1 = centerX + widthX / 2;
  const xc = centerX;
  const vertices = [
    [x0, baseY, z0], [x1, baseY, z0], [xc, baseY + heightY, z0],
    [x0, baseY, z1], [x1, baseY, z1], [xc, baseY + heightY, z1],
  ];
  const faces = [
    [0, 1, 2], [3, 5, 4], [0, 3, 4], [0, 4, 1],
    [1, 4, 5], [1, 5, 2], [2, 5, 3], [2, 3, 0],
  ];
  mesh.addPart(name, material, vertices, faces);
}

function buildK1() {
  const m = new Mesh("Yamada_K1_Demo");

  for (const side of [-1, 1]) {
    const sx = side * 0.17;
    box(m, `Foot_${side < 0 ? "L" : "R"}`, [sx, 0.075, 0.055], [0.25, 0.15, 0.42], "K1_DarkSteel");
    box(m, `ToeArmor_${side < 0 ? "L" : "R"}`, [sx, 0.115, 0.205], [0.23, 0.07, 0.16], side < 0 ? "YTC_Orange" : "K1_WhiteGray", [0.04, 0, 0]);
    frustum(m, `Shin_${side < 0 ? "L" : "R"}`, [sx, 0.39, 0.0], 0.49, [0.17, 0.18], [0.23, 0.23], "K1_WhiteGray");
    box(m, `ShinFront_${side < 0 ? "L" : "R"}`, [sx, 0.42, 0.125], [0.17, 0.33, 0.055], side < 0 ? "YTC_Orange" : "K1_LightPanel", [0.04, 0, 0]);
    cylinderBetween(m, `Knee_${side < 0 ? "L" : "R"}`, [sx, 0.625, 0], [sx, 0.69, 0], 0.125, 0.125, 8, "K1_JointRubber");
    box(m, `KneeGuard_${side < 0 ? "L" : "R"}`, [sx, 0.67, 0.14], [0.20, 0.18, 0.07], "K1_DarkSteel", [-0.15, 0, 0]);
    frustum(m, `Thigh_${side < 0 ? "L" : "R"}`, [sx, 0.89, 0], 0.42, [0.21, 0.20], [0.25, 0.25], "K1_WhiteGray", [0, 0, side * 0.025]);
    box(m, `ThighPlate_${side < 0 ? "L" : "R"}`, [sx, 0.92, 0.135], [0.17, 0.25, 0.045], side > 0 ? "YTC_Orange" : "K1_LightPanel", [-0.04, 0, 0]);
  }

  box(m, "PelvisCore", [0, 1.095, 0], [0.48, 0.23, 0.28], "K1_DarkSteel");
  frustum(m, "PelvisArmor", [0, 1.135, 0.04], 0.25, [0.48, 0.28], [0.42, 0.24], "K1_WhiteGray");
  box(m, "PelvisOrangeMark", [0.155, 1.17, 0.175], [0.12, 0.07, 0.025], "YTC_Orange", [0, 0, -0.16]);
  cylinderBetween(m, "WaistJoint", [0, 1.20, 0], [0, 1.28, 0], 0.19, 0.17, 10, "K1_JointRubber");

  frustum(m, "TorsoCore", [0, 1.43, 0], 0.42, [0.47, 0.27], [0.67, 0.36], "K1_DarkSteel");
  frustum(m, "ChestArmor", [0, 1.46, 0.105], 0.36, [0.43, 0.18], [0.61, 0.23], "K1_LightPanel");
  box(m, "ChestOrangeStripe", [-0.115, 1.47, 0.235], [0.075, 0.36, 0.022], "YTC_Orange", [0, 0, -0.34]);
  box(m, "ChestRepairScarA", [0.105, 1.52, 0.238], [0.018, 0.20, 0.018], "Repair_Dark", [0, 0, 0.50]);
  box(m, "ChestRepairScarB", [0.155, 1.49, 0.239], [0.014, 0.13, 0.018], "Repair_Dark", [0, 0, 0.50]);
  cylinderBetween(m, "LARKCoreRingOuter", [-0.205, 1.39, 0.238], [-0.205, 1.39, 0.255], 0.055, 0.055, 12, "LARK_Mint");
  cylinderBetween(m, "LARKCoreInset", [-0.205, 1.39, 0.254], [-0.205, 1.39, 0.263], 0.028, 0.028, 12, "K1_DarkSteel");

  cylinderBetween(m, "Neck", [0, 1.62, 0], [0, 1.69, 0], 0.105, 0.09, 10, "K1_JointRubber");
  frustum(m, "Helmet", [0, 1.76, 0.015], 0.26, [0.23, 0.24], [0.20, 0.20], "K1_WhiteGray");
  box(m, "Visor", [0, 1.785, 0.135], [0.19, 0.07, 0.025], "K1_VisorBlue", [0.06, 0, 0]);
  box(m, "HelmetOrangeMark", [-0.095, 1.80, 0.05], [0.035, 0.18, 0.025], "YTC_Orange", [0, 0, -0.18]);
  cylinderBetween(m, "HelmetAntenna", [-0.105, 1.87, 0], [-0.13, 2.00, -0.015], 0.018, 0.010, 8, "K1_DarkSteel");

  const armData = [
    { side: -1, shoulder: [-0.42, 1.52, 0], elbow: [-0.58, 1.22, 0.015], wrist: [-0.61, 0.94, 0.04] },
    { side: 1, shoulder: [0.42, 1.52, 0], elbow: [0.58, 1.22, -0.005], wrist: [0.62, 0.94, 0.04] },
  ];
  for (const arm of armData) {
    const tag = arm.side < 0 ? "L" : "R";
    cylinderBetween(m, `UpperArm_${tag}`, arm.shoulder, arm.elbow, 0.12, 0.10, 8, "K1_WhiteGray");
    cylinderBetween(m, `Elbow_${tag}`, arm.elbow, add(arm.elbow, [0, -0.06, 0]), 0.105, 0.10, 8, "K1_JointRubber");
    cylinderBetween(m, `Forearm_${tag}`, add(arm.elbow, [0, -0.04, 0]), arm.wrist, 0.11, 0.085, 8, arm.side > 0 ? "K1_DarkSteel" : "K1_WhiteGray");
    cylinderBetween(m, `Hand_${tag}`, arm.wrist, add(arm.wrist, [0, -0.14, 0.02]), 0.075, 0.065, 8, "K1_JointRubber");
  }
  box(m, "ShoulderArmor_L", [-0.45, 1.53, 0.015], [0.29, 0.20, 0.34], "YTC_Orange", [0, 0, -0.20]);
  box(m, "ShoulderArmor_R", [0.45, 1.53, 0.005], [0.24, 0.16, 0.28], "K1_LightPanel", [0, 0, 0.16]);
  cylinderBetween(m, "LeftTrialSensor", [-0.50, 1.57, -0.13], [-0.50, 1.78, -0.13], 0.055, 0.045, 8, "K1_DarkSteel");
  box(m, "RightForearmTrialModule", [0.65, 1.08, 0.035], [0.14, 0.27, 0.18], "K1_DarkSteel", [0, 0, -0.10]);
  box(m, "RightForearmOrangeLatch", [0.73, 1.08, 0.04], [0.025, 0.13, 0.08], "YTC_Orange");

  box(m, "BackFrame", [0, 1.45, -0.205], [0.44, 0.40, 0.14], "K1_DarkSteel");
  cylinderBetween(m, "Jet_L", [-0.18, 1.55, -0.29], [-0.18, 1.18, -0.31], 0.105, 0.135, 10, "K1_WhiteGray");
  cylinderBetween(m, "Jet_R_Prototype", [0.20, 1.60, -0.30], [0.22, 1.12, -0.34], 0.13, 0.17, 10, "K1_DarkSteel");
  cylinderBetween(m, "JetGlow_L", [-0.18, 1.19, -0.31], [-0.18, 1.16, -0.31], 0.09, 0.07, 10, "K1_VisorBlue");
  cylinderBetween(m, "JetGlow_R", [0.22, 1.13, -0.34], [0.22, 1.09, -0.34], 0.12, 0.09, 10, "K1_VisorBlue");
  box(m, "BackServiceTag", [0.02, 1.39, -0.285], [0.18, 0.07, 0.018], "YTC_Orange", [0, 0, 0.08]);

  // Small smile-shaped field mark: two square 'eyes' and an angled mouth made from three bars.
  box(m, "SmileEyeA", [-0.235, 0.91, 0.163], [0.018, 0.018, 0.012], "YTC_Orange");
  box(m, "SmileEyeB", [-0.185, 0.91, 0.163], [0.018, 0.018, 0.012], "YTC_Orange");
  box(m, "SmileMouthA", [-0.235, 0.865, 0.164], [0.045, 0.012, 0.012], "YTC_Orange", [0, 0, -0.18]);
  box(m, "SmileMouthB", [-0.195, 0.855, 0.164], [0.045, 0.012, 0.012], "YTC_Orange");
  box(m, "SmileMouthC", [-0.155, 0.865, 0.164], [0.045, 0.012, 0.012], "YTC_Orange", [0, 0, 0.18]);

  return m;
}

function addEdgeNotches(m, x0, x1, y, z, step = 1.0) {
  for (let x = x0 + 0.35; x < x1; x += step) {
    box(m, `WalkableNotch_${x.toFixed(2)}_${z}`, [x, y, z], [0.42, 0.025, 0.13], "Field_EdgeDark");
  }
}

function buildField(includeBackground = true) {
  const m = new Mesh(includeBackground ? "Central_Industrial_Belt_Demo" : "Central_Industrial_Belt_Collision");

  const slabs = [
    { name: "Start", x: -12.5, size: 11 },
    { name: "Training", x: -1.0, size: 12 },
    { name: "JumpExit", x: 10.0, size: 8 },
    { name: "Finish", x: 18.0, size: 8 },
  ];
  for (const s of slabs) {
    box(m, `Floor_${s.name}`, [s.x, -0.22, 0], [s.size, 0.44, 6], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
    if (includeBackground) {
      box(m, `FloorLip_${s.name}_Front`, [s.x, -0.04, -2.91], [s.size, 0.13, 0.18], "Field_EdgeDark");
      box(m, `FloorLip_${s.name}_Back`, [s.x, -0.04, 2.91], [s.size, 0.13, 0.18], "Field_EdgeDark");
      addEdgeNotches(m, s.x - s.size / 2, s.x + s.size / 2, 0.015, -2.80);
    }
  }

  box(m, "JumpBarrier", [-10.0, 0.33, 0], [0.65, 0.66, 5.1], includeBackground ? "Field_WhitePanel" : "Field_EdgeDark");
  if (includeBackground) {
    box(m, "JumpBarrierTop", [-10.0, 0.68, 0], [0.80, 0.08, 5.2], "Field_HazardYellow");
    for (let z = -2.0; z <= 2.0; z += 1.0) {
      triangularPrism(m, `BarrierChevron_${z}`, -10.34, 0.05, 0.10, 0.24, z - 0.30, z + 0.30, "Field_HazardYellow");
    }
  }

  // Three unmistakable step silhouettes, followed by a broad training platform.
  box(m, "Step_A", [-5.3, 0.18, 0], [1.0, 0.36, 4.0], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  box(m, "Step_B", [-4.15, 0.36, 0], [1.0, 0.72, 4.0], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  box(m, "Step_C", [-3.0, 0.54, 0], [1.0, 1.08, 4.0], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  box(m, "RaisedTrainingDeck", [-0.3, 1.02, 0], [4.4, 0.30, 4.0], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  ramp(m, "TrainingRampDown", 1.9, 4.4, -2.0, 2.0, 0, 1.18, 0.10, includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  if (includeBackground) {
    box(m, "DeckEdge", [-0.3, 1.20, -1.92], [4.4, 0.16, 0.16], "Field_EdgeDark");
    box(m, "DeckBlueConsole", [0.65, 1.58, 1.45], [0.62, 0.92, 0.42], "Field_InteractiveBlue");
    frustum(m, "DeckBlueConsoleTop", [0.65, 2.08, 1.45], 0.14, [0.72, 0.52], [0.56, 0.42], "Field_InteractiveBlue");
    box(m, "DeckConsoleScreen", [0.65, 1.85, 1.225], [0.38, 0.25, 0.025], "LARK_Mint", [-0.18, 0, 0]);
  }

  // Jump trench: absence of floor, deep red base and repeated triangular teeth communicate danger by shape.
  box(m, "HazardTrenchBase", [5.5, -1.15, 0], [1.0, 0.16, 6.0], includeBackground ? "Field_DangerRed" : "Field_EdgeDark");
  if (includeBackground) {
    for (let z = -2.6; z <= 2.6; z += 0.65) {
      triangularPrism(m, `HazardTooth_${z.toFixed(2)}`, 5.5, -1.07, 0.70, 0.75, z - 0.20, z + 0.20, "Field_HazardYellow");
    }
    box(m, "TrenchWarningLipA", [4.88, 0.06, 0], [0.18, 0.12, 6.0], "Field_HazardYellow");
    box(m, "TrenchWarningLipB", [6.12, 0.06, 0], [0.18, 0.12, 6.0], "Field_HazardYellow");
  }

  box(m, "LandingBlock", [7.2, 0.38, 0], [1.8, 0.76, 4.4], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  box(m, "HighPlatform", [10.4, 1.05, 0], [4.6, 0.28, 4.4], includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  ramp(m, "HighPlatformRamp", 12.7, 15.0, -2.2, 2.2, 0, 1.18, 0.10, includeBackground ? "Field_Walkable" : "Field_EdgeDark");
  if (includeBackground) {
    box(m, "HighPlatformFrontLip", [10.4, 1.20, -2.12], [4.6, 0.17, 0.17], "Field_EdgeDark");
    ring(m, "FinishPadOuter", [18.0, 0.015, 0], 1.45, 1.05, 0.035, 20, "YTC_Orange");
    ring(m, "SpawnPadOuter", [-15.2, 0.015, 0], 1.15, 0.82, 0.035, 20, "YTC_Orange");
    box(m, "FinishBeacon", [18.0, 1.20, 2.2], [0.28, 2.4, 0.28], "Field_InteractiveBlue");
    ring(m, "FinishBeaconRing", [18.0, 2.35, 2.2], 0.46, 0.30, 0.08, 12, "LARK_Mint");
  }

  if (!includeBackground) return m;

  // Logistics warehouse, deliberately placed behind the playable z-band.
  box(m, "WarehouseMain", [-2.0, 2.35, 5.4], [17.0, 4.7, 3.0], "Field_BackgroundGray");
  box(m, "WarehouseFacade", [-2.0, 2.25, 3.84], [16.5, 4.2, 0.12], "Field_WhitePanel");
  for (let x = -8.5; x <= 4.5; x += 2.6) {
    box(m, `WarehouseRib_${x.toFixed(1)}`, [x, 2.25, 3.73], [0.18, 4.2, 0.14], "Field_EdgeDark");
  }
  for (let x = -7.7; x <= 3.7; x += 3.8) {
    box(m, `WarehouseWindow_${x.toFixed(1)}`, [x, 3.1, 3.66], [2.5, 0.70, 0.08], "Field_Glass");
  }
  box(m, "WarehouseDoor", [2.8, 1.25, 3.65], [3.2, 2.5, 0.10], "Field_EdgeDark");
  box(m, "WarehouseDoorStripe", [2.8, 2.58, 3.58], [3.2, 0.16, 0.11], "YTC_Orange");

  // Cargo silhouettes reinforce the logistics identity without entering the collision lane.
  const cargo = [
    [-12.2, 0.75, 4.2, "Field_BackgroundDark"],
    [-9.4, 0.75, 4.45, "Field_BackgroundGray"],
    [7.2, 0.75, 4.4, "Field_BackgroundDark"],
    [10.0, 0.75, 4.15, "YTC_Orange"],
  ];
  cargo.forEach(([x, y, z, mat], i) => {
    box(m, `CargoContainer_${i}`, [x, y, z], [2.4, 1.5, 1.5], mat);
    for (let rib = -0.9; rib <= 0.9; rib += 0.45) {
      box(m, `CargoRib_${i}_${rib.toFixed(2)}`, [x + rib, y, z - 0.76], [0.06, 1.32, 0.04], "Field_EdgeDark");
    }
  });

  // Elevated road and columns form a strong horizontal background layer.
  box(m, "OverpassDeck", [1.5, 6.25, 8.0], [47.0, 0.70, 3.4], "Field_BackgroundDark");
  box(m, "OverpassGuard", [1.5, 6.82, 6.32], [47.0, 0.45, 0.18], "Field_BackgroundGray");
  for (const x of [-16, -6, 4, 14]) {
    frustum(m, `OverpassColumn_${x}`, [x, 3.0, 8.0], 6.0, [1.3, 1.6], [0.75, 1.15], "Field_BackgroundGray");
  }

  // Distant factory skyline and sunset plate.
  box(m, "SunsetBackdrop", [2.0, 5.0, 12.0], [55.0, 11.0, 0.20], "Field_Sunset");
  box(m, "DistantFactory", [14.5, 2.25, 10.5], [12.0, 4.5, 2.0], "Field_BackgroundDark");
  for (const x of [11.5, 14.5, 17.2]) {
    cylinderBetween(m, `FactoryStack_${x}`, [x, 3.8, 10.2], [x, 8.2 + (x % 2), 10.2], 0.55, 0.42, 12, "Field_BackgroundDark");
    cylinderBetween(m, `FactoryStackBand_${x}`, [x, 6.7, 10.2], [x, 6.95, 10.2], 0.58, 0.58, 12, "YTC_Orange");
  }
  box(m, "DistantTank", [-14.5, 2.0, 10.0], [4.2, 4.0, 3.2], "Field_BackgroundGray");
  cylinderBetween(m, "DistantTankCap", [-14.5, 4.0, 10.0], [-14.5, 4.5, 10.0], 2.1, 1.55, 14, "Field_BackgroundGray");

  // Gantry frame establishes scale and the white test-facility language.
  for (const x of [-15.8, -8.2]) {
    box(m, `GantryLeg_${x}`, [x, 2.75, -3.6], [0.42, 5.5, 0.42], "Field_WhitePanel");
  }
  box(m, "GantryBeam", [-12.0, 5.35, -3.6], [8.0, 0.45, 0.45], "Field_WhitePanel");
  box(m, "GantryOrangeLine", [-12.0, 5.42, -3.82], [6.9, 0.12, 0.03], "YTC_Orange");
  cylinderBetween(m, "GantryLamp", [-12.0, 5.10, -3.6], [-12.0, 4.72, -3.6], 0.08, 0.12, 8, "Field_InteractiveBlue");

  return m;
}

function writeObj(mesh, fileName, mtlRelativePath) {
  const lines = [
    `# ${mesh.name}`,
    "# Original YTC prototype asset. Unity units: 1 OBJ unit = 1 meter. Axis: Y up, Z forward.",
    `mtllib ${mtlRelativePath.replaceAll("\\", "/")}`,
    "s off",
  ];
  let vertexOffset = 0;
  let normalOffset = 0;
  for (const part of mesh.parts) {
    lines.push(`o ${part.name}`, `g ${part.name}`, `usemtl ${part.material}`);
    for (const v of part.vertices) lines.push(`v ${v[0].toFixed(6)} ${v[1].toFixed(6)} ${v[2].toFixed(6)}`);
    const faceNormals = part.faces.map((f) => norm(cross(sub(part.vertices[f[1]], part.vertices[f[0]]), sub(part.vertices[f[2]], part.vertices[f[0]]))));
    for (const n of faceNormals) lines.push(`vn ${n[0].toFixed(6)} ${n[1].toFixed(6)} ${n[2].toFixed(6)}`);
    part.faces.forEach((f, faceIndex) => {
      const ni = normalOffset + faceIndex + 1;
      const idx = f.map((i) => `${vertexOffset + i + 1}//${ni}`).join(" ");
      lines.push(`f ${idx}`);
    });
    vertexOffset += part.vertices.length;
    normalOffset += part.faces.length;
  }
  fs.writeFileSync(path.join(modelDir, fileName), `${lines.join("\n")}\n`, "utf8");
}

function writeMtl() {
  const lines = ["# YTC original prototype materials. No external textures required."];
  for (const [name, mat] of Object.entries(MATERIALS)) {
    lines.push(
      "",
      `newmtl ${name}`,
      `Ka ${(mat.kd[0] * 0.16).toFixed(4)} ${(mat.kd[1] * 0.16).toFixed(4)} ${(mat.kd[2] * 0.16).toFixed(4)}`,
      `Kd ${mat.kd.map((v) => v.toFixed(4)).join(" ")}`,
      `Ks ${mat.ks.map((v) => v.toFixed(4)).join(" ")}`,
      `Ns ${mat.ns}`,
      "d 1.0",
      "illum 2",
    );
  }
  fs.writeFileSync(path.join(materialDir, "ytc_design_assets.mtl"), `${lines.join("\n")}\n`, "utf8");
}

function hexToRgb(hex) {
  const n = Number.parseInt(hex.slice(1), 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

function shadeHex(hex, factor) {
  const [r, g, b] = hexToRgb(hex);
  const adjust = (v) => Math.max(0, Math.min(255, Math.round(v * factor))).toString(16).padStart(2, "0");
  return `#${adjust(r)}${adjust(g)}${adjust(b)}`;
}

function makePreviewSvg(mesh, options) {
  const width = options.width;
  const height = options.height;
  const target = options.target;
  const view = norm(options.cameraVector);
  const right = norm(cross([0, 1, 0], view));
  const up = norm(cross(view, right));
  const light = norm([-0.5, 1.0, 0.8]);
  const triangles = [];
  const projected = [];

  for (const part of mesh.parts) {
    for (const f of part.faces) {
      const p = f.map((i) => part.vertices[i]);
      const normal = norm(cross(sub(p[1], p[0]), sub(p[2], p[0])));
      const center = mul(add(add(p[0], p[1]), p[2]), 1 / 3);
      const proj = p.map((v) => {
        const d = sub(v, target);
        return [dot(d, right), dot(d, up), dot(d, view)];
      });
      projected.push(...proj);
      const facing = dot(normal, view);
      if (facing < -0.12) continue;
      triangles.push({ proj, depth: dot(sub(center, target), view), normal, material: part.material });
    }
  }

  const minX = Math.min(...projected.map((p) => p[0]));
  const maxX = Math.max(...projected.map((p) => p[0]));
  const minY = Math.min(...projected.map((p) => p[1]));
  const maxY = Math.max(...projected.map((p) => p[1]));
  const margin = options.margin ?? 50;
  const scale = Math.min((width - margin * 2) / (maxX - minX), (height - margin * 2) / (maxY - minY));
  const offsetX = width / 2 - ((minX + maxX) / 2) * scale;
  const offsetY = height / 2 + ((minY + maxY) / 2) * scale;
  triangles.sort((a, b) => a.depth - b.depth);

  const polys = triangles.map((tri) => {
    const points = tri.proj.map((p) => `${(offsetX + p[0] * scale).toFixed(1)},${(offsetY - p[1] * scale).toFixed(1)}`).join(" ");
    const base = MATERIALS[tri.material]?.color ?? "#AAAAAA";
    const lighting = 0.72 + Math.max(0, dot(tri.normal, light)) * 0.30;
    return `<polygon points="${points}" fill="${shadeHex(base, lighting)}" stroke="#182026" stroke-width="${options.stroke ?? 0.7}" stroke-linejoin="round"/>`;
  }).join("\n");

  const label = options.label ?? mesh.name;
  const sublabel = options.sublabel ?? "";
  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${options.bgTop ?? "#D68A68"}"/>
      <stop offset="1" stop-color="${options.bgBottom ?? "#18232B"}"/>
    </linearGradient>
    <filter id="shadow"><feGaussianBlur stdDeviation="12"/></filter>
  </defs>
  <rect width="100%" height="100%" fill="url(#bg)"/>
  <ellipse cx="${width * 0.51}" cy="${height * 0.80}" rx="${width * 0.25}" ry="${height * 0.045}" fill="#05080A" opacity="0.42" filter="url(#shadow)"/>
  ${polys}
  <rect x="28" y="28" width="8" height="78" fill="#F28C28"/>
  <text x="54" y="66" fill="#F4F1E8" font-family="Arial, sans-serif" font-size="30" font-weight="700">${label}</text>
  <text x="54" y="96" fill="#C6D0D5" font-family="Arial, sans-serif" font-size="17">${sublabel}</text>
</svg>\n`;
}

function meshStats(mesh) {
  const vertices = mesh.allVertices();
  const mins = [0, 1, 2].map((i) => Math.min(...vertices.map((v) => v[i])));
  const maxs = [0, 1, 2].map((i) => Math.max(...vertices.map((v) => v[i])));
  return {
    objectCount: mesh.parts.length,
    vertices: mesh.parts.reduce((s, p) => s + p.vertices.length, 0),
    triangles: mesh.parts.reduce((s, p) => s + p.faces.length, 0),
    boundsMin: mins,
    boundsMax: maxs,
    size: maxs.map((v, i) => v - mins[i]),
  };
}

const k1 = buildK1();
const field = buildField(true);
const collision = buildField(false);

writeMtl();
writeObj(k1, "yamada_k1_demo.obj", "../Materials/ytc_design_assets.mtl");
writeObj(field, "central_industrial_belt_demo.obj", "../Materials/ytc_design_assets.mtl");
writeObj(collision, "central_industrial_belt_collision.obj", "../Materials/ytc_design_assets.mtl");

fs.writeFileSync(
  path.join(previewDir, "yamada_k1_preview.svg"),
  makePreviewSvg(k1, {
    width: 1200, height: 1200, target: [0, 1.0, 0], cameraVector: [1.4, 0.8, 2.2],
    margin: 120, stroke: 0.85, label: "YAMADA / K1 DEMO", sublabel: "2.00 m equipped height · mid-weight prototype · Y-up / Z-forward",
    bgTop: "#A86A55", bgBottom: "#152029",
  }),
  "utf8",
);

fs.writeFileSync(
  path.join(previewDir, "central_industrial_belt_preview.svg"),
  makePreviewSvg(field, {
    width: 1600, height: 900, target: [1.5, 2.8, 3.8], cameraVector: [1.9, 1.0, -2.4],
    margin: 52, stroke: 0.40, label: "CENTRAL INDUSTRIAL BELT / DEMO FIELD", sublabel: "white test facility · logistics warehouse · elevated road · industrial sunset",
    bgTop: "#D58A68", bgBottom: "#26323A",
  }),
  "utf8",
);

const manifest = {
  generatedAt: new Date().toISOString(),
  units: "1 OBJ unit = 1 meter",
  axis: "Y up, Z forward",
  assets: {
    yamadaK1: meshStats(k1),
    centralIndustrialBelt: meshStats(field),
    centralIndustrialBeltCollision: meshStats(collision),
  },
  materials: Object.keys(MATERIALS),
};
fs.writeFileSync(path.join(rootDir, "asset_manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`, "utf8");

console.log(JSON.stringify(manifest, null, 2));
