import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { fileURLToPath } from "node:url";

const sourceDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(sourceDir, "..");
const modelDir = path.join(rootDir, "Models");
const errors = [];
const notes = [];
const check = (condition, message) => { if (!condition) errors.push(message); };

function readGlb(fileName) {
  const file = path.join(modelDir, fileName);
  const b = fs.readFileSync(file);
  check(b.readUInt32LE(0) === 0x46546c67, `${fileName}: invalid GLB magic`);
  check(b.readUInt32LE(4) === 2, `${fileName}: GLB version is not 2`);
  check(b.readUInt32LE(8) === b.length, `${fileName}: declared length mismatch`);
  const jsonLength = b.readUInt32LE(12);
  check(b.readUInt32LE(16) === 0x4e4f534a, `${fileName}: missing JSON chunk`);
  const json = JSON.parse(b.subarray(20, 20 + jsonLength).toString("utf8").trim());
  const binHeader = 20 + jsonLength;
  const binLength = b.readUInt32LE(binHeader);
  check(b.readUInt32LE(binHeader + 4) === 0x004e4942, `${fileName}: missing BIN chunk`);
  check(json.buffers?.[0]?.byteLength <= binLength, `${fileName}: BIN shorter than buffer declaration`);
  for (const [i, bv] of (json.bufferViews ?? []).entries()) check((bv.byteOffset ?? 0) + bv.byteLength <= json.buffers[0].byteLength, `${fileName}: bufferView ${i} out of range`);
  for (const [i, a] of (json.accessors ?? []).entries()) check(Number.isInteger(a.bufferView) && a.bufferView >= 0 && a.bufferView < json.bufferViews.length, `${fileName}: accessor ${i} bad bufferView`);
  for (const [i, n] of (json.nodes ?? []).entries()) for (const child of n.children ?? []) check(child >= 0 && child < json.nodes.length && child !== i, `${fileName}: node ${i} bad child`);
  for (const skin of json.skins ?? []) for (const j of skin.joints ?? []) check(j >= 0 && j < json.nodes.length, `${fileName}: skin joint out of range`);
  for (const anim of json.animations ?? []) for (const c of anim.channels ?? []) check(c.target.node >= 0 && c.target.node < json.nodes.length, `${fileName}: animation target out of range`);
  notes.push(`${fileName}: ${b.length} bytes / nodes ${json.nodes?.length ?? 0} / meshes ${json.meshes?.length ?? 0} / animations ${json.animations?.length ?? 0}`);
  return json;
}

function validateObj(fileName) {
  const lines = fs.readFileSync(path.join(modelDir, fileName), "utf8").split(/\r?\n/);
  let vertices = 0, faces = 0, objects = 0;
  for (const line of lines) {
    if (line.startsWith("v ")) vertices++;
    if (line.startsWith("o ")) objects++;
    if (line.startsWith("f ")) {
      faces++;
      for (const token of line.slice(2).trim().split(/\s+/)) {
        const ix = Number(token.split("/")[0]);
        check(Number.isInteger(ix) && ix > 0 && ix <= vertices, `${fileName}: face ${faces} index ${ix} out of current vertex range ${vertices}`);
      }
    }
  }
  check(vertices > 0 && faces > 0 && objects > 0, `${fileName}: empty OBJ content`);
  notes.push(`${fileName}: objects ${objects} / vertices ${vertices} / triangles ${faces}`);
}

const k1 = readGlb("yamada_k1_rigged_v2.glb");
const k11 = readGlb("k11_rifle_v2.glb");
const field = readGlb("central_industrial_belt_v2.glb");
for (const f of ["yamada_k1_segmented_v2.obj", "k11_rifle_v2.obj", "central_industrial_belt_v2.obj", "central_industrial_belt_collision_v2.obj"]) validateObj(f);

const k1Names = new Set(k1.nodes.map((n) => n.name));
for (const name of ["K1_Root","Pelvis","Spine_01","Chest","Neck","Head","UpperArm_L","LowerArm_L","Hand_L","UpperArm_R","LowerArm_R","Hand_R","UpperLeg_L","LowerLeg_L","Foot_L","Toe_L","UpperLeg_R","LowerLeg_R","Foot_R","Toe_R","WeaponSocket_R","JetSocket_L","JetSocket_R"]) check(k1Names.has(name), `K1 missing node ${name}`);
check(k1.skins?.length === 1, "K1 must contain exactly one skin");
check(k1.animations?.length === 13, `K1 animation count ${k1.animations?.length ?? 0}, expected 13`);
const clipNames = new Set((k1.animations ?? []).map((a) => a.name));
for (const name of ["Idle_Loop","WalkForward_Loop","WalkDepth_Positive_Loop","WalkDepth_Negative_Loop","Turn180_L","Turn180_R","Jump_Start","Jump_Loop","Land","Jet_Start","Jet_Loop","Jet_End","Shoot_Recoil"]) check(clipNames.has(name), `K1 missing clip ${name}`);
for (const primitive of k1.meshes?.[0]?.primitives ?? []) for (const name of ["POSITION","NORMAL","JOINTS_0","WEIGHTS_0"]) check(Number.isInteger(primitive.attributes?.[name]), `K1 primitive missing ${name}`);

const k11Root = k11.nodes.findIndex((n) => n.name === "WeaponRoot");
const k11Muzzle = k11.nodes.findIndex((n) => n.name === "MuzzleSocket");
check(k11Root >= 0 && k11Muzzle >= 0, "K11 missing WeaponRoot or MuzzleSocket");
check(k11.nodes[k11Root]?.children?.includes(k11Muzzle), "K11 MuzzleSocket is not child of WeaponRoot");
check((field.animations?.length ?? 0) === 0 && (field.skins?.length ?? 0) === 0, "Field display must be static");

const collisionText = fs.readFileSync(path.join(modelDir, "central_industrial_belt_collision_v2.obj"), "utf8");
check(!/Backdrop|Overpass|Foreground|HazardTooth|InteractiveConsole/.test(collisionText), "Collision OBJ contains display/background object");
check((collisionText.match(/^o COL_/gm) ?? []).length === 10, "Collision OBJ expected 10 COL_* objects");

const manifest = JSON.parse(fs.readFileSync(path.join(rootDir, "asset_manifest_v2.json"), "utf8"));
for (const [fileName, expected] of Object.entries(manifest.sha256)) {
  const actual = crypto.createHash("sha256").update(fs.readFileSync(path.join(modelDir, fileName))).digest("hex");
  check(actual === expected, `${fileName}: SHA-256 mismatch`);
}
check(manifest.k1.motionContract.forwardSpeedMps === 4.2, "Manifest forward speed must be 4.2 m/s");
check(manifest.k1.motionContract.forwardCycleSeconds === 0.8, "Manifest forward cycle must be 0.8 s");
check(manifest.field.lane.minZ === -2.56 && manifest.field.lane.maxZ === 2.56, "Manifest lane boundary mismatch");
check(manifest.field.displaySkinTopOffsetMeters <= 0.02, "Field display/collision top offset exceeds 0.02 m");

const previewRequired = ["comparison_k1_old_vs_v2.png","comparison_field_old_vs_v2.png","normal_gameplay_distance_v2.png","yamada_k1_v2_preview.png","central_industrial_belt_v2_preview.png"];
for (const f of previewRequired) check(fs.existsSync(path.join(rootDir, "Previews", f)) && fs.statSync(path.join(rootDir, "Previews", f)).size > 10000, `Preview missing/too small: ${f}`);

console.log(notes.join("\n"));
if (errors.length) {
  console.error(`\nFAILED (${errors.length})\n- ${errors.join("\n- ")}`);
  process.exit(1);
}
console.log("\nPASS: GLB structure, rig/clip/socket names, OBJ indices, collision scope, hashes and previews validated.");
