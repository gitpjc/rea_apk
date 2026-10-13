"""Bounded static ELF/string triage, not Dart decompilation or runtime evidence.

Extract only the two explicitly selected libapp.so files. Output allowlisted
tokens, never complete arbitrary strings, URLs, credentials or account data.
"""
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "analysis/generated"
TARGETS = [
    ("primary-code42", "apk/MOOER_iAMP_V1.6.1_2026.06.26.apk", "x86_64"),
    ("phone-code43", "apk/extracted/split_config.arm64_v8a.apk", "arm64-v8a"),
]
PATTERNS = {
    "full_uuid_candidates": re.compile(rb"(?<![0-9A-Fa-f])[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}(?![0-9A-Fa-f])"),
    "dart_package_uri_candidates": re.compile(rb"package:[A-Za-z0-9_]+/[A-Za-z0-9_./-]+\.dart"),
    "named_uuid_hints": re.compile(rb"[A-Za-z_][A-Za-z0-9_]{0,55}(?:UUID|Uuid|uuid|Characteristic|ServiceUuid|ServiceUUID)[A-Za-z0-9_]{0,55}"),
    "device_protocol_identifiers": re.compile(rb"[A-Za-z_][A-Za-z0-9_]{0,55}(?:Bluetooth|Gatt|GATT|RFCOMM|BLE|Ble|Mooer|MOOER|F15i|F15I|Iamp|IAMP|Protocol|Packet|Checksum|Midi|MIDI)[A-Za-z0-9_]{0,55}"),
    "platform_channel_candidates": re.compile(rb"(?:MethodChannel|EventChannel|BasicMessageChannel|BluetoothCharacteristic|BluetoothService|flutter_blue_plus|flutter_blue|flutter_reactive_ble|[A-Za-z0-9_.-]*(?:bluetooth|flutter_blue|reactive_ble)[A-Za-z0-9_.-]*/[A-Za-z0-9_./-]{1,100})", re.I),
}
EXACT_NAMES = re.compile(rb"(?<![A-Za-z0-9])(?:F15i(?: Li)?|F15I(?: LI)?|FUTURE SERIES|MOOER|iAMP)(?![A-Za-z0-9])")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def elf_header(data):
    if len(data) < 64 or data[:4] != b"\x7fELF":
        raise ValueError("Expected ELF library")
    elf_class, encoding = data[4], data[5]
    if elf_class not in (1, 2) or encoding not in (1, 2):
        raise ValueError("Unsupported ELF header")
    byte_order = "<" if encoding == 1 else ">"
    machine = struct.unpack_from(byte_order + "H", data, 18)[0]
    return {"bits": 64 if elf_class == 2 else 32,
            "endianness": "little" if encoding == 1 else "big",
            "machine_id": machine,
            "architecture": {62: "x86_64", 183: "aarch64", 40: "arm", 3: "x86"}.get(machine, "unknown")}


def candidates(data, pattern, limit=300, relevant=None):
    # Find complete printable runs first, then return only matched allowlisted
    # substrings. Offsets identify static file bytes, not execution addresses.
    results = {}
    for printable in re.finditer(rb"[\x20-\x7e]{4,}", data):
        for match in pattern.finditer(printable.group()):
            value = match.group().decode("ascii")
            if relevant and not relevant.search(value):
                continue
            if value not in results:
                results[value] = {"value": value, "file_offset_hex": hex(printable.start() + match.start()), "occurrences": 0}
            results[value]["occurrences"] += 1
    ordered = sorted(results.values(), key=lambda item: item["value"])
    return {"unique_count": len(ordered), "truncated": len(ordered) > limit, "items": ordered[:limit]}


def main():
    report = {
        "schema_version": 1,
        "method": "ZIP-selected libapp.so extraction, SHA-256, ELF header parsing and allowlisted ASCII token matching",
        "limitations": [
            "No target execution, Dart source recovery, decompilation or protocol verification.",
            "ASCII-only scan misses encoded, split, compressed, UTF-16 and constructed strings.",
            "Names/UUIDs may describe frameworks, unused code or unrelated functions; they do not prove control endpoints.",
            "File offsets are provenance within extracted ELF, not recovered function addresses.",
            "Output tokens are allowlisted; no complete arbitrary strings, URLs or credentials are retained.",
            "Each category is capped at 300 unique tokens; counts and truncation flags preserve coverage limits.",
        ],
        "artifacts": [],
    }
    for build, apk_relative, abi in TARGETS:
        apk = ROOT / apk_relative
        member = f"lib/{abi}/libapp.so"
        with zipfile.ZipFile(apk) as archive:
            data = archive.read(member)
        destination = OUT / "native" / build / abi / "libapp.so"
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        with apk.open("rb") as stream:
            apk_sha256 = hashlib.file_digest(stream, "sha256").hexdigest()
        item = {"build_label": build, "apk": apk_relative, "apk_sha256": apk_sha256,
                "member": member, "extracted_path": destination.relative_to(ROOT).as_posix(),
                "bytes": len(data), "sha256": sha(data), "elf": elf_header(data),
                "candidates": {name: candidates(data, pattern,
                    relevant=re.compile(r"bluetooth|flutter_blue|reactive_ble|/ble/|/ble_|f15|protocol|packet|platform_channel", re.I)
                    if name == "dart_package_uri_candidates" else None)
                    for name, pattern in PATTERNS.items()}}
        item["candidates"]["device_name_tokens"] = candidates(data, EXACT_NAMES)
        report["artifacts"].append(item)
    OUT.mkdir(parents=True, exist_ok=True)
    output = OUT / "flutter-triage.json"
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": output.relative_to(ROOT).as_posix(), "artifacts": [
        {"build": item["build_label"], "elf": item["elf"], "sha256": item["sha256"],
         "candidate_counts": {name: result["unique_count"] for name, result in item["candidates"].items()}}
        for item in report["artifacts"]]}, indent=2))


if __name__ == "__main__":
    main()
