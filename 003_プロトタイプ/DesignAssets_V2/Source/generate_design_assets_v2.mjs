import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { fileURLToPath } from "node:url";

const sourceDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(sourceDir, "..");
const modelDir = path.join(rootDir, "Models");
const materialDir = path.join(rootDir, "Materials");
const previewDir = path.join(rootDir, "Previews");
for (const dir of [modelDir, materialDir, previewDir]) fs.mkdirSync(dir, { recursive: true });

const MAT = {
  K1_CeramicWhite: { hex: "#D9D9D2", base: [0.70, 0.70, 0.66, 1], metallic: 0.18, rough: 0.48 },
  K1_WarmWhite: { hex: "#ECE9DF", base: [0.82, 0.80, 0.74, 1], metallic: 0.08, rough: 0.58 },
  K1_Gunmetal: { hex: "#343B40", base: [0.09, 0.11, 0.12, 1], metallic: 0.62, rough: 0.40 },
  K1_Undersuit: { hex: "#161C20", base: [0.025, 0.035, 0.045, 1], metallic: 0.05, rough: 0.84 },
  K1_Joint: { hex: "#252B2F", base: [0.055, 0.065, 0.075, 1], metallic: 0.35, rough: 0.60 },
  YTC_Orange: { hex: "#F28C28", base: [0.88, 0.34, 0.045, 1], metallic: 0.12, rough: 0.44 },
  K1_VisorMint: { hex: "#8DE4D0", base: [0.18, 0.66, 0.54, 1], metallic: 0.10, rough: 0.22, emissive: [0.12, 0.50, 0.40] },
  K1_Repair: { hex: "#696B69", base: [0.18, 0.19, 0.18, 1], metallic: 0.38, rough: 0.72 },
  Field_Walkable: { hex: "#D8D9D4", base: [0.69, 0.70, 0.67, 1], metallic: 0.06, rough: 0.78 },
  Field_WalkableTop: { hex: "#F0EFE8", base: [0.83, 0.82, 0.77, 1], metallic: 0.04, rough: 0.72 },
  Field_Edge: { hex: "#293137", base: [0.07, 0.09, 0.11, 1], metallic: 0.35, rough: 0.64 },
  Field_Hazard: { hex: "#E7B83E", base: [0.78, 0.52, 0.06, 1], metallic: 0.08, rough: 0.66 },
  Field_Danger: { hex: "#C93F3F", base: [0.62, 0.06, 0.06, 1], metallic: 0.05, rough: 0.62 },
  Field_Interactive: { hex: "#39A9DB", base: [0.05, 0.42, 0.64, 1], metallic: 0.10, rough: 0.38, emissive: [0.02, 0.18, 0.35] },
  Field_BackMid: { hex: "#75818A", base: [0.28, 0.33, 0.37, 1], metallic: 0.08, rough: 0.82 },
  Field_TestWhite: { hex: "#A9B0B1", base: [0.43, 0.46, 0.46, 1], metallic: 0.04, rough: 0.84 },
  Field_BackFar: { hex: "#46545D", base: [0.13, 0.18, 0.21, 1], metallic: 0.05, rough: 0.88 },
  Field_Sky: { hex: "#B97364", base: [0.42, 0.18, 0.15, 1], metallic: 0, rough: 1 },
  Enemy_Body: { hex: "#35393D", base: [0.10, 0.11, 0.12, 1], metallic: 0.34, rough: 0.68 },
  Enemy_Telegraph: { hex: "#E54848", base: [0.78, 0.06, 0.06, 1], metallic: 0.02, rough: 0.44, emissive: [0.55, 0.02, 0.02] },
};

const add = (a, b) => a.map((v, i) => v + b[i]);
const sub = (a, b) => a.map((v, i) => v - b[i]);
const mul = (a, s) => a.map((v) => v * s);
const dot = (a, b) => a.reduce((s, v, i) => s + v * b[i], 0);
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const length = (a) => Math.sqrt(dot(a, a));
const norm = (a) => { const l = length(a); return l > 1e-8 ? mul(a, 1 / l) : [0, 1, 0]; };

function rotatePoint(p, r) {
  let [x, y, z] = p;
  let c = Math.cos(r[0]), s = Math.sin(r[0]); [y, z] = [y * c - z * s, y * s + z * c];
  c = Math.cos(r[1]); s = Math.sin(r[1]); [x, z] = [x * c + z * s, -x * s + z * c];
  c = Math.cos(r[2]); s = Math.sin(r[2]); [x, y] = [x * c - y * s, x * s + y * c];
  return [x, y, z];
}

class Mesh {
  constructor(name) { this.name = name; this.parts = []; }
  add(name, material, vertices, faces, joint = "Chest") { this.parts.push({ name, material, vertices, faces, joint }); }
  vertices() { return this.parts.flatMap((p) => p.vertices); }
}

function octPrism(mesh, name, center, height, lower, upper, material, joint, rot = [0, 0, 0]) {
  const ring = (x, z) => [[-x * 0.62, -z], [x * 0.62, -z], [x, -z * 0.58], [x, z * 0.58], [x * 0.62, z], [-x * 0.62, z], [-x, z * 0.58], [-x, -z * 0.58]];
  const local = [...ring(lower[0] / 2, lower[1] / 2).map(([x, z]) => [x, -height / 2, z]), ...ring(upper[0] / 2, upper[1] / 2).map(([x, z]) => [x, height / 2, z])];
  const vertices = local.map((p) => add(rotatePoint(p, rot), center));
  const faces = [];
  for (let i = 1; i < 7; i++) faces.push([0, i + 1, i], [8, 8 + i, 8 + i + 1]);
  for (let i = 0; i < 8; i++) { const n = (i + 1) % 8; faces.push([i, n, 8 + n], [i, 8 + n, 8 + i]); }
  mesh.add(name, material, vertices, faces, joint);
}

function box(mesh, name, center, size, material, joint = "Chest", rot = [0, 0, 0]) {
  const [x, y, z] = size.map((v) => v / 2);
  const local = [[-x,-y,-z],[x,-y,-z],[x,y,-z],[-x,y,-z],[-x,-y,z],[x,-y,z],[x,y,z],[-x,y,z]];
  const vertices = local.map((p) => add(rotatePoint(p, rot), center));
  const faces = [[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[3,7,6],[3,6,2],[0,4,7],[0,7,3],[1,2,6],[1,6,5]];
  mesh.add(name, material, vertices, faces, joint);
}

function cylinder(mesh, name, p0, p1, r0, r1, sides, material, joint) {
  const axis = norm(sub(p1, p0));
  const ref = Math.abs(axis[1]) < 0.88 ? [0, 1, 0] : [1, 0, 0];
  const u = norm(cross(axis, ref)); const v = norm(cross(axis, u));
  const vertices = [p0, p1];
  for (let i = 0; i < sides; i++) {
    const a = i * Math.PI * 2 / sides; const radial = add(mul(u, Math.cos(a)), mul(v, Math.sin(a)));
    vertices.push(add(p0, mul(radial, r0)), add(p1, mul(radial, r1)));
  }
  const faces = [];
  for (let i = 0; i < sides; i++) {
    const n = (i + 1) % sides, b0 = 2 + i * 2, t0 = b0 + 1, b1 = 2 + n * 2, t1 = b1 + 1;
    faces.push([b0,b1,t1],[b0,t1,t0],[0,b1,b0],[1,t0,t1]);
  }
  mesh.add(name, material, vertices, faces, joint);
}

function disc(mesh, name, center, axisEnd, outer, inner, material, joint) {
  cylinder(mesh, `${name}_Outer`, center, axisEnd, outer, outer, 12, material, joint);
  cylinder(mesh, `${name}_Hub`, add(center, mul(sub(axisEnd, center), 0.25)), add(center, mul(sub(axisEnd, center), 0.75)), inner, inner, 12, "K1_Joint", joint);
}

function trianglePrism(mesh, name, center, width, height, depth, material, joint = "Chest") {
  const [x,y,z] = center; const w = width / 2, d = depth / 2;
  const vertices = [[x-w,y,z-d],[x+w,y,z-d],[x,y+height,z-d],[x-w,y,z+d],[x+w,y,z+d],[x,y+height,z+d]];
  const faces = [[0,1,2],[3,5,4],[0,3,4],[0,4,1],[1,4,5],[1,5,2],[2,5,3],[2,3,0]];
  mesh.add(name, material, vertices, faces, joint);
}

const joints = [
  { name: "K1_Root", parent: null, p: [0,0,0] },
  { name: "Pelvis", parent: "K1_Root", p: [0,1.02,0] },
  { name: "Spine_01", parent: "Pelvis", p: [0,1.23,0] },
  { name: "Chest", parent: "Spine_01", p: [0,1.48,0] },
  { name: "Neck", parent: "Chest", p: [0,1.69,0] },
  { name: "Head", parent: "Neck", p: [0,1.84,0] },
  { name: "Clavicle_L", parent: "Chest", p: [-0.25,1.57,0] },
  { name: "UpperArm_L", parent: "Clavicle_L", p: [-0.40,1.54,0] },
  { name: "LowerArm_L", parent: "UpperArm_L", p: [-0.54,1.29,0.04] },
  { name: "Hand_L", parent: "LowerArm_L", p: [-0.58,1.04,0.09] },
  { name: "Clavicle_R", parent: "Chest", p: [0.25,1.57,0] },
  { name: "UpperArm_R", parent: "Clavicle_R", p: [0.40,1.54,0] },
  { name: "LowerArm_R", parent: "UpperArm_R", p: [0.54,1.29,0.04] },
  { name: "Hand_R", parent: "LowerArm_R", p: [0.58,1.04,0.09] },
  { name: "UpperLeg_L", parent: "Pelvis", p: [-0.145,0.97,0] },
  { name: "LowerLeg_L", parent: "UpperLeg_L", p: [-0.145,0.56,0.015] },
  { name: "Foot_L", parent: "LowerLeg_L", p: [-0.145,0.13,0.035] },
  { name: "Toe_L", parent: "Foot_L", p: [-0.145,0.055,0.22] },
  { name: "UpperLeg_R", parent: "Pelvis", p: [0.145,0.97,0] },
  { name: "LowerLeg_R", parent: "UpperLeg_R", p: [0.145,0.56,0.015] },
  { name: "Foot_R", parent: "LowerLeg_R", p: [0.145,0.13,0.035] },
  { name: "Toe_R", parent: "Foot_R", p: [0.145,0.055,0.22] },
];

function buildK1(pose = "bind") {
  const m = new Mesh(`Yamada_K1_V2_${pose}`);
  const combat = pose === "combat";
  const arm = combat ? {
    L: { s:[-0.40,1.54,0], e:[-0.26,1.33,0.20], w:[0.00,1.30,0.36] },
    R: { s:[0.40,1.54,0], e:[0.31,1.27,0.20], w:[0.22,1.18,0.38] },
  } : {
    L: { s:[-0.40,1.54,0], e:[-0.54,1.29,0.04], w:[-0.58,1.04,0.09] },
    R: { s:[0.40,1.54,0], e:[0.54,1.29,0.04], w:[0.58,1.04,0.09] },
  };

  for (const side of ["L", "R"]) {
    const sign = side === "L" ? -1 : 1, x = sign * 0.145;
    box(m, `FootCore_${side}`, [x,0.09,0.10], [0.20,0.12,0.40], "K1_Undersuit", `Foot_${side}`);
    octPrism(m, `FootShell_${side}`, [x,0.105,0.15], 0.13, [0.22,0.39], [0.19,0.34], side === "L" ? "K1_Gunmetal" : "K1_WarmWhite", `Foot_${side}`);
    cylinder(m, `CalfCore_${side}`, [x,0.16,0.02], [x,0.52,0.01], 0.075,0.092,10,"K1_Undersuit",`LowerLeg_${side}`);
    octPrism(m, `ShinShell_${side}`, [x,0.35,0.055], 0.35, [0.17,0.16],[0.20,0.18], "K1_CeramicWhite", `LowerLeg_${side}`);
    box(m, `ShinBlade_${side}`, [x,0.355,0.145], [0.115,0.27,0.034], side === "L" ? "K1_Repair" : "K1_WarmWhite", `LowerLeg_${side}`, [-0.05,0,0]);
    disc(m, `Knee_${side}`, [x-sign*0.045,0.555,0.01],[x+sign*0.045,0.555,0.01],0.105,0.052,"K1_Gunmetal",`LowerLeg_${side}`);
    box(m, `KneeGuard_${side}`, [x,0.59,0.125], [0.15,0.17,0.052], side === "R" ? "YTC_Orange" : "K1_CeramicWhite", `LowerLeg_${side}`, [-0.15,0,0]);
    cylinder(m, `ThighCore_${side}`, [x,0.61,0],[x,0.96,0],0.095,0.115,10,"K1_Undersuit",`UpperLeg_${side}`);
    octPrism(m, `ThighShell_${side}`, [x,0.80,0.015], 0.34, [0.19,0.18],[0.225,0.22], "K1_WarmWhite", `UpperLeg_${side}`, [0,0,sign*0.025]);
    box(m, `ThighFront_${side}`, [x,0.82,0.125], [0.125,0.22,0.035], side === "L" ? "K1_CeramicWhite" : "K1_Gunmetal", `UpperLeg_${side}`, [-0.03,0,0]);
  }
  // Left leg repair: exposed brace and three visibly separate straps.
  cylinder(m,"LeftLegBrace",[-0.235,0.21,-0.015],[-0.235,0.49,-0.005],0.018,0.018,8,"K1_Repair","LowerLeg_L");
  for (const y of [0.25,0.34,0.43]) box(m,`LeftRepairStrap_${y}`,[ -0.145,y,0.137],[0.19,0.024,0.025],"YTC_Orange","LowerLeg_L",[0,0,-0.08]);

  octPrism(m,"PelvisCore",[0,1.06,0],0.23,[0.34,0.21],[0.41,0.25],"K1_Undersuit","Pelvis");
  octPrism(m,"PelvisArmor",[0,1.085,0.04],0.20,[0.38,0.24],[0.43,0.25],"K1_CeramicWhite","Pelvis");
  box(m,"PelvisOrangeLatch",[-0.16,1.10,0.17],[0.055,0.08,0.026],"YTC_Orange","Pelvis",[0,0,-0.18]);
  cylinder(m,"WaistBellows",[0,1.17,0],[0,1.27,0],0.15,0.17,12,"K1_Undersuit","Spine_01");
  octPrism(m,"TorsoCore",[0,1.40,0],0.37,[0.35,0.24],[0.56,0.31],"K1_Undersuit","Chest");
  // Split chest plates preserve the formal white K1 silhouette while leaving a dark flex channel.
  octPrism(m,"ChestPlate_L",[-0.145,1.45,0.105],0.30,[0.22,0.14],[0.29,0.19],"K1_WarmWhite","Chest",[0,0,-0.035]);
  octPrism(m,"ChestPlate_R",[0.145,1.45,0.105],0.30,[0.22,0.14],[0.29,0.19],"K1_CeramicWhite","Chest",[0,0,0.035]);
  box(m,"ChestCenterRail",[0,1.44,0.205],[0.046,0.27,0.036],"K1_Gunmetal","Chest");
  box(m,"ChestOrangeWire",[-0.226,1.43,0.203],[0.025,0.24,0.026],"YTC_Orange","Chest",[0,0,-0.19]);
  box(m,"ChestRepairScar_A",[0.13,1.48,0.211],[0.014,0.16,0.014],"K1_Repair","Chest",[0,0,0.55]);
  box(m,"ChestRepairScar_B",[0.18,1.44,0.212],[0.012,0.10,0.014],"K1_Repair","Chest",[0,0,0.55]);
  // ytc smile mark: legible at gameplay distance.
  box(m,"SmileEye_L",[-0.095,1.49,0.225],[0.022,0.022,0.014],"YTC_Orange","Chest");
  box(m,"SmileEye_R",[-0.050,1.49,0.225],[0.022,0.022,0.014],"YTC_Orange","Chest");
  box(m,"SmileMouth_L",[-0.092,1.445,0.226],[0.045,0.012,0.014],"YTC_Orange","Chest",[0,0,-0.18]);
  box(m,"SmileMouth_R",[-0.052,1.438,0.226],[0.045,0.012,0.014],"YTC_Orange","Chest",[0,0,0.18]);

  cylinder(m,"NeckSeal",[0,1.62,0],[0,1.70,0],0.083,0.088,12,"K1_Undersuit","Neck");
  octPrism(m,"HelmetShell",[0,1.82,0.018],0.27,[0.205,0.205],[0.18,0.18],"K1_WarmWhite","Head");
  box(m,"HelmetJaw",[0,1.735,0.09],[0.17,0.09,0.115],"K1_Gunmetal","Head",[-0.10,0,0]);
  box(m,"VisorBand",[0,1.855,0.127],[0.205,0.048,0.026],"K1_VisorMint","Head",[0.04,0,0]);
  box(m,"VisorBrow",[0,1.895,0.115],[0.215,0.032,0.042],"K1_Gunmetal","Head");
  box(m,"HelmetOrangeSlash",[-0.092,1.83,0.111],[0.026,0.15,0.018],"YTC_Orange","Head",[0,0,-0.20]);
  cylinder(m,"Antenna_L",[-0.09,1.94,-0.01],[-0.12,2.055,-0.015],0.014,0.008,8,"K1_Gunmetal","Head");

  for (const side of ["L","R"]) {
    const sign = side === "L" ? -1 : 1; const a = arm[side];
    cylinder(m,`UpperArmCore_${side}`,a.s,a.e,0.075,0.068,10,"K1_Undersuit",`UpperArm_${side}`);
    cylinder(m,`UpperArmShell_${side}`,add(a.s,[sign*0.018,-0.035,0]),add(a.e,[0,0.06,0]),0.108,0.085,10,"K1_CeramicWhite",`UpperArm_${side}`);
    disc(m,`Elbow_${side}`,add(a.e,[-sign*0.045,0,0]),add(a.e,[sign*0.045,0,0]),0.088,0.044,"K1_Gunmetal",`LowerArm_${side}`);
    cylinder(m,`ForearmCore_${side}`,a.e,a.w,0.067,0.058,10,"K1_Undersuit",`LowerArm_${side}`);
    cylinder(m,`ForearmShell_${side}`,add(a.e,[0,-0.035,0.01]),add(a.w,[0,0.035,0.005]),side === "R" ? 0.12 : 0.095,side === "R" ? 0.086 : 0.074,10,side === "R" ? "K1_Gunmetal" : "K1_WarmWhite",`LowerArm_${side}`);
    cylinder(m,`Hand_${side}`,a.w,add(a.w,[0,-0.085,0.025]),0.058,0.052,10,"K1_Joint",`Hand_${side}`);
    const shoulderX = sign * (side === "L" ? 0.425 : 0.415);
    octPrism(m,`ShoulderArmor_${side}`,[shoulderX,1.55,0.012],side === "L" ? 0.20 : 0.17,side === "L" ? [0.25,0.29] : [0.21,0.24],side === "L" ? [0.21,0.25] : [0.18,0.21],side === "L" ? "K1_CeramicWhite" : "K1_WarmWhite",`UpperArm_${side}`,[0,0,sign*(side === "L" ? 0.12 : 0.05)]);
  }
  // Asymmetric trial mechanisms: silhouette, not only colour.
  cylinder(m,"LeftShoulderSensor",[-0.515,1.56,-0.08],[-0.515,1.74,-0.08],0.036,0.026,10,"K1_Gunmetal","UpperArm_L");
  box(m,"LeftSensorMint",[-0.515,1.745,-0.08],[0.055,0.025,0.055],"K1_VisorMint","UpperArm_L");
  box(m,"RightForearmTrialRail",[0.64,1.19,0.08],[0.05,0.25,0.10],"YTC_Orange","LowerArm_R",[0,0,-0.08]);

  box(m,"BackFrame",[0,1.43,-0.17],[0.31,0.36,0.12],"K1_Gunmetal","Chest");
  cylinder(m,"JetPod_L",[-0.14,1.54,-0.25],[-0.16,1.24,-0.27],0.072,0.092,12,"K1_CeramicWhite","Chest");
  cylinder(m,"JetPod_R",[0.15,1.55,-0.25],[0.18,1.21,-0.285],0.085,0.108,12,"K1_Gunmetal","Chest");
  cylinder(m,"JetNozzle_L",[-0.16,1.245,-0.27],[-0.165,1.20,-0.275],0.074,0.058,12,"K1_VisorMint","Chest");
  cylinder(m,"JetNozzle_R",[0.18,1.215,-0.285],[0.185,1.16,-0.29],0.09,0.068,12,"K1_VisorMint","Chest");
  box(m,"BackOrangeServiceLine",[0.02,1.40,-0.235],[0.18,0.035,0.018],"YTC_Orange","Chest",[0,0,0.12]);
  return m;
}

function buildK11() {
  const m = new Mesh("K11_Rifle_V2");
  box(m,"K11_Core",[0,0,0.34],[0.16,0.17,0.54],"K1_Gunmetal","Hand_R");
  octPrism(m,"K11_UpperShell",[0,0.075,0.35],0.16,[0.20,0.55],[0.15,0.48],"K1_WarmWhite","Hand_R",[Math.PI/2,0,0]);
  box(m,"K11_OrangeSpine",[0,0.17,0.36],[0.055,0.035,0.47],"YTC_Orange","Hand_R");
  box(m,"K11_Stock",[0,-0.015,-0.05],[0.14,0.20,0.28],"K1_CeramicWhite","Hand_R",[-0.18,0,0]);
  box(m,"K11_Grip",[0,-0.18,0.16],[0.105,0.28,0.105],"K1_Undersuit","Hand_R",[-0.34,0,0]);
  box(m,"K11_Magazine",[0,-0.19,0.38],[0.11,0.28,0.13],"K1_Gunmetal","Hand_R",[-0.10,0,0]);
  box(m,"K11_Foregrip",[0,-0.105,0.49],[0.09,0.20,0.09],"K1_Undersuit","Hand_R",[-0.12,0,0]);
  box(m,"K11_BarrelRail",[0,0.02,0.67],[0.078,0.068,0.44],"K1_Gunmetal","Hand_R");
  cylinder(m,"K11_Barrel",[0,0.02,0.57],[0,0.02,0.84],0.052,0.042,12,"K1_Gunmetal","Hand_R");
  cylinder(m,"K11_Muzzle",[0,0.02,0.82],[0,0.02,0.90],0.066,0.052,12,"YTC_Orange","Hand_R");
  box(m,"K11_Sight",[0,0.19,0.48],[0.09,0.08,0.14],"K1_VisorMint","Hand_R");
  return m;
}

function buildEnemy() {
  const m = new Mesh("Enemy_Readability_Proxy");
  octPrism(m,"EnemyBody",[0,0.92,0],1.10,[0.42,0.28],[0.34,0.23],"Enemy_Body","Chest");
  octPrism(m,"EnemyHead",[0,1.58,0],0.28,[0.25,0.22],[0.22,0.20],"Enemy_Body","Head");
  trianglePrism(m,"EnemySensorTriangle",[0,1.51,0.125],0.24,0.20,0.035,"Enemy_Telegraph","Head");
  cylinder(m,"EnemyLeg_L",[-0.13,0.10,0],[-0.13,0.55,0],0.09,0.11,8,"Enemy_Body","LowerLeg_L");
  cylinder(m,"EnemyLeg_R",[0.13,0.10,0],[0.13,0.55,0],0.09,0.11,8,"Enemy_Body","LowerLeg_R");
  return m;
}

function buildField(display = true) {
  const m = new Mesh(display ? "Central_Industrial_Belt_V2" : "Central_Industrial_Belt_Collision_V2");
  const floorMat = display ? "Field_Walkable" : "Field_Edge";
  const slabs = [[-13,10],[ -3.5,8.5],[5.0,7.5],[14.0,10.0]];
  for (let i=0;i<slabs.length;i++) {
    const [x,w]=slabs[i]; box(m,`COL_Walkable_${i}`,[x,-0.18,0],[w,0.36,5.2],floorMat,"K1_Root");
    if (display) {
      box(m,`WalkableTop_${i}`,[x,0.008,0],[w,0.016,5.12],"Field_WalkableTop","K1_Root");
      box(m,`FrontEdge_${i}`,[x,-0.02,-2.56],[w,0.18,0.17],"Field_Edge","K1_Root");
      for(let n=Math.ceil(x-w/2);n<x+w/2;n+=1) box(m,`EdgeNotch_${i}_${n}`,[n,-0.005,-2.67],[0.42,0.045,0.13],"Field_Edge","K1_Root");
    }
  }
  // Readable height lesson: three broad steps with vertical dark risers.
  for (let i=0;i<3;i++) {
    const x=-7.4+i*1.1, h=0.22*(i+1);
    box(m,`COL_Step_${i}`,[x,h/2,0],[1.05,h,3.4],floorMat,"K1_Root");
    if(display) { box(m,`StepTop_${i}`,[x,h+0.008,0],[1.0,0.016,3.32],"Field_WalkableTop","K1_Root"); box(m,`StepRiser_${i}`,[x-0.53,h/2,-1.62],[0.08,h,0.10],"Field_Edge","K1_Root"); }
  }
  box(m,"COL_RaisedDeck",[-2.8,0.83,0],[4.0,0.26,3.7],floorMat,"K1_Root");
  if(display) {
    box(m,"RaisedDeckTop",[-2.8,0.968,0],[3.95,0.016,3.64],"Field_WalkableTop","K1_Root");
    box(m,"RaisedDeckFront",[-2.8,0.88,-1.80],[4.0,0.20,0.16],"Field_Edge","K1_Root");
    // Blue console: slanted top + fork silhouette makes it distinct without colour.
    box(m,"InteractiveConsole",[-1.9,1.36,1.25],[0.48,0.74,0.34],"Field_Interactive","K1_Root",[-0.10,0,0]);
    box(m,"ConsoleFork_L",[-2.10,1.83,1.25],[0.10,0.32,0.12],"Field_Interactive","K1_Root");
    box(m,"ConsoleFork_R",[-1.70,1.83,1.25],[0.10,0.32,0.12],"Field_Interactive","K1_Root");
  }
  // Trench is a true floor gap in collision; teeth and red base are display only.
  if(display) {
    box(m,"DangerTrenchBase",[1.35,-1.10,0],[1.2,0.12,5.2],"Field_Danger","K1_Root");
    for(let z=-2.35;z<2.4;z+=0.55) trianglePrism(m,`HazardTooth_${z.toFixed(2)}`,[1.35,-1.03,z],0.72,0.64,0.30,"Field_Hazard","K1_Root");
    box(m,"HazardLip_L",[0.68,0.07,0],[0.18,0.14,5.2],"Field_Hazard","K1_Root");
    box(m,"HazardLip_R",[2.02,0.07,0],[0.18,0.14,5.2],"Field_Hazard","K1_Root");
  }
  box(m,"COL_Landing",[3.15,0.28,0],[2.2,0.56,3.9],floorMat,"K1_Root");
  if(display) box(m,"LandingTop",[3.15,0.568,0],[2.14,0.016,3.84],"Field_WalkableTop","K1_Root");
  box(m,"COL_HighDeck",[7.1,0.88,0],[5.6,0.25,4.0],floorMat,"K1_Root");
  if(display) {
    box(m,"HighDeckTop",[7.1,1.013,0],[5.52,0.016,3.92],"Field_WalkableTop","K1_Root");
    box(m,"HighDeckFront",[7.1,0.94,-1.96],[5.6,0.18,0.16],"Field_Edge","K1_Root");
  }
  if(!display) return m;

  // MIDGROUND: lower contrast, simple large silhouettes, no red/orange behind combat lane.
  box(m,"TestFacilityMass",[-8.5,2.5,5.1],[14,5.0,2.2],"Field_BackMid","K1_Root");
  box(m,"TestFacilityWhiteFace",[-8.5,2.4,3.94],[13.3,4.5,0.10],"Field_TestWhite","K1_Root");
  for(let x=-14;x<=-3;x+=2.2) box(m,`TestFacilityRib_${x}`,[x,2.4,3.86],[0.13,4.5,0.10],"Field_BackFar","K1_Root");
  box(m,"WarehouseMass",[6.0,2.1,5.4],[13,4.2,2.7],"Field_BackMid","K1_Root");
  for(let x=0.5;x<=11.5;x+=2.8) box(m,`WarehouseDoor_${x}`,[x,1.35,3.98],[2.05,2.7,0.10],"Field_BackFar","K1_Root");
  // Overpass is a framing silhouette, with sparse columns that do not sit behind enemy spawn markers.
  box(m,"OverpassDeck",[0,6.25,8.1],[43,0.62,3.0],"Field_BackFar","K1_Root");
  box(m,"OverpassGuard",[0,6.73,6.66],[43,0.35,0.15],"Field_BackMid","K1_Root");
  for(const x of [-16,-8,8,16]) octPrism(m,`OverpassColumn_${x}`,[x,3.05,8.0],6.1,[1.10,1.25],[0.72,0.94],"Field_BackMid","K1_Root");
  // FAR BACKGROUND: dark, low-density industrial skyline against subdued sunset.
  box(m,"SunsetBackdrop",[0,5.0,12.0],[48,11,0.15],"Field_Sky","K1_Root");
  box(m,"FarFactory",[13,2.0,10.6],[10,4,1.7],"Field_BackFar","K1_Root");
  for(const x of [11,14.3,17]) cylinder(m,`FarStack_${x}`,[x,3.7,10.3],[x,7.4+(x%2)*0.4,10.3],0.45,0.34,10,"Field_BackFar","K1_Root");
  cylinder(m,"FarTank",[-15,0.3,10],[-15,4.2,10],1.55,1.55,14,"Field_BackFar","K1_Root");
  // Foreground frame: very dark, thin and kept outside the play silhouette.
  box(m,"ForegroundRail",[0,-0.55,-4.1],[42,0.22,0.22],"Field_Edge","K1_Root");
  for(const x of [-13,13]) box(m,`ForegroundPost_${x}`,[x,0.25,-4.0],[0.20,1.6,0.20],"Field_Edge","K1_Root");
  return m;
}

function meshStats(mesh) {
  const vs=mesh.vertices(); const min=[0,1,2].map(i=>Math.min(...vs.map(v=>v[i]))); const max=[0,1,2].map(i=>Math.max(...vs.map(v=>v[i])));
  return { objects:mesh.parts.length, vertices:mesh.parts.reduce((s,p)=>s+p.vertices.length,0), triangles:mesh.parts.reduce((s,p)=>s+p.faces.length,0), bounds:{min,max,size:max.map((v,i)=>v-min[i])} };
}

function writeObj(mesh, fileName) {
  const lines=[`# ${mesh.name}`,"# Original YTC V2 asset. 1 unit = 1 meter. Y-up, +Z-forward.","mtllib ../Materials/ytc_design_assets_v2.mtl","s off"];
  let vo=0,no=0;
  for(const p of mesh.parts){ lines.push(`o ${p.name}`,`g ${p.name}`,`usemtl ${p.material}`,`# rig_joint ${p.joint}`); for(const v of p.vertices)lines.push(`v ${v.map(n=>n.toFixed(6)).join(" ")}`); const ns=p.faces.map(f=>norm(cross(sub(p.vertices[f[1]],p.vertices[f[0]]),sub(p.vertices[f[2]],p.vertices[f[0]])))); for(const n of ns)lines.push(`vn ${n.map(x=>x.toFixed(6)).join(" ")}`); p.faces.forEach((f,i)=>lines.push(`f ${f.map(x=>`${vo+x+1}//${no+i+1}`).join(" ")}`)); vo+=p.vertices.length; no+=p.faces.length; }
  fs.writeFileSync(path.join(modelDir,fileName),`${lines.join("\n")}\n`);
}

function writeMtl(){ const lines=["# Original YTC V2 materials. Values are preview/import references."]; for(const [name,m] of Object.entries(MAT)){ lines.push("",`newmtl ${name}`,`Kd ${m.base.slice(0,3).map(v=>v.toFixed(4)).join(" ")}`,"Ka 0.0300 0.0300 0.0300",`Ks ${m.metallic.toFixed(3)} ${m.metallic.toFixed(3)} ${m.metallic.toFixed(3)}`,`Ns ${Math.round((1-m.rough)*100)}`,"d 1.0","illum 2"); } fs.writeFileSync(path.join(materialDir,"ytc_design_assets_v2.mtl"),`${lines.join("\n")}\n`); }

class Bin {
  constructor(){this.chunks=[];this.length=0;}
  add(buffer,target){const pad=(4-this.length%4)%4;if(pad){this.chunks.push(Buffer.alloc(pad));this.length+=pad;}const offset=this.length;this.chunks.push(buffer);this.length+=buffer.length;return{buffer:0,byteOffset:offset,byteLength:buffer.length,...(target?{target}: {})};}
  finish(){return Buffer.concat(this.chunks);}
}
function f32(arr){return Buffer.from(new Float32Array(arr).buffer);}
function u16(arr){return Buffer.from(new Uint16Array(arr).buffer);}
function accessor(json,bin,arr,type,componentType,target,min,max){const bv=json.bufferViews.push(bin.add(componentType===5126?f32(arr):u16(arr),target))-1;const comps={SCALAR:1,VEC2:2,VEC3:3,VEC4:4,MAT4:16}[type];const a={bufferView:bv,componentType,count:arr.length/comps,type,...(min?{min}:{}),...(max?{max}:{})};json.accessors.push(a);return json.accessors.length-1;}

function qEuler(rx=0,ry=0,rz=0){const c1=Math.cos(rx/2),s1=Math.sin(rx/2),c2=Math.cos(ry/2),s2=Math.sin(ry/2),c3=Math.cos(rz/2),s3=Math.sin(rz/2);return[s1*c2*c3-c1*s2*s3,c1*s2*c3+s1*c2*s3,c1*c2*s3-s1*s2*c3,c1*c2*c3+s1*s2*s3];}

function makeBaseGltf(){return{asset:{version:"2.0",generator:"YTC DesignAssets V2 procedural original"},scene:0,scenes:[{nodes:[]}],nodes:[],meshes:[],materials:Object.entries(MAT).map(([name,m])=>({name,pbrMetallicRoughness:{baseColorFactor:m.base,metallicFactor:m.metallic,roughnessFactor:m.rough},...(m.emissive?{emissiveFactor:m.emissive}: {})})),accessors:[],bufferViews:[],buffers:[{byteLength:0}]};}

function addMeshPrimitives(json,bin,mesh,rigged,jointIndex){
  const byMat=new Map();
  for(const p of mesh.parts){if(!byMat.has(p.material))byMat.set(p.material,[]);byMat.get(p.material).push(p);}
  const primitives=[];
  for(const [material,parts] of byMat){const pos=[],normal=[],joints0=[],weights=[];for(const p of parts){const ji=jointIndex?.get(p.joint)??0;for(const face of p.faces){const n=norm(cross(sub(p.vertices[face[1]],p.vertices[face[0]]),sub(p.vertices[face[2]],p.vertices[face[0]])));for(const ix of face){pos.push(...p.vertices[ix]);normal.push(...n);if(rigged){joints0.push(ji,0,0,0);weights.push(1,0,0,0);}}}}const mins=[0,1,2].map(i=>Math.min(...pos.filter((_,k)=>k%3===i)));const maxs=[0,1,2].map(i=>Math.max(...pos.filter((_,k)=>k%3===i)));const attrs={POSITION:accessor(json,bin,pos,"VEC3",5126,34962,mins,maxs),NORMAL:accessor(json,bin,normal,"VEC3",5126,34962)};if(rigged){attrs.JOINTS_0=accessor(json,bin,joints0,"VEC4",5123,34962);attrs.WEIGHTS_0=accessor(json,bin,weights,"VEC4",5126,34962);}primitives.push({attributes:attrs,material:Object.keys(MAT).indexOf(material),mode:4});}
  return primitives;
}

function addAnimations(json,bin,nodeByName){
  const deg=(n)=>n*Math.PI/180;
  const clips=[
    {name:"Idle_Loop",t:[0,0.6,1.2],tracks:{Chest:[[0,0,0],[deg(1),0,0],[0,0,0]],Head:[[0,0,0],[0,deg(-3),0],[0,0,0]],UpperArm_L:[[0,0,deg(-4)],[0,0,deg(-2)],[0,0,deg(-4)]],UpperArm_R:[[0,0,deg(4)],[0,0,deg(2)],[0,0,deg(4)]]}},
    {name:"WalkForward_Loop",t:[0,0.2,0.4,0.6,0.8],tracks:{UpperLeg_L:[[deg(24),0,0],[0,0,0],[deg(-24),0,0],[0,0,0],[deg(24),0,0]],UpperLeg_R:[[deg(-24),0,0],[0,0,0],[deg(24),0,0],[0,0,0],[deg(-24),0,0]],LowerLeg_L:[[deg(5),0,0],[deg(42),0,0],[deg(8),0,0],[deg(3),0,0],[deg(5),0,0]],LowerLeg_R:[[deg(8),0,0],[deg(3),0,0],[deg(5),0,0],[deg(42),0,0],[deg(8),0,0]],UpperArm_L:[[deg(-15),0,0],[0,0,0],[deg(15),0,0],[0,0,0],[deg(-15),0,0]],UpperArm_R:[[deg(15),0,0],[0,0,0],[deg(-15),0,0],[0,0,0],[deg(15),0,0]],Chest:[[0,0,deg(-2)],[0,0,0],[0,0,deg(2)],[0,0,0],[0,0,deg(-2)]]}},
    {name:"WalkDepth_Positive_Loop",t:[0,0.215,0.43,0.645,0.86],tracks:{UpperLeg_L:[[deg(20),0,0],[0,0,0],[deg(-20),0,0],[0,0,0],[deg(20),0,0]],UpperLeg_R:[[deg(-20),0,0],[0,0,0],[deg(20),0,0],[0,0,0],[deg(-20),0,0]],Chest:[[0,deg(7),0],[0,deg(4),0],[0,deg(7),0],[0,deg(4),0],[0,deg(7),0]]}},
    {name:"WalkDepth_Negative_Loop",t:[0,0.215,0.43,0.645,0.86],tracks:{UpperLeg_L:[[deg(20),0,0],[0,0,0],[deg(-20),0,0],[0,0,0],[deg(20),0,0]],UpperLeg_R:[[deg(-20),0,0],[0,0,0],[deg(20),0,0],[0,0,0],[deg(-20),0,0]],Chest:[[0,deg(-7),0],[0,deg(-4),0],[0,deg(-7),0],[0,deg(-4),0],[0,deg(-7),0]]}},
    {name:"Turn180_L",t:[0,0.12,0.30],tracks:{K1_Root:[[0,0,0],[0,deg(-62),0],[0,deg(-180),0]],Chest:[[0,0,0],[0,deg(-10),deg(-4)],[0,0,0]]}},
    {name:"Turn180_R",t:[0,0.12,0.30],tracks:{K1_Root:[[0,0,0],[0,deg(62),0],[0,deg(180),0]],Chest:[[0,0,0],[0,deg(10),deg(4)],[0,0,0]]}},
    {name:"Jump_Start",t:[0,0.24],tracks:{Pelvis:[[0,0,0],[deg(-8),0,0]],UpperLeg_L:[[0,0,0],[deg(-18),0,0]],UpperLeg_R:[[0,0,0],[deg(-18),0,0]],LowerLeg_L:[[0,0,0],[deg(30),0,0]],LowerLeg_R:[[0,0,0],[deg(30),0,0]]}},
    {name:"Jump_Loop",t:[0,0.45],tracks:{Chest:[[deg(-5),0,0],[deg(-4),0,0]],UpperLeg_L:[[deg(-14),0,0],[deg(-12),0,0]],UpperLeg_R:[[deg(-14),0,0],[deg(-12),0,0]]}},
    {name:"Land",t:[0,0.10,0.28],tracks:{Pelvis:[[deg(-4),0,0],[deg(-12),0,0],[0,0,0]],LowerLeg_L:[[deg(12),0,0],[deg(38),0,0],[0,0,0]],LowerLeg_R:[[deg(12),0,0],[deg(38),0,0],[0,0,0]]}},
    {name:"Jet_Start",t:[0,0.18],tracks:{Chest:[[0,0,0],[deg(-8),0,0]],UpperLeg_L:[[0,0,0],[deg(8),0,0]],UpperLeg_R:[[0,0,0],[deg(8),0,0]]}},
    {name:"Jet_Loop",t:[0,0.5],tracks:{Chest:[[deg(-8),0,deg(-1)],[deg(-7),0,deg(1)]],UpperLeg_L:[[deg(8),0,0],[deg(10),0,0]],UpperLeg_R:[[deg(8),0,0],[deg(6),0,0]]}},
    {name:"Jet_End",t:[0,0.20],tracks:{Chest:[[deg(-8),0,0],[0,0,0]],UpperLeg_L:[[deg(8),0,0],[0,0,0]],UpperLeg_R:[[deg(8),0,0],[0,0,0]]}},
    {name:"RifleReady_Loop",t:[0,0.5,1.0],tracks:{Chest:[[deg(2),0,0],[deg(3),0,0],[deg(2),0,0]],UpperArm_L:[[deg(-55),0,deg(28)],[deg(-54),0,deg(28)],[deg(-55),0,deg(28)]],LowerArm_L:[[deg(24),deg(-8),deg(-10)],[deg(23),deg(-8),deg(-10)],[deg(24),deg(-8),deg(-10)]],Hand_L:[[0,deg(-5),0],[0,deg(-4),0],[0,deg(-5),0]],UpperArm_R:[[deg(-52),0,deg(-22)],[deg(-51),0,deg(-22)],[deg(-52),0,deg(-22)]],LowerArm_R:[[deg(18),deg(5),deg(12)],[deg(17),deg(5),deg(12)],[deg(18),deg(5),deg(12)]],Hand_R:[[0,deg(4),0],[0,deg(3),0],[0,deg(4),0]]}},
    {name:"Shoot_Recoil",t:[0,0.055,0.15],tracks:{Chest:[[0,0,0],[deg(-3),0,0],[0,0,0]],UpperArm_R:[[0,0,0],[deg(-6),0,0],[0,0,0]],LowerArm_R:[[0,0,0],[deg(8),0,0],[0,0,0]]}},
  ];
  json.animations=[];
  for(const c of clips){const a={name:c.name,samplers:[],channels:[]};const input=accessor(json,bin,c.t,"SCALAR",5126,undefined,[Math.min(...c.t)],[Math.max(...c.t)]);for(const [bone,es] of Object.entries(c.tracks)){const values=es.flatMap(e=>qEuler(...e));const output=accessor(json,bin,values,"VEC4",5126);const si=a.samplers.push({input,output,interpolation:"LINEAR"})-1;a.channels.push({sampler:si,target:{node:nodeByName.get(bone),path:"rotation"}});}json.animations.push(a);}
  return clips.map(c=>({name:c.name,length:c.t.at(-1),loop:c.name.endsWith("_Loop")}));
}

function writeRiggedGlb(mesh,fileName){const json=makeBaseGltf(),bin=new Bin(),nodeByName=new Map();for(const j of joints){const parent=j.parent?joints.find(x=>x.name===j.parent):null;const translation=parent?sub(j.p,parent.p):j.p;nodeByName.set(j.name,json.nodes.push({name:j.name,translation,children:[]})-1);}for(const j of joints){if(j.parent)json.nodes[nodeByName.get(j.parent)].children.push(nodeByName.get(j.name));}
  const socketDefs=[{name:"WeaponSocket_R",parent:"Hand_R",t:[0,-0.07,0.05]},{name:"JetSocket_L",parent:"Chest",t:[-0.14,0.06,-0.25]},{name:"JetSocket_R",parent:"Chest",t:[0.15,0.07,-0.25]}];for(const s of socketDefs){const n=json.nodes.push({name:s.name,translation:s.t})-1;json.nodes[nodeByName.get(s.parent)].children.push(n);nodeByName.set(s.name,n);}
  const jointIndex=new Map(joints.map((j,i)=>[j.name,i]));const primitives=addMeshPrimitives(json,bin,mesh,true,jointIndex);const meshIx=json.meshes.push({name:mesh.name,primitives})-1;const meshNode=json.nodes.push({name:"K1_SkinnedMesh",mesh:meshIx,skin:0})-1;
  const ibm=[];for(const j of joints){const [x,y,z]=j.p;ibm.push(1,0,0,0,0,1,0,0,0,0,1,0,-x,-y,-z,1);}const ibmAccessor=accessor(json,bin,ibm,"MAT4",5126);json.skins=[{name:"K1_GenericRig",inverseBindMatrices:ibmAccessor,skeleton:nodeByName.get("K1_Root"),joints:joints.map(j=>nodeByName.get(j.name))}];json.scenes[0].nodes=[nodeByName.get("K1_Root"),meshNode];const clipManifest=addAnimations(json,bin,nodeByName);writeGlb(json,bin,path.join(modelDir,fileName));return clipManifest;}

function writeStaticGlb(mesh,fileName,extraNodes=[]){const json=makeBaseGltf(),bin=new Bin();const primitives=addMeshPrimitives(json,bin,mesh,false);const mi=json.meshes.push({name:mesh.name,primitives})-1;const root=json.nodes.push({name:mesh.name,mesh:mi})-1;json.scenes[0].nodes=[root];for(const n of extraNodes){const ix=json.nodes.push(n)-1;json.scenes[0].nodes.push(ix);}writeGlb(json,bin,path.join(modelDir,fileName));}

function writeWeaponGlb(mesh,fileName){const json=makeBaseGltf(),bin=new Bin();const primitives=addMeshPrimitives(json,bin,mesh,false);const mi=json.meshes.push({name:mesh.name,primitives})-1;const meshNode=json.nodes.push({name:"K11_RenderMesh",mesh:mi})-1;const muzzle=json.nodes.push({name:"MuzzleSocket",translation:[0,0.02,0.90]})-1;const leftHand=json.nodes.push({name:"LeftHandTarget",translation:[0,-0.03,0.49]})-1;const root=json.nodes.push({name:"WeaponRoot",translation:[0,0,0],children:[meshNode,muzzle,leftHand]})-1;json.scenes[0].nodes=[root];writeGlb(json,bin,path.join(modelDir,fileName));}

function writeGlb(json,bin,file){const body=bin.finish();json.buffers[0].byteLength=body.length;let jb=Buffer.from(JSON.stringify(json));const jp=(4-jb.length%4)%4;jb=Buffer.concat([jb,Buffer.alloc(jp,0x20)]);const bp=(4-body.length%4)%4;const bb=Buffer.concat([body,Buffer.alloc(bp)]);const header=Buffer.alloc(12);header.writeUInt32LE(0x46546c67,0);header.writeUInt32LE(2,4);header.writeUInt32LE(12+8+jb.length+8+bb.length,8);const jh=Buffer.alloc(8);jh.writeUInt32LE(jb.length,0);jh.writeUInt32LE(0x4e4f534a,4);const bh=Buffer.alloc(8);bh.writeUInt32LE(bb.length,0);bh.writeUInt32LE(0x004e4942,4);fs.writeFileSync(file,Buffer.concat([header,jh,jb,bh,bb]));}

function cloneTransform(mesh,name,translation=[0,0,0],rotation=[0,0,0],scale=1){const out=new Mesh(name);for(const p of mesh.parts)out.add(p.name,p.material,p.vertices.map(v=>add(mul(rotatePoint(v,rotation),scale),translation)),p.faces,p.joint);return out;}
function merge(name,...meshes){const out=new Mesh(name);for(const m of meshes)for(const p of m.parts)out.parts.push({...p,name:`${m.name}_${p.name}`});return out;}

function hexShade(hex,f){const n=parseInt(hex.slice(1),16),c=[n>>16,(n>>8)&255,n&255].map(v=>Math.max(0,Math.min(255,Math.round(v*f))).toString(16).padStart(2,"0"));return`#${c.join("")}`;}
function makeSvg(mesh,opt){const view=norm(opt.view??[1.3,0.65,2]),right=norm(cross([0,1,0],view)),up=norm(cross(view,right)),target=opt.target??[0,1,0],tris=[];let pAll=[];for(const p of mesh.parts)for(const f of p.faces){const vs=f.map(i=>p.vertices[i]),n=norm(cross(sub(vs[1],vs[0]),sub(vs[2],vs[0]))),proj=vs.map(v=>{const d=sub(v,target);return[dot(d,right),dot(d,up),dot(d,view)];});pAll.push(...proj);if(dot(n,view)<-0.05)continue;tris.push({proj,d:proj.reduce((s,v)=>s+v[2],0)/3,n,mat:p.material});}let scale=opt.scale;if(!scale){const minX=Math.min(...pAll.map(p=>p[0])),maxX=Math.max(...pAll.map(p=>p[0])),minY=Math.min(...pAll.map(p=>p[1])),maxY=Math.max(...pAll.map(p=>p[1]));scale=Math.min((opt.w-2*(opt.margin??60))/(maxX-minX),(opt.h-2*(opt.margin??60))/(maxY-minY));}const ox=opt.w/2+(opt.offsetX??0),oy=opt.h/2+(opt.offsetY??0);tris.sort((a,b)=>a.d-b.d);const light=norm([-0.5,1,0.8]);const polys=tris.map(t=>`<polygon points="${t.proj.map(p=>`${(ox+p[0]*scale).toFixed(1)},${(oy-p[1]*scale).toFixed(1)}`).join(" ")}" fill="${hexShade(MAT[t.mat]?.hex??"#AAA",0.70+Math.max(0,dot(t.n,light))*0.32)}" stroke="#172027" stroke-width="${opt.stroke??0.7}"/>`).join("\n");return`<svg xmlns="http://www.w3.org/2000/svg" width="${opt.w}" height="${opt.h}" viewBox="0 0 ${opt.w} ${opt.h}"><defs><linearGradient id="bg" x2="0" y2="1"><stop stop-color="${opt.top??"#A96F65"}"/><stop offset="1" stop-color="${opt.bottom??"#17232B"}"/></linearGradient></defs><rect width="100%" height="100%" fill="url(#bg)"/>${polys}<rect x="28" y="28" width="8" height="72" fill="#F28C28"/><text x="52" y="62" font-family="Arial,sans-serif" font-size="28" font-weight="700" fill="#F4F1E8">${opt.title}</text><text x="52" y="90" font-family="Arial,sans-serif" font-size="16" fill="#D1D8DA">${opt.sub}</text></svg>`;}

const k1Bind=buildK1("bind"),k1Combat=buildK1("combat"),k11=buildK11(),enemy=buildEnemy(),field=buildField(true),collision=buildField(false);
writeMtl();writeObj(k1Bind,"yamada_k1_segmented_v2.obj");writeObj(k11,"k11_rifle_v2.obj");writeObj(field,"central_industrial_belt_v2.obj");writeObj(collision,"central_industrial_belt_collision_v2.obj");
const clips=writeRiggedGlb(k1Bind,"yamada_k1_rigged_v2.glb");writeWeaponGlb(k11,"k11_rifle_v2.glb");writeStaticGlb(field,"central_industrial_belt_v2.glb");
fs.writeFileSync(path.join(previewDir,"yamada_k1_v2_preview.svg"),makeSvg(merge("K1Ready",k1Combat,cloneTransform(k11,"K11",[0.21,1.22,0.33],[0,0,0],0.92)),{w:1200,h:1200,target:[0,1.03,0.08],view:[1.25,0.45,2.0],margin:110,title:"YAMADA / K1 — VISUAL V2",sub:"formal-concept silhouette · split armor · asymmetric trial hardware · 2.055 m"}));
fs.writeFileSync(path.join(previewDir,"central_industrial_belt_v2_preview.svg"),makeSvg(field,{w:1600,h:900,target:[0,2.6,4.0],view:[1.65,0.75,-2.15],margin:48,stroke:0.38,title:"CENTRAL INDUSTRIAL BELT — FIELD V2",sub:"bright playable plane / quieter midground / sparse dark skyline / industrial sunset"}));
const gameK1=cloneTransform(merge("K1",k1Combat,cloneTransform(k11,"K11",[0.21,1.22,0.33],[0,0,0],0.92)),"K1_SIDE",[4,0,-0.35],[0,-Math.PI/2,0],1);const gameEnemy=cloneTransform(enemy,"Enemy",[-4.2,0,0.25],[0,Math.PI/2,0],1);const gameplay=merge("Gameplay",field,gameK1,gameEnemy);fs.writeFileSync(path.join(previewDir,"normal_gameplay_distance_v2.svg"),makeSvg(gameplay,{w:1600,h:900,target:[0,2.15,0],view:[0,0,-1],scale:76,offsetY:125,stroke:0.50,title:"2.5D NORMAL GAMEPLAY DISTANCE — V2",sub:"K1 / enemy / walkable / hazard / interaction remain separable at first glance"}));

const files=["yamada_k1_rigged_v2.glb","yamada_k1_segmented_v2.obj","k11_rifle_v2.glb","k11_rifle_v2.obj","central_industrial_belt_v2.glb","central_industrial_belt_v2.obj","central_industrial_belt_collision_v2.obj"];
const hashes=Object.fromEntries(files.map(f=>[f,crypto.createHash("sha256").update(fs.readFileSync(path.join(modelDir,f))).digest("hex")]));
const manifest={version:"2.1.0",generatedAt:new Date().toISOString(),license:"Original project asset; see LICENSE.md",coordinate:{unitMeters:1,up:"+Y",forward:"+Z",root:[0,0,0],importRotation:[0,0,0],importScale:[1,1,1]},k1:{dimensionsMeters:meshStats(k1Bind).bounds.size,bounds:meshStats(k1Bind).bounds,rigType:"Generic",rootMotion:false,bones:joints.map(j=>j.name),sockets:{WeaponSocket_R:"Hand_R child",MuzzleSocket:"K11 WeaponRoot child",LeftHandTarget:"K11 WeaponRoot child at [0,-0.03,0.49]",JetSocket_L:"Chest child",JetSocket_R:"Chest child"},skin:{maxInfluences:4,normalized:true,mechanicalArmorWeighting:"rigid"},clips,events:{WalkForward_Loop:[{time:0.0,event:"Footstep_L"},{time:0.4,event:"Footstep_R"}],WalkDepth_Positive_Loop:[{time:0.0,event:"Footstep_L"},{time:0.43,event:"Footstep_R"}],WalkDepth_Negative_Loop:[{time:0.0,event:"Footstep_L"},{time:0.43,event:"Footstep_R"}],Shoot_Recoil:[{time:0.0,event:"Fire"}]},motionContract:{forwardSpeedMps:4.2,forwardCycleSeconds:0.8,depthCycleSeconds:0.86,turn180Seconds:0.30,weaponPose:"RifleReady_Loop on upper-body override; LeftHand IK to K11 LeftHandTarget"}},field:{displayBounds:meshStats(field).bounds,collisionBounds:meshStats(collision).bounds,walkableTopY:[0,0.22,0.44,0.66,0.56,0.96,1.005],displaySkinTopOffsetMeters:0.016,lane:{centerZ:0,minZ:-2.56,maxZ:2.56},markers:{playerSpawn:[-15,0,0],enemySpawns:[[4.2,0,0.25],[8.0,1.05,-0.7]],interactiveConsole:[-1.9,1.36,1.25],trenchCenter:[1.35,-1.1,0]},collisionPolicy:"Only COL_* walkable/step/deck/landing meshes; backdrop, overpass, foreground, markers and hazard display are excluded."},materials:Object.fromEntries(Object.entries(MAT).map(([n,v])=>[n,{hex:v.hex,baseColorFactor:v.base,metallic:v.metallic,roughness:v.rough}])),sha256:hashes};
fs.writeFileSync(path.join(rootDir,"asset_manifest_v2.json"),`${JSON.stringify(manifest,null,2)}\n`);
console.log(JSON.stringify({k1:meshStats(k1Bind),field:meshStats(field),collision:meshStats(collision),clips:clips.length,files:files.length},null,2));
