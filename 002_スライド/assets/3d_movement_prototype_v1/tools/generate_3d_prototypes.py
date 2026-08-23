#!/usr/bin/env python3
"""Generate self-contained GLB prototypes and software-rendered previews.

The assets deliberately use only glTF 2.0 core features so they can be
imported by Godot, Unity, Unreal, Blender, or a browser renderer without
project-specific dependencies.
"""

from __future__ import annotations

import hashlib
import json
import math
import struct
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent.parent


def align4(data: bytearray) -> None:
    while len(data) % 4:
        data.append(0)


def vec_add(a, b):
    return tuple(a[i] + b[i] for i in range(3))


def vec_sub(a, b):
    return tuple(a[i] - b[i] for i in range(3))


def vec_mul(a, scalar):
    return tuple(v * scalar for v in a)


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def cross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def normalize(v):
    length = math.sqrt(max(dot(v, v), 1e-12))
    return tuple(x / length for x in v)


def quat_axis(axis, degrees):
    axis = normalize(axis)
    half = math.radians(degrees) * 0.5
    s = math.sin(half)
    return [axis[0] * s, axis[1] * s, axis[2] * s, math.cos(half)]


def quat_euler(rx=0.0, ry=0.0, rz=0.0):
    """XYZ Euler degrees to an xyzw quaternion."""
    x = math.radians(rx) * 0.5
    y = math.radians(ry) * 0.5
    z = math.radians(rz) * 0.5
    sx, cx = math.sin(x), math.cos(x)
    sy, cy = math.sin(y), math.cos(y)
    sz, cz = math.sin(z), math.cos(z)
    return [
        sx * cy * cz + cx * sy * sz,
        cx * sy * cz - sx * cy * sz,
        cx * cy * sz + sx * sy * cz,
        cx * cy * cz - sx * sy * sz,
    ]


def mat_identity():
    return [
        [1.0, 0.0, 0.0, 0.0],
        [0.0, 1.0, 0.0, 0.0],
        [0.0, 0.0, 1.0, 0.0],
        [0.0, 0.0, 0.0, 1.0],
    ]


def mat_mul(a, b):
    return [
        [sum(a[r][k] * b[k][c] for k in range(4)) for c in range(4)]
        for r in range(4)
    ]


def trs_matrix(translation, rotation, scale):
    x, y, z, w = rotation
    sx, sy, sz = scale
    xx, yy, zz = x * x, y * y, z * z
    xy, xz, yz = x * y, x * z, y * z
    wx, wy, wz = w * x, w * y, w * z
    rotation_matrix = [
        [1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0.0],
        [2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0.0],
        [2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0.0],
        [0.0, 0.0, 0.0, 1.0],
    ]
    scale_matrix = [
        [sx, 0.0, 0.0, 0.0],
        [0.0, sy, 0.0, 0.0],
        [0.0, 0.0, sz, 0.0],
        [0.0, 0.0, 0.0, 1.0],
    ]
    translation_matrix = mat_identity()
    translation_matrix[0][3] = translation[0]
    translation_matrix[1][3] = translation[1]
    translation_matrix[2][3] = translation[2]
    return mat_mul(translation_matrix, mat_mul(rotation_matrix, scale_matrix))


def transform_point(matrix, point):
    x, y, z = point
    return (
        matrix[0][0] * x + matrix[0][1] * y + matrix[0][2] * z + matrix[0][3],
        matrix[1][0] * x + matrix[1][1] * y + matrix[1][2] * z + matrix[1][3],
        matrix[2][0] * x + matrix[2][1] * y + matrix[2][2] * z + matrix[2][3],
    )


def face_geometry(vertices, faces):
    positions = []
    normals = []
    indices = []
    for face in faces:
        a, b, c = (vertices[face[i]] for i in range(3))
        normal = normalize(cross(vec_sub(b, a), vec_sub(c, a)))
        base = len(positions)
        positions.extend(vertices[i] for i in face)
        normals.extend([normal] * len(face))
        for i in range(1, len(face) - 1):
            indices.extend([base, base + i, base + i + 1])
    return positions, normals, indices


def box_geometry():
    v = [
        (-0.5, -0.5, -0.5),
        (0.5, -0.5, -0.5),
        (0.5, 0.5, -0.5),
        (-0.5, 0.5, -0.5),
        (-0.5, -0.5, 0.5),
        (0.5, -0.5, 0.5),
        (0.5, 0.5, 0.5),
        (-0.5, 0.5, 0.5),
    ]
    faces = [
        (0, 3, 2, 1),
        (4, 5, 6, 7),
        (0, 4, 7, 3),
        (1, 2, 6, 5),
        (3, 7, 6, 2),
        (0, 1, 5, 4),
    ]
    return face_geometry(v, faces)


def wedge_geometry():
    # Triangular prism: the walkable face rises from -X to +X.
    v = [
        (-0.5, -0.5, -0.5),
        (0.5, -0.5, -0.5),
        (0.5, 0.5, -0.5),
        (-0.5, -0.5, 0.5),
        (0.5, -0.5, 0.5),
        (0.5, 0.5, 0.5),
    ]
    faces = [
        (0, 2, 1),
        (3, 4, 5),
        (0, 3, 5, 2),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
    ]
    return face_geometry(v, faces)


def cylinder_geometry(segments=12):
    positions = []
    normals = []
    indices = []
    for i in range(segments):
        a0 = i * math.tau / segments
        a1 = (i + 1) * math.tau / segments
        p0 = (math.cos(a0) * 0.5, -0.5, math.sin(a0) * 0.5)
        p1 = (math.cos(a1) * 0.5, -0.5, math.sin(a1) * 0.5)
        p2 = (math.cos(a1) * 0.5, 0.5, math.sin(a1) * 0.5)
        p3 = (math.cos(a0) * 0.5, 0.5, math.sin(a0) * 0.5)
        base = len(positions)
        positions.extend([p0, p1, p2, p3])
        normals.extend(
            [
                normalize((p0[0], 0.0, p0[2])),
                normalize((p1[0], 0.0, p1[2])),
                normalize((p2[0], 0.0, p2[2])),
                normalize((p3[0], 0.0, p3[2])),
            ]
        )
        indices.extend([base, base + 1, base + 2, base, base + 2, base + 3])
    for y, normal, reverse in [(-0.5, (0.0, -1.0, 0.0), True), (0.5, (0.0, 1.0, 0.0), False)]:
        center = len(positions)
        positions.append((0.0, y, 0.0))
        normals.append(normal)
        for i in range(segments):
            angle = i * math.tau / segments
            positions.append((math.cos(angle) * 0.5, y, math.sin(angle) * 0.5))
            normals.append(normal)
        for i in range(segments):
            a = center + 1 + i
            b = center + 1 + ((i + 1) % segments)
            indices.extend([center, b, a] if reverse else [center, a, b])
    return positions, normals, indices


def sphere_geometry(segments=14, rings=8):
    positions = []
    normals = []
    indices = []
    for ring in range(rings + 1):
        phi = math.pi * ring / rings
        y = math.cos(phi) * 0.5
        radius = math.sin(phi) * 0.5
        for segment in range(segments + 1):
            theta = math.tau * segment / segments
            point = (radius * math.cos(theta), y, radius * math.sin(theta))
            positions.append(point)
            normals.append(normalize(point))
    stride = segments + 1
    for ring in range(rings):
        for segment in range(segments):
            a = ring * stride + segment
            b = a + stride
            indices.extend([a, b, a + 1, a + 1, b, b + 1])
    return positions, normals, indices


GEOMETRIES = {
    "box": box_geometry(),
    "wedge": wedge_geometry(),
    "cylinder": cylinder_geometry(),
    "sphere": sphere_geometry(),
}


class GLBBuilder:
    def __init__(self, name):
        self.name = name
        self.binary = bytearray()
        self.buffer_views = []
        self.accessors = []
        self.materials = []
        self.material_colors = []
        self.meshes = []
        self.mesh_geometry = []
        self.mesh_cache = {}
        self.nodes = []
        self.animations = []

    def add_material(self, name, color, metallic=0.0, roughness=0.8, emissive=None):
        material = {
            "name": name,
            "pbrMetallicRoughness": {
                "baseColorFactor": [*color[:3], color[3] if len(color) > 3 else 1.0],
                "metallicFactor": metallic,
                "roughnessFactor": roughness,
            },
        }
        if emissive:
            material["emissiveFactor"] = list(emissive)
        self.materials.append(material)
        self.material_colors.append(tuple(int(max(0, min(1, c)) * 255) for c in color[:3]))
        return len(self.materials) - 1

    def _buffer_view(self, payload, target=None):
        align4(self.binary)
        offset = len(self.binary)
        self.binary.extend(payload)
        view = {"buffer": 0, "byteOffset": offset, "byteLength": len(payload)}
        if target:
            view["target"] = target
        self.buffer_views.append(view)
        return len(self.buffer_views) - 1

    def _float_accessor(self, values, accessor_type, *, target=None, include_bounds=False):
        components = {"SCALAR": 1, "VEC3": 3, "VEC4": 4}[accessor_type]
        flat = [component for value in values for component in (value if isinstance(value, (tuple, list)) else [value])]
        payload = struct.pack("<" + "f" * len(flat), *flat)
        view = self._buffer_view(payload, target)
        accessor = {
            "bufferView": view,
            "componentType": 5126,
            "count": len(values),
            "type": accessor_type,
        }
        if include_bounds:
            grouped = [flat[i::components] for i in range(components)]
            accessor["min"] = [min(group) for group in grouped]
            accessor["max"] = [max(group) for group in grouped]
        self.accessors.append(accessor)
        return len(self.accessors) - 1

    def _index_accessor(self, indices):
        use_uint32 = max(indices, default=0) > 65535
        component_type = 5125 if use_uint32 else 5123
        code = "I" if use_uint32 else "H"
        payload = struct.pack("<" + code * len(indices), *indices)
        view = self._buffer_view(payload, 34963)
        accessor = {
            "bufferView": view,
            "componentType": component_type,
            "count": len(indices),
            "type": "SCALAR",
            "min": [min(indices)],
            "max": [max(indices)],
        }
        self.accessors.append(accessor)
        return len(self.accessors) - 1

    def mesh(self, shape, material):
        key = (shape, material)
        if key in self.mesh_cache:
            return self.mesh_cache[key]
        positions, normals, indices = GEOMETRIES[shape]
        pos_accessor = self._float_accessor(positions, "VEC3", target=34962, include_bounds=True)
        normal_accessor = self._float_accessor(normals, "VEC3", target=34962)
        index_accessor = self._index_accessor(indices)
        mesh = {
            "name": f"{shape}_{self.materials[material]['name']}",
            "primitives": [
                {
                    "attributes": {"POSITION": pos_accessor, "NORMAL": normal_accessor},
                    "indices": index_accessor,
                    "material": material,
                }
            ],
        }
        self.meshes.append(mesh)
        self.mesh_geometry.append((positions, indices, material))
        index = len(self.meshes) - 1
        self.mesh_cache[key] = index
        return index

    def node(self, name, *, mesh=None, translation=None, rotation=None, scale=None, parent=None, extras=None):
        node = {"name": name}
        if mesh is not None:
            node["mesh"] = mesh
        if translation and any(abs(v) > 1e-9 for v in translation):
            node["translation"] = list(translation)
        if rotation and any(abs(rotation[i]) > 1e-9 for i in range(3)):
            node["rotation"] = list(rotation)
        if scale and any(abs(scale[i] - 1.0) > 1e-9 for i in range(3)):
            node["scale"] = list(scale)
        if extras:
            node["extras"] = extras
        self.nodes.append(node)
        index = len(self.nodes) - 1
        if parent is not None:
            self.nodes[parent].setdefault("children", []).append(index)
        return index

    def primitive(self, parent, name, shape, material, translation, scale, rotation=None, extras=None):
        return self.node(
            name,
            mesh=self.mesh(shape, material),
            translation=translation,
            scale=scale,
            rotation=rotation or [0.0, 0.0, 0.0, 1.0],
            parent=parent,
            extras=extras,
        )

    def animation(self, name, channels):
        samplers = []
        channel_defs = []
        for channel in channels:
            input_accessor = self._float_accessor(channel["times"], "SCALAR", include_bounds=True)
            output_accessor = self._float_accessor(
                channel["values"],
                "VEC4" if channel["path"] == "rotation" else "VEC3",
            )
            samplers.append(
                {
                    "input": input_accessor,
                    "output": output_accessor,
                    "interpolation": channel.get("interpolation", "LINEAR"),
                }
            )
            channel_defs.append(
                {
                    "sampler": len(samplers) - 1,
                    "target": {"node": channel["node"], "path": channel["path"]},
                }
            )
        self.animations.append({"name": name, "samplers": samplers, "channels": channel_defs})

    def export(self, roots, path, extras):
        gltf = {
            "asset": {
                "version": "2.0",
                "generator": "YTC Design Manager procedural prototype generator",
                "copyright": "YTC project prototype; generated in-repository without external assets",
                "extras": extras,
            },
            "scene": 0,
            "scenes": [{"name": self.name, "nodes": list(roots)}],
            "nodes": self.nodes,
            "meshes": self.meshes,
            "materials": self.materials,
            "buffers": [{"byteLength": len(self.binary)}],
            "bufferViews": self.buffer_views,
            "accessors": self.accessors,
        }
        if self.animations:
            gltf["animations"] = self.animations
        json_data = json.dumps(gltf, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
        while len(json_data) % 4:
            json_data += b" "
        align4(self.binary)
        bin_data = bytes(self.binary)
        total_length = 12 + 8 + len(json_data) + 8 + len(bin_data)
        payload = bytearray()
        payload.extend(struct.pack("<III", 0x46546C67, 2, total_length))
        payload.extend(struct.pack("<II", len(json_data), 0x4E4F534A))
        payload.extend(json_data)
        payload.extend(struct.pack("<II", len(bin_data), 0x004E4942))
        payload.extend(bin_data)
        path.write_bytes(payload)
        triangles = sum(len(geometry[1]) // 3 for geometry in self.mesh_geometry)
        return {
            "file": path.name,
            "bytes": len(payload),
            "nodes": len(self.nodes),
            "uniqueMeshes": len(self.meshes),
            "sourceTriangles": triangles,
            "materials": len(self.materials),
            "animations": [animation["name"] for animation in self.animations],
        }


def build_palette(builder):
    return {
        "white": builder.add_material("K1_WhiteGray", (0.68, 0.70, 0.68, 1.0), metallic=0.45, roughness=0.58),
        "graphite": builder.add_material("K1_Graphite", (0.075, 0.085, 0.09, 1.0), metallic=0.55, roughness=0.72),
        "dark_metal": builder.add_material("K1_DarkMetal", (0.15, 0.17, 0.18, 1.0), metallic=0.8, roughness=0.4),
        "orange": builder.add_material("YTC_Orange", (0.94, 0.29, 0.035, 1.0), metallic=0.2, roughness=0.6),
        "green": builder.add_material("LARK_SafetyGreen", (0.48, 0.86, 0.58, 1.0), metallic=0.05, roughness=0.35, emissive=(0.08, 0.28, 0.12)),
        "blue": builder.add_material("Operation_Blue", (0.09, 0.42, 0.67, 1.0), metallic=0.25, roughness=0.4, emissive=(0.02, 0.12, 0.25)),
        "yellow": builder.add_material("Hazard_Yellow", (0.95, 0.66, 0.05, 1.0), metallic=0.05, roughness=0.7),
        "floor": builder.add_material("Field_Floor", (0.24, 0.27, 0.28, 1.0), metallic=0.25, roughness=0.88),
        "wall": builder.add_material("Field_Wall", (0.58, 0.61, 0.60, 1.0), metallic=0.18, roughness=0.82),
    }


def add_armor_strip(builder, parent, palette, name, translation, scale, rotation=None):
    builder.primitive(parent, name, "box", palette["orange"], translation, scale, rotation)


def build_yamada():
    builder = GLBBuilder("Yamada_K1_Prototype_v1")
    p = build_palette(builder)
    root = builder.node(
        "YAMADA_K1_ROOT",
        extras={
            "assetId": "CHR_YAMADA_K1_PROTO_V1",
            "units": "meters",
            "upAxis": "+Y",
            "forwardAxis": "+Z",
            "groundOrigin": [0.0, 0.0, 0.0],
            "boundsMeters": [1.42, 2.30, 0.90],
            "recommendedCapsule": {"radius": 0.38, "height": 1.86, "center": [0.0, 0.93, 0.0]},
            "prototypePurpose": "WASD movement and Space jump visual validation",
        },
    )
    rig = builder.node("RIG_YAMADA", parent=root)
    pelvis = builder.node("RIG_PELVIS", translation=[0.0, 1.28, 0.0], parent=rig)
    builder.primitive(pelvis, "VIS_PELVIS_UNDERSUIT", "box", p["graphite"], [0, 0, 0], [0.58, 0.30, 0.34])
    builder.primitive(pelvis, "VIS_PELVIS_ARMOR_FRONT", "box", p["white"], [0, 0.03, 0.20], [0.54, 0.19, 0.12])
    builder.primitive(pelvis, "VIS_BELT", "box", p["dark_metal"], [0, 0.10, 0], [0.68, 0.10, 0.38])
    add_armor_strip(builder, pelvis, p, "VIS_BELT_ORANGE_TAB", [0.24, 0.11, 0.205], [0.08, 0.08, 0.015])

    torso = builder.node("RIG_SPINE", translation=[0.0, 0.10, 0.0], parent=pelvis)
    builder.primitive(torso, "VIS_TORSO_UNDERSUIT", "box", p["graphite"], [0, 0.26, 0], [0.67, 0.58, 0.38])
    builder.primitive(torso, "VIS_CHEST_ARMOR", "box", p["white"], [0, 0.29, 0.235], [0.63, 0.43, 0.13])
    builder.primitive(torso, "VIS_CHEST_UPPER_PLATE", "box", p["white"], [0, 0.52, 0.16], [0.74, 0.16, 0.23], rotation=quat_euler(8, 0, 0))
    add_armor_strip(builder, torso, p, "VIS_CHEST_ORANGE_LINE", [0.0, 0.47, 0.315], [0.42, 0.025, 0.012])
    # Small service-panel smile: two eyes and a restrained mouth bar.
    for x in (-0.055, 0.055):
        builder.primitive(torso, f"VIS_SMILE_EYE_{x:+.3f}", "sphere", p["orange"], [x, 0.25, 0.31], [0.026, 0.026, 0.012])
    builder.primitive(torso, "VIS_SMILE_MOUTH", "box", p["orange"], [0, 0.19, 0.31], [0.12, 0.018, 0.012], rotation=quat_euler(0, 0, -5))

    builder.primitive(torso, "VIS_BACK_SERVICE_PACK", "box", p["dark_metal"], [0, 0.28, -0.29], [0.54, 0.42, 0.18])
    for side, x in [("L", -0.22), ("R", 0.22)]:
        builder.primitive(torso, f"VIS_JET_{side}", "cylinder", p["dark_metal"], [x, 0.23, -0.37], [0.18, 0.48, 0.18])
        builder.primitive(torso, f"VIS_JET_NOZZLE_{side}", "cylinder", p["blue"], [x, -0.035, -0.37], [0.13, 0.035, 0.13])

    head = builder.node("RIG_HEAD", translation=[0.0, 0.73, 0.0], parent=torso)
    builder.primitive(head, "VIS_HELMET_CORE", "sphere", p["white"], [0, 0, 0], [0.42, 0.38, 0.40])
    builder.primitive(head, "VIS_HELMET_JAW", "box", p["dark_metal"], [0, -0.08, 0.20], [0.34, 0.16, 0.20], rotation=quat_euler(-8, 0, 0))
    builder.primitive(head, "VIS_HELMET_VISOR", "box", p["green"], [0, 0.04, 0.365], [0.36, 0.075, 0.025])
    builder.primitive(head, "VIS_HELMET_SENSOR_L", "cylinder", p["dark_metal"], [-0.22, 0.00, 0.0], [0.11, 0.05, 0.11], rotation=quat_euler(0, 0, 90))
    add_armor_strip(builder, head, p, "VIS_HELMET_ORANGE_TAB", [0.22, -0.03, 0.05], [0.025, 0.11, 0.04])

    limb_nodes = {}
    for side, sign in [("L", -1.0), ("R", 1.0)]:
        hip = builder.node(f"RIG_UPPER_LEG_{side}", translation=[0.21 * sign, -0.05, 0], parent=pelvis)
        limb_nodes[f"upper_leg_{side}"] = hip
        builder.primitive(hip, f"VIS_UPPER_LEG_INNER_{side}", "cylinder", p["graphite"], [0, -0.27, 0], [0.24, 0.55, 0.24])
        builder.primitive(hip, f"VIS_THIGH_ARMOR_{side}", "box", p["white"], [0, -0.25, 0.10], [0.31 if side == "L" else 0.28, 0.45, 0.25], rotation=quat_euler(0, 0, -2 * sign))
        if side == "L":
            builder.primitive(hip, "VIS_LEFT_LEG_CALIBRATION_PORT", "cylinder", p["orange"], [-0.16, -0.24, 0.12], [0.06, 0.025, 0.06], rotation=quat_euler(0, 0, 90))
        knee = builder.node(f"RIG_LOWER_LEG_{side}", translation=[0, -0.55, 0], parent=hip)
        limb_nodes[f"lower_leg_{side}"] = knee
        builder.primitive(knee, f"VIS_KNEE_JOINT_{side}", "sphere", p["dark_metal"], [0, 0, 0], [0.27, 0.19, 0.26])
        builder.primitive(knee, f"VIS_SHIN_INNER_{side}", "cylinder", p["graphite"], [0, -0.25, 0], [0.20, 0.48, 0.20])
        builder.primitive(knee, f"VIS_SHIN_ARMOR_{side}", "box", p["white"], [0, -0.24, 0.11], [0.28, 0.40, 0.24])
        boot = builder.node(f"RIG_FOOT_{side}", translation=[0, -0.57, 0.06], parent=knee)
        builder.primitive(boot, f"VIS_BOOT_{side}", "box", p["graphite"], [0, 0, 0.07], [0.30, 0.22, 0.46])
        builder.primitive(boot, f"VIS_BOOT_TOE_{side}", "box", p["white"], [0, 0.03, 0.25], [0.27, 0.14, 0.17])
        add_armor_strip(builder, boot, p, f"VIS_BOOT_ORANGE_{side}", [0.12 * sign, 0.08, 0.22], [0.03, 0.10, 0.05])

        shoulder = builder.node(f"RIG_UPPER_ARM_{side}", translation=[0.48 * sign, 0.48, 0], parent=torso)
        limb_nodes[f"upper_arm_{side}"] = shoulder
        builder.primitive(shoulder, f"VIS_SHOULDER_JOINT_{side}", "sphere", p["dark_metal"], [0, 0, 0], [0.27, 0.25, 0.27])
        shoulder_scale = [0.36, 0.24, 0.34] if side == "L" else [0.29, 0.20, 0.29]
        builder.primitive(shoulder, f"VIS_SHOULDER_ARMOR_{side}", "box", p["white"], [0.03 * sign, -0.03, 0.05], shoulder_scale, rotation=quat_euler(0, 0, -7 * sign))
        builder.primitive(shoulder, f"VIS_UPPER_ARM_{side}", "cylinder", p["graphite"], [0, -0.25, 0], [0.19, 0.46, 0.19])
        elbow = builder.node(f"RIG_LOWER_ARM_{side}", translation=[0, -0.50, 0], parent=shoulder)
        limb_nodes[f"lower_arm_{side}"] = elbow
        builder.primitive(elbow, f"VIS_ELBOW_{side}", "sphere", p["dark_metal"], [0, 0, 0], [0.20, 0.17, 0.20])
        builder.primitive(elbow, f"VIS_FOREARM_INNER_{side}", "cylinder", p["graphite"], [0, -0.22, 0], [0.17, 0.41, 0.17])
        forearm_scale = [0.25, 0.38, 0.26] if side == "R" else [0.22, 0.35, 0.23]
        builder.primitive(elbow, f"VIS_FOREARM_ARMOR_{side}", "box", p["white"], [0, -0.21, 0.10], forearm_scale)
        hand = builder.node(f"RIG_HAND_{side}", translation=[0, -0.46, 0.02], parent=elbow)
        builder.primitive(hand, f"VIS_GAUNTLET_{side}", "box", p["graphite"], [0, 0, 0.04], [0.20, 0.20, 0.18])
        add_armor_strip(builder, elbow, p, f"VIS_FOREARM_ORANGE_{side}", [0.11 * sign, -0.18, 0.20], [0.025, 0.20, 0.025])

    times = [0.0, 0.25, 0.5, 0.75, 1.0]

    def rotation_values(angles):
        return [quat_axis((1, 0, 0), angle) for angle in angles]

    builder.animation(
        "Walk_Prototype",
        [
            {"node": limb_nodes["upper_leg_L"], "path": "rotation", "times": times, "values": rotation_values([25, 0, -25, 0, 25])},
            {"node": limb_nodes["upper_leg_R"], "path": "rotation", "times": times, "values": rotation_values([-25, 0, 25, 0, -25])},
            {"node": limb_nodes["lower_leg_L"], "path": "rotation", "times": times, "values": rotation_values([0, 16, 24, 8, 0])},
            {"node": limb_nodes["lower_leg_R"], "path": "rotation", "times": times, "values": rotation_values([24, 8, 0, 16, 24])},
            {"node": limb_nodes["upper_arm_L"], "path": "rotation", "times": times, "values": rotation_values([-18, 0, 18, 0, -18])},
            {"node": limb_nodes["upper_arm_R"], "path": "rotation", "times": times, "values": rotation_values([18, 0, -18, 0, 18])},
        ],
    )
    builder.animation(
        "Idle_Prototype",
        [
            {
                "node": torso,
                "path": "translation",
                "times": [0.0, 1.0, 2.0],
                "values": [[0, 0.10, 0], [0, 0.115, 0], [0, 0.10, 0]],
            }
        ],
    )
    builder.animation(
        "Jump_Pose_Prototype",
        [
            {"node": limb_nodes["upper_leg_L"], "path": "rotation", "times": [0, 0.4, 0.8], "values": rotation_values([0, 38, 0])},
            {"node": limb_nodes["upper_leg_R"], "path": "rotation", "times": [0, 0.4, 0.8], "values": rotation_values([0, 38, 0])},
            {"node": limb_nodes["lower_leg_L"], "path": "rotation", "times": [0, 0.4, 0.8], "values": rotation_values([0, -55, 0])},
            {"node": limb_nodes["lower_leg_R"], "path": "rotation", "times": [0, 0.4, 0.8], "values": rotation_values([0, -55, 0])},
        ],
    )
    return builder, [root]


def collision_box(builder, root, p, name, center, scale, material="floor", extras=None):
    merged_extras = {"collider": "box", "isWalkable": True}
    if extras:
        merged_extras.update(extras)
    return builder.primitive(root, name, "box", p[material], center, scale, extras=merged_extras)


def build_field():
    builder = GLBBuilder("Central_Belt_Stage01_DemoField_v1")
    p = build_palette(builder)
    root = builder.node(
        "FIELD_CENTRAL_BELT_STAGE01_ROOT",
        extras={
            "assetId": "FLD_CENTRAL_BELT_STAGE01_PROTO_V1",
            "units": "meters",
            "upAxis": "+Y",
            "primaryMovementAxis": "+/-X",
            "laneCenterZ": 0.0,
            "boundsMeters": [32.0, 5.1, 8.0],
            "recommendedPlayerSpawn": [-13.7, 0.0, 0.0],
            "recommendedGoal": [13.2, 0.8, 0.0],
            "recommendedCamera": {"position": [-8.0, 4.2, 11.0], "lookAt": [-8.0, 1.2, 0.0]},
            "prototypePurpose": "WASD movement and Space jump validation",
        },
    )
    collision_box(builder, root, p, "COLLISION_LOWER_FLOOR", [0, -0.95, 0], [32, 0.30, 8], extras={"isWalkable": True, "surface": "recovery_floor"})
    collision_box(builder, root, p, "COLLISION_START_PLATFORM", [-10.0, -0.15, 0], [10.0, 0.30, 4.4])
    collision_box(builder, root, p, "COLLISION_MIDDLE_PLATFORM", [1.5, -0.15, 0], [9.0, 0.30, 4.4])
    builder.primitive(
        root,
        "COLLISION_ASCENT_RAMP",
        "wedge",
        p["floor"],
        [7.5, 0.40, 0],
        [3.0, 0.80, 4.4],
        extras={"collider": "convex", "isWalkable": True, "riseMeters": 0.8},
    )
    collision_box(builder, root, p, "COLLISION_GOAL_PLATFORM", [12.0, 0.65, 0], [6.0, 0.30, 4.4])
    collision_box(builder, root, p, "COLLISION_LOW_OBSTACLE", [-7.3, 0.30, 0], [1.0, 0.60, 1.7], material="wall", extras={"isWalkable": True, "test": "low_obstacle_jump"})
    step_specs = [(-1.4, 0.10, 0.20), (-0.3, 0.20, 0.40), (0.8, 0.30, 0.60)]
    for index, (x, center_y, height) in enumerate(step_specs, 1):
        collision_box(
            builder,
            root,
            p,
            f"COLLISION_STEP_{index:02d}",
            [x, center_y, 0],
            [1.05, height, 2.2],
            material="wall",
            extras={"test": "step_height", "heightMeters": height},
        )

    # The gap is intentionally visible and two meters wide, with a lower recovery floor.
    for x in (-4.75, -4.25, -3.75, -3.25):
        builder.primitive(root, f"VIS_GAP_HAZARD_{x:+.2f}", "box", p["yellow"], [x, -0.77, 0], [0.22, 0.035, 3.8], rotation=quat_euler(0, 0, 18))
    builder.node(
        "MARKER_JUMP_GAP",
        translation=[-4.0, 0.0, 0.0],
        parent=root,
        extras={"markerType": "jump_gap", "gapWidthMeters": 2.0, "fromX": -5.0, "toX": -3.0},
    )

    # Spawn and goal markers remain simple geometry so they are visible in any importer.
    builder.primitive(root, "MARKER_PLAYER_SPAWN", "cylinder", p["green"], [-13.7, 0.025, 0], [0.9, 0.05, 0.9], extras={"markerType": "player_spawn"})
    builder.primitive(root, "MARKER_GOAL", "cylinder", p["orange"], [13.2, 0.825, 0], [0.9, 0.05, 0.9], extras={"markerType": "goal"})

    # White test-facility architecture and depth-only background shapes.
    builder.primitive(root, "VIS_BACKGROUND_WALL", "box", p["wall"], [0, 2.0, -3.72], [32, 4.0, 0.24], extras={"collisionRecommended": False})
    for x in range(-15, 16, 5):
        builder.primitive(root, f"VIS_WALL_COLUMN_{x:+03d}", "box", p["white"], [x, 2.0, -3.52], [0.28, 4.0, 0.22])
    builder.primitive(root, "VIS_WALL_BEAM_TOP", "box", p["white"], [0, 3.85, -3.50], [32, 0.30, 0.26])
    builder.primitive(root, "VIS_OPERATION_PANEL", "box", p["blue"], [-9.8, 1.65, -3.35], [1.5, 1.0, 0.10], extras={"collisionRecommended": False})
    builder.primitive(root, "VIS_OPERATION_PANEL_INSET", "box", p["green"], [-9.8, 1.65, -3.28], [0.55, 0.12, 0.035])

    # Platform readability strips: orange for ytc-safe route, yellow only for hazards.
    strip_specs = [
        (-10.0, 0.015, 10.0),
        (1.5, 0.015, 9.0),
        (12.0, 0.815, 6.0),
    ]
    for index, (x, y, length) in enumerate(strip_specs, 1):
        builder.primitive(root, f"VIS_SAFE_EDGE_{index:02d}", "box", p["orange"], [x, y, 2.18], [length, 0.06, 0.08])

    # Industrial props sit outside the center lane so they supply scale without blocking input tests.
    for index, (x, z, scale) in enumerate([(-12.0, -2.8, 0.8), (3.6, -2.8, 1.0), (10.8, -2.8, 0.75)], 1):
        builder.primitive(root, f"VIS_SERVICE_CRATE_{index:02d}", "box", p["dark_metal"], [x, scale * 0.35, z], [scale, scale * 0.70, scale])
        add_armor_strip(builder, root, p, f"VIS_CRATE_ORANGE_{index:02d}", [x, scale * 0.36, z + scale * 0.51], [scale * 0.62, 0.055, 0.025])
    for index, x in enumerate((-14.5, 14.5), 1):
        builder.primitive(root, f"VIS_SAFETY_BOLLARD_{index:02d}", "cylinder", p["yellow"], [x, 0.55 if x < 0 else 1.35, 2.8], [0.22, 1.1, 0.22])
        builder.primitive(root, f"VIS_BOLLARD_CAP_{index:02d}", "cylinder", p["graphite"], [x, 1.12 if x < 0 else 1.92, 2.8], [0.26, 0.08, 0.26])

    builder.node(
        "MARKER_CAMERA_GUIDE",
        parent=root,
        extras={
            "markerType": "camera_guide",
            "sideViewLaneAxis": "X",
            "suggestedCameraOffset": [0.0, 3.8, 10.5],
            "lookAtHeight": 1.2,
        },
    )
    return builder, [root]


def render_preview(builder, roots, path, camera, target, size=(1200, 800), fov=40.0):
    width, height = size
    image = Image.new("RGB", size, (222, 224, 222))
    draw = ImageDraw.Draw(image)
    for y in range(height):
        shade = int(232 - 34 * (y / max(height - 1, 1)))
        draw.line([(0, y), (width, y)], fill=(shade, shade + 1, shade))

    forward = normalize(vec_sub(target, camera))
    right = normalize(cross(forward, (0.0, 1.0, 0.0)))
    up = normalize(cross(right, forward))
    focal = 0.5 * width / math.tan(math.radians(fov) * 0.5)
    light = normalize((-0.5, 0.9, 0.7))
    triangles = []

    def walk(node_index, parent_matrix):
        node = builder.nodes[node_index]
        local = trs_matrix(
            node.get("translation", [0.0, 0.0, 0.0]),
            node.get("rotation", [0.0, 0.0, 0.0, 1.0]),
            node.get("scale", [1.0, 1.0, 1.0]),
        )
        world = mat_mul(parent_matrix, local)
        if "mesh" in node:
            positions, indices, material = builder.mesh_geometry[node["mesh"]]
            world_positions = [transform_point(world, position) for position in positions]
            for index in range(0, len(indices), 3):
                points = [world_positions[indices[index + offset]] for offset in range(3)]
                relative = [vec_sub(point, camera) for point in points]
                camera_points = [
                    (dot(point, right), dot(point, up), dot(point, forward))
                    for point in relative
                ]
                if min(point[2] for point in camera_points) <= 0.05:
                    continue
                screen = [
                    (
                        width * 0.5 + focal * point[0] / point[2],
                        height * 0.52 - focal * point[1] / point[2],
                    )
                    for point in camera_points
                ]
                normal = normalize(cross(vec_sub(points[1], points[0]), vec_sub(points[2], points[0])))
                intensity = 0.48 + 0.52 * abs(dot(normal, light))
                base = builder.material_colors[material]
                color = tuple(int(max(0, min(255, component * intensity))) for component in base)
                triangles.append((sum(point[2] for point in camera_points) / 3.0, screen, color))
        for child in node.get("children", []):
            walk(child, world)

    for root in roots:
        walk(root, mat_identity())
    triangles.sort(key=lambda item: item[0], reverse=True)
    for _, points, color in triangles:
        outline = tuple(max(0, component - 38) for component in color)
        draw.polygon(points, fill=color, outline=outline)
    image.save(path, optimize=True)


def export_mtl(builder, path):
    lines = ["# YTC prototype materials; no external textures", ""]
    for material in builder.materials:
        name = material["name"]
        pbr = material["pbrMetallicRoughness"]
        color = pbr["baseColorFactor"]
        metallic = pbr["metallicFactor"]
        roughness = pbr["roughnessFactor"]
        specular = 0.12 + 0.55 * metallic
        shininess = max(4.0, 180.0 * (1.0 - roughness))
        lines.extend(
            [
                f"newmtl {name}",
                f"Ka {color[0] * 0.18:.6f} {color[1] * 0.18:.6f} {color[2] * 0.18:.6f}",
                f"Kd {color[0]:.6f} {color[1]:.6f} {color[2]:.6f}",
                f"Ks {specular:.6f} {specular:.6f} {specular:.6f}",
                f"Ns {shininess:.4f}",
                "d 1.000000",
                "illum 2",
            ]
        )
        if "emissiveFactor" in material:
            emissive = material["emissiveFactor"]
            lines.append(f"Ke {emissive[0]:.6f} {emissive[1]:.6f} {emissive[2]:.6f}")
        lines.append("")
    path.write_text("\n".join(lines), encoding="utf-8")


def export_obj(builder, roots, path, mtl_name):
    """Flatten a GLB scene to a Unity-friendly static OBJ fallback."""
    lines = [
        "# YTC design prototype static compatibility export",
        f"mtllib {mtl_name}",
        "s off",
        "",
    ]
    vertex_index = 1
    normal_index = 1
    triangle_count = 0

    def safe_name(name):
        return "".join(character if character.isalnum() or character in "_-" else "_" for character in name)

    def walk(node_index, parent_matrix):
        nonlocal vertex_index, normal_index, triangle_count
        node = builder.nodes[node_index]
        local = trs_matrix(
            node.get("translation", [0.0, 0.0, 0.0]),
            node.get("rotation", [0.0, 0.0, 0.0, 1.0]),
            node.get("scale", [1.0, 1.0, 1.0]),
        )
        world = mat_mul(parent_matrix, local)
        if "mesh" in node:
            positions, indices, material_index = builder.mesh_geometry[node["mesh"]]
            world_positions = [transform_point(world, point) for point in positions]
            lines.append(f"o {safe_name(node['name'])}")
            lines.append(f"usemtl {builder.materials[material_index]['name']}")
            for offset in range(0, len(indices), 3):
                points = [world_positions[indices[offset + corner]] for corner in range(3)]
                normal = normalize(cross(vec_sub(points[1], points[0]), vec_sub(points[2], points[0])))
                for point in points:
                    lines.append(f"v {point[0]:.6f} {point[1]:.6f} {point[2]:.6f}")
                lines.append(f"vn {normal[0]:.6f} {normal[1]:.6f} {normal[2]:.6f}")
                lines.append(
                    f"f {vertex_index}//{normal_index} {vertex_index + 1}//{normal_index} {vertex_index + 2}//{normal_index}"
                )
                vertex_index += 3
                normal_index += 1
                triangle_count += 1
            lines.append("")
        for child in node.get("children", []):
            walk(child, world)

    for root in roots:
        walk(root, mat_identity())
    path.write_text("\n".join(lines), encoding="utf-8")
    return {"file": path.name, "triangles": triangle_count, "bytes": path.stat().st_size}


def validate_glb(path):
    payload = path.read_bytes()
    if len(payload) < 28:
        raise ValueError(f"{path.name}: file too short")
    magic, version, declared_length = struct.unpack_from("<III", payload, 0)
    if magic != 0x46546C67 or version != 2 or declared_length != len(payload):
        raise ValueError(f"{path.name}: invalid GLB header")
    json_length, json_type = struct.unpack_from("<II", payload, 12)
    if json_type != 0x4E4F534A:
        raise ValueError(f"{path.name}: missing JSON chunk")
    gltf = json.loads(payload[20 : 20 + json_length].decode("utf-8"))
    bin_header = 20 + json_length
    bin_length, bin_type = struct.unpack_from("<II", payload, bin_header)
    if bin_type != 0x004E4942:
        raise ValueError(f"{path.name}: missing BIN chunk")
    if gltf["buffers"][0]["byteLength"] > bin_length:
        raise ValueError(f"{path.name}: binary buffer shorter than declared")
    for view in gltf["bufferViews"]:
        end = view.get("byteOffset", 0) + view["byteLength"]
        if end > gltf["buffers"][0]["byteLength"]:
            raise ValueError(f"{path.name}: out-of-range bufferView")
    return gltf


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    yamada_builder, yamada_roots = build_yamada()
    field_builder, field_roots = build_field()

    yamada_path = ROOT / "yamada_k1_prototype_v1.glb"
    field_path = ROOT / "central_belt_stage01_demo_field_v1.glb"
    yamada_stats = yamada_builder.export(
        yamada_roots,
        yamada_path,
        {
            "designBasis": "Yamada/Courier in K1 Version 1, white-gray test armor, ytc orange identification, visible repairable parts",
            "scope": "Design prototype; technical implementation remains owned by the technical lead",
        },
    )
    field_stats = field_builder.export(
        field_roots,
        field_path,
        {
            "designBasis": "Central Industrial Belt Stage 01: walking, jumping, low obstacles",
            "scope": "Design prototype; technical implementation remains owned by the technical lead",
        },
    )
    validate_glb(yamada_path)
    validate_glb(field_path)

    mtl_path = ROOT / "prototype_materials_v1.mtl"
    yamada_obj_path = ROOT / "yamada_k1_prototype_v1.obj"
    field_obj_path = ROOT / "central_belt_stage01_demo_field_v1.obj"
    export_mtl(yamada_builder, mtl_path)
    yamada_obj_stats = export_obj(yamada_builder, yamada_roots, yamada_obj_path, mtl_path.name)
    field_obj_stats = export_obj(field_builder, field_roots, field_obj_path, mtl_path.name)

    yamada_preview = ROOT / "preview_yamada_k1_prototype_v1.png"
    field_preview = ROOT / "preview_central_belt_stage01_demo_field_v1.png"
    render_preview(
        yamada_builder,
        yamada_roots,
        yamada_preview,
        camera=(4.5, 3.0, 6.4),
        target=(0.0, 1.15, 0.0),
        size=(1200, 900),
        fov=32.0,
    )
    render_preview(
        field_builder,
        field_roots,
        field_preview,
        camera=(25.0, 15.5, 25.0),
        target=(0.0, 0.8, 0.0),
        size=(1400, 850),
        fov=43.0,
    )

    manifest = {
        "format": "glTF 2.0 binary (.glb)",
        "coordinateSystem": {"units": "meters", "up": "+Y", "forward": "+Z", "movementLane": "+/-X"},
        "generatedWithoutExternalAssets": True,
        "assets": [
            {**yamada_stats, "sha256": sha256(yamada_path), "preview": yamada_preview.name},
            {**field_stats, "sha256": sha256(field_path), "preview": field_preview.name},
        ],
        "staticCompatibilityExports": [
            {**yamada_obj_stats, "sha256": sha256(yamada_obj_path), "materialLibrary": mtl_path.name},
            {**field_obj_stats, "sha256": sha256(field_obj_path), "materialLibrary": mtl_path.name},
            {"file": mtl_path.name, "bytes": mtl_path.stat().st_size, "sha256": sha256(mtl_path)},
        ],
    }
    (ROOT / "asset_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
