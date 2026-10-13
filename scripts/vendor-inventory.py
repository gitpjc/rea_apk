"""Read-only static inventory of the explicitly selected installed MOOER utility."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct

ROOT = Path(__file__).resolve().parent.parent
SOURCE = Path("C:/Program Files (x86)/Mooer Audio Corp/Mooer Studio For Future Series")
OUT = ROOT / "analysis/generated/vendor"


def pe_info(data):
    if data[:2] != b"MZ" or len(data) < 64:
        return None
    pe = struct.unpack_from("<I", data, 60)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        return None
    machine, count = struct.unpack_from("<HH", data, pe + 4)
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    directory = optional + (112 if magic == 0x20b else 96)
    sections = []
    for index in range(count):
        offset = optional + optional_size + index * 40
        virtual_size, rva, raw_size, raw_offset = struct.unpack_from("<IIII", data, offset + 8)
        sections.append((rva, max(virtual_size, raw_size), raw_offset))

    def file_offset(rva):
        for start, size, raw in sections:
            if start <= rva < start + size:
                return raw + rva - start
        return rva

    def name_at(rva):
        start = file_offset(rva)
        return data[start:data.index(b"\0", start)].decode("ascii", errors="replace")

    import_rva, import_size = struct.unpack_from("<II", data, directory + 8)
    managed_rva, managed_size = struct.unpack_from("<II", data, directory + 14 * 8)
    imports = []
    if import_rva:
        descriptor = file_offset(import_rva)
        for _ in range(min(import_size // 20, 2048)):
            original, timestamp, forwarder, name_rva, first = struct.unpack_from("<IIIII", data, descriptor)
            if not any((original, timestamp, forwarder, name_rva, first)):
                break
            imports.append(name_at(name_rva))
            descriptor += 20
    return {"machine_id": machine, "architecture": {0x14c: "x86", 0x8664: "x86_64", 0xaa64: "arm64"}.get(machine, "unknown"),
            "pe_format": "PE32+" if magic == 0x20b else "PE32", "clr_directory_present": bool(managed_rva and managed_size),
            "imported_dlls": imports}


def main():
    files = []
    for path in sorted(SOURCE.rglob("*")):
        if not path.is_file():
            continue
        data = path.read_bytes()
        files.append({"path": path.relative_to(SOURCE).as_posix(), "bytes": len(data),
                      "sha256": hashlib.sha256(data).hexdigest(), "pe": pe_info(data)})
    executable = SOURCE / "MooerStudioForF40i.exe"
    data = executable.read_bytes()
    # Allowlisted identifiers only; do not dump arbitrary strings or URLs.
    patterns = [rb"HidD_[A-Za-z0-9_]+", rb"HidP_[A-Za-z0-9_]+", rb"hid_[A-Za-z0-9_]+",
                rb"(?:QMEMainWriteThread|StartConnectSync|RequestFileInfoData|receiveDeviceInfo|DealData|StartImportData|StartUpdateIrsNameDeleteIrs|receiveFileInfo|receiveIrUpdateNameOrDelete|emitTrafficControl)",
                rb"SetupDi[A-Za-z0-9_]+", rb"(?:ReadFile|WriteFile|CreateFileW|CreateFileA|DeviceIoControl)",
                rb"[A-Za-z_][A-Za-z0-9_]{0,45}(?:Backup|Restore|Firmware|Hid|HID|Usb|USB|Checksum|Packet)[A-Za-z0-9_]{0,45}"]
    candidates = {}
    for pattern in patterns:
        for match in re.finditer(pattern, data):
            value = match.group().decode("ascii")
            candidates.setdefault(value, {"value": value, "file_offset_hex": hex(match.start())})
    OUT.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(executable, OUT / executable.name)
    report = {"source_directory": str(SOURCE), "method": "Read-only recursive file hashes; stdlib PE header and import-DLL parsing; allowlisted executable ASCII candidates",
              "limitations": ["No execution, device I/O, decompilation or USB command verification.", "Imports and strings do not establish call paths or wire formats.", "Framework classification uses observed DLL dependencies; not an exhaustive code audit."],
              "files": files, "selected_executable_candidates": sorted(candidates.values(), key=lambda item: item["value"])}
    (OUT / "inventory.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    main_file = next(item for item in files if item["path"] == executable.name)
    print(json.dumps({"file_count": len(files), "main": main_file, "candidates": report["selected_executable_candidates"]}, indent=2))


if __name__ == "__main__":
    main()
