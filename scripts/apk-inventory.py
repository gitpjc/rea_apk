"""Offline APK inventory using existing bundled Python, never an installed dependency.

Uses the established ZIP and struct libraries for checked binary-table inspection;
keeps that specialized parsing separate from the C# application and Node tooling.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "analysis/generated"
TOOLS = Path(os.environ.get("ANDROID_BUILD_TOOLS", "D:/JAVA_STUFF/Android/Sdk/build-tools/36.0.0"))
JAVA = Path(os.environ.get("JAVA_HOME", "D:/JAVA_STUFF/OpenJDK/jdk-21.0.1")) / "bin/java.exe"


def command(args):
    result = subprocess.run([str(a) for a in args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return {"exit_code": result.returncode, "stdout": result.stdout, "stderr": result.stderr}


def dex_strings(data):
    if not data.startswith(b"dex\n"):
        return []
    count, offset = struct.unpack_from("<II", data, 56)
    if offset + count * 4 > len(data):
        raise ValueError("Invalid DEX string table")
    values = []
    for i in range(count):
        pos = struct.unpack_from("<I", data, offset + i * 4)[0]
        for _ in range(5):
            byte = data[pos]
            pos += 1
            if byte < 128:
                break
        else:
            raise ValueError("Invalid DEX string length")
        end = data.index(b"\0", pos)
        values.append(data[pos:end].decode("utf-8", errors="replace"))
    return values


OUT.mkdir(parents=True, exist_ok=True)
records = []
for apk in sorted((ROOT / "apk").rglob("*.apk")):
    tag = apk.relative_to(ROOT / "apk").as_posix().replace("/", "_").removesuffix(".apk")
    with apk.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    badging = command([TOOLS / "aapt.exe", "dump", "badging", apk])
    manifest = command([TOOLS / "aapt.exe", "dump", "xmltree", apk, "AndroidManifest.xml"])
    signing = command([JAVA, "-jar", TOOLS / "lib/apksigner.jar", "verify", "--print-certs", apk])
    (OUT / f"{tag}.badging.txt").write_text(badging["stdout"], encoding="utf-8")
    (OUT / f"{tag}.manifest.txt").write_text(manifest["stdout"], encoding="utf-8")
    (OUT / f"{tag}.signing.txt").write_text(signing["stdout"] + signing["stderr"], encoding="utf-8")
    candidates = []
    native_facts = []
    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        for name in names:
            if name.endswith(".dex"):
                for value in dex_strings(archive.read(name)):
                    if re.search(r"bluetooth|flutter_blue|reactive_ble|usbserial|UsbManager|BluetoothGatt|methodchannel|mooer|RFCOMM", value, re.I):
                        candidates.append({"member": name, "value": value})
            if name.startswith("lib/") and name.endswith(".so"):
                data = archive.read(name)
                native_facts.append({"member": name, "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)})
        record = {"path": apk.relative_to(ROOT).as_posix(), "sha256": digest, "bytes": apk.stat().st_size,
                  "package": re.search(r"^package: (.+)$", badging["stdout"], re.M).group(1) if badging["exit_code"] == 0 else None,
                  "signature_verified": signing["exit_code"] == 0,
                  "signing_sha256": re.findall(r"^Signer #\d+ certificate SHA-256 digest: (\w+)", signing["stdout"], re.M),
                  "source_stamp_sha256": re.findall(r"^Source Stamp Signer certificate SHA-256 digest: (\w+)", signing["stdout"], re.M),
                  "dex": [n for n in names if n.endswith(".dex")], "native": native_facts,
                  "asset_count": sum(n.startswith("assets/") for n in names), "plugin_candidates": candidates}
    records.append(record)
    print(json.dumps({k: record[k] for k in ["path", "package", "signature_verified", "signing_sha256"]}))
(OUT / "apk-inventory.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
splits = [r for r in records if r["path"].startswith("apk/extracted/")]
identities = {(re.search(r"name='([^']+)'", r["package"]).group(1),
               re.search(r"versionCode='([^']+)'", r["package"]).group(1),
               tuple(r["signing_sha256"])) for r in splits}
if len(identities) != 1:
    raise SystemExit("Split package/version/publisher certificate mismatch")
summary = [{k: r[k] for k in ["path", "sha256", "bytes", "package", "signature_verified", "signing_sha256", "source_stamp_sha256"]} for r in records]
(ROOT / "DOCO/Codex/Artifact-Inventory.json").write_text(json.dumps({"split_identity_consistent": True, "artifacts": summary}, indent=2) + "\n", encoding="utf-8")
if any(not r["signature_verified"] for r in records):
    raise SystemExit("At least one APK failed signature verification; inspect generated reports")
