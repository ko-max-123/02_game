import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const modelDir = path.join(root, "Models");
const mtlPath = path.join(root, "Materials", "ytc_design_assets.mtl");

function parseObj(fileName) {
  const filePath = path.join(modelDir, fileName);
  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/);
  const vertices = [];
  const normals = [];
  const faces = [];
  const objects = new Set();
  const usedMaterials = new Set();
  const mtllibs = [];
  const errors = [];

  for (let lineNumber = 0; lineNumber < lines.length; lineNumber += 1) {
    const line = lines[lineNumber].trim();
    if (!line || line.startsWith("#")) continue;
    const tokens = line.split(/\s+/);
    if (tokens[0] === "v") {
      const v = tokens.slice(1, 4).map(Number);
      if (v.length !== 3 || v.some((n) => !Number.isFinite(n))) errors.push(`invalid vertex at line ${lineNumber + 1}`);
      vertices.push(v);
    } else if (tokens[0] === "vn") {
      const n = tokens.slice(1, 4).map(Number);
      if (n.length !== 3 || n.some((x) => !Number.isFinite(x))) errors.push(`invalid normal at line ${lineNumber + 1}`);
      normals.push(n);
    } else if (tokens[0] === "f") {
      if (tokens.length !== 4) errors.push(`non-triangle face at line ${lineNumber + 1}`);
      const face = tokens.slice(1).map((ref) => {
        const [vi, , ni] = ref.split("/");
        return { vi: Number(vi), ni: Number(ni) };
      });
      faces.push({ face, lineNumber: lineNumber + 1 });
    } else if (tokens[0] === "o") {
      objects.add(tokens.slice(1).join(" "));
    } else if (tokens[0] === "usemtl") {
      usedMaterials.add(tokens[1]);
    } else if (tokens[0] === "mtllib") {
      mtllibs.push(tokens.slice(1).join(" "));
    }
  }

  for (const { face, lineNumber } of faces) {
    if (face.some(({ vi }) => !Number.isInteger(vi) || vi < 1 || vi > vertices.length)) errors.push(`vertex index out of range at line ${lineNumber}`);
    if (face.some(({ ni }) => !Number.isInteger(ni) || ni < 1 || ni > normals.length)) errors.push(`normal index out of range at line ${lineNumber}`);
    if (face.every(({ vi }) => vi >= 1 && vi <= vertices.length)) {
      const [a, b, c] = face.map(({ vi }) => vertices[vi - 1]);
      const ab = b.map((v, i) => v - a[i]);
      const ac = c.map((v, i) => v - a[i]);
      const cross = [ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0]];
      const area2 = Math.hypot(...cross);
      if (area2 < 1e-8) errors.push(`degenerate face at line ${lineNumber}`);
    }
  }

  const mins = [0, 1, 2].map((i) => Math.min(...vertices.map((v) => v[i])));
  const maxs = [0, 1, 2].map((i) => Math.max(...vertices.map((v) => v[i])));
  return { fileName, vertices, normals, faces, objects, usedMaterials, mtllibs, errors, mins, maxs };
}

function pngDimensions(filePath) {
  const data = fs.readFileSync(filePath);
  if (data.toString("ascii", 1, 4) !== "PNG") throw new Error(`${filePath} is not PNG`);
  return [data.readUInt32BE(16), data.readUInt32BE(20)];
}

const definedMaterials = new Set(
  fs.readFileSync(mtlPath, "utf8").split(/\r?\n/).filter((line) => line.startsWith("newmtl ")).map((line) => line.slice(7).trim()),
);

const requirements = {
  "yamada_k1_demo.obj": ["ChestOrangeStripe", "ChestRepairScarA", "LeftTrialSensor", "RightForearmTrialModule", "Jet_R_Prototype", "SmileMouthB"],
  "central_industrial_belt_demo.obj": ["JumpBarrier", "DeckBlueConsole", "HazardTrenchBase", "HazardTooth_", "WarehouseMain", "OverpassDeck", "SunsetBackdrop"],
  "central_industrial_belt_collision.obj": ["Floor_Start", "JumpBarrier", "RaisedTrainingDeck", "HazardTrenchBase", "HighPlatform"],
};

let failed = false;
for (const [fileName, requiredNames] of Object.entries(requirements)) {
  const result = parseObj(fileName);
  for (const material of result.usedMaterials) {
    if (!definedMaterials.has(material)) result.errors.push(`undefined material: ${material}`);
  }
  for (const mtllib of result.mtllibs) {
    const resolved = path.resolve(modelDir, mtllib.replaceAll("/", path.sep));
    if (!fs.existsSync(resolved)) result.errors.push(`missing mtllib: ${mtllib}`);
  }
  for (const requiredName of requiredNames) {
    if (![...result.objects].some((name) => name.startsWith(requiredName))) result.errors.push(`missing required object marker: ${requiredName}`);
  }
  if (fileName === "yamada_k1_demo.obj" && Math.abs(result.mins[1]) > 1e-6) result.errors.push(`K1 feet do not touch Y=0 (minY=${result.mins[1]})`);

  const status = result.errors.length === 0 ? "PASS" : "FAIL";
  if (result.errors.length) failed = true;
  console.log(`${status} ${fileName}`);
  console.log(`  objects=${result.objects.size} vertices=${result.vertices.length} normals=${result.normals.length} triangles=${result.faces.length}`);
  console.log(`  bounds min=[${result.mins.map((v) => v.toFixed(3)).join(", ")}] max=[${result.maxs.map((v) => v.toFixed(3)).join(", ")}]`);
  for (const error of result.errors) console.log(`  ERROR ${error}`);
}

for (const [name, expected] of [
  ["yamada_k1_preview.png", [1200, 1200]],
  ["central_industrial_belt_preview.png", [1600, 900]],
]) {
  const dims = pngDimensions(path.join(root, "Previews", name));
  const ok = dims[0] === expected[0] && dims[1] === expected[1];
  console.log(`${ok ? "PASS" : "FAIL"} ${name} ${dims[0]}x${dims[1]}`);
  if (!ok) failed = true;
}

for (const required of ["README.md", "LICENSE.txt", "asset_manifest.json", "Materials/ytc_design_assets.mtl"]) {
  const ok = fs.existsSync(path.join(root, required));
  console.log(`${ok ? "PASS" : "FAIL"} required file ${required}`);
  if (!ok) failed = true;
}

if (failed) process.exit(1);

