"""Extract the eight official base-skin English R voice variants from WPK.
Uses the event bank to resolve its random container; no guessed media IDs.
Usage: python Tools/Extract-IreliaRVoice.py <voice-bank-folder>
"""
import array
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess
import sys
import uuid
import wave

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1]).resolve()
work = root / "TestResults/IreliaVoiceImport"
work.mkdir(parents=True, exist_ok=True)
output = root / "Assets/Resources/Audio/IreliaVoice"
output.mkdir(parents=True, exist_ok=True)
event = "Play_vo_Irelia_IreliaR_cast3D"
(work / "r-names.txt").write_text(event + "\n", encoding="utf-8")
subprocess.run([sys.executable, str(root / "TestResults/IreliaAudioTools/wwiser.pyz"),
    str(source / "irelia_base_vo_events.bnk"), "-d", "none", "-g", "-gra", "-gd",
    "-go", str(work / "r-txtp"), "-nl", str(work / "r-names.txt"), "-gf", event], check=True)
recipes = sorted((work / "r-txtp").glob(event + "*.txtp"))
assert len(recipes) == 8, "Unexpected R voice container; review bank before importing"
recipe = recipes[0].read_text(encoding="utf-8")
media_ids = re.findall(r"^\s*[^\r\n#]*/(\d+)\.wem", recipe, re.MULTILINE)
assert len(media_ids) == 8 and len(set(media_ids)) == 8
package = source / "irelia_base_vo_audio.wpk"
data = package.read_bytes()
assert data[:4] == b"r3d2"
version, count = struct.unpack_from("<II", data, 4)
assert version == 1 and 12 + count * 4 <= len(data)
entries = {}
for i in range(count):
    offset = struct.unpack_from("<I", data, 12 + i * 4)[0]
    start, size, name_length = struct.unpack_from("<III", data, offset)
    name = data[offset + 12:offset + 12 + name_length * 2].decode("utf-16le").rstrip("\0")
    assert start + size <= len(data)
    entries[name] = (start, size)
manifest = {"language": "en_US", "event": event, "event_id": 1837586924,
    "source": str(source), "variants": [], "banks": [],
    "notes": ["Eight children of the R cast voice event random/shuffle container.",
              "Extracted only; gameplay playback is not connected by this importer."]}
for name in ["irelia_base_vo_audio.bnk", "irelia_base_vo_audio.wpk", "irelia_base_vo_events.bnk"]:
    manifest["banks"].append({"file": name, "sha256": hashlib.sha256((source / name).read_bytes()).hexdigest()})
decoded = []
for i, media_id in enumerate(media_ids, 1):
    start, size = entries[media_id + ".wem"]
    wem = work / (media_id + ".wem")
    wem.write_bytes(data[start:start + size])
    assert wem.read_bytes()[:4] == b"RIFF"
    target = output / ("RCast_" + str(i) + ".wav")
    temporary = work / (media_id + ".float.wav")
    subprocess.run([str(root / "TestResults/IreliaAudioTools/vgmstream/vgmstream-cli.exe"),
                    "-i", "-W", "4", "-o", str(temporary), str(wem)], check=True, capture_output=True)
    raw = temporary.read_bytes(); offset = 12; samples = None
    while offset + 8 <= len(raw):
        tag = raw[offset:offset + 4]; length = struct.unpack_from("<I", raw, offset + 4)[0]
        if tag == b"fmt ":
            fmt, channels, rate = struct.unpack_from("<HHI", raw, offset + 8)
            assert fmt == 3
        if tag == b"data":
            samples = array.array("f", raw[offset + 8:offset + 8 + length])
        offset += 8 + length + length % 2
    assert samples
    decoded.append((target, media_id, channels, rate, samples))
peak = max(abs(value) for _, _, _, _, samples in decoded for value in samples)
gain = min(1.0, .95 / peak)
manifest["pcm_conversion"] = {"float_peak": peak, "common_gain": gain}
for target, media_id, channels, rate, samples in decoded:
    pcm = array.array("h", (round(value * gain * 32767) for value in samples))
    with wave.open(str(target), "wb") as wav:
        wav.setnchannels(channels); wav.setsampwidth(2); wav.setframerate(rate); wav.writeframes(pcm.tobytes())
    with wave.open(str(target), "rb") as wav:
        assert wav.getsampwidth() == 2 and wav.getnframes() > 0
        entry = {"file": target.name, "media_id": int(media_id), "seconds": wav.getnframes() / wav.getframerate(),
                 "sample_rate": wav.getframerate(), "channels": wav.getnchannels(),
                 "sha256": hashlib.sha256(target.read_bytes()).hexdigest()}
    manifest["variants"].append(entry)
    meta = Path(str(target) + ".meta")
    if not meta.exists():
        template = (root / "Assets/Resources/Audio/Irelia/RCast_1.wav.meta").read_text(encoding="utf-8-sig")
        meta.write_text(re.sub(r"guid: [a-f0-9]+", "guid: " + uuid.uuid4().hex, template), encoding="utf-8")
(output / "Sources.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
for target in [output, output / "Sources.json"]:
    meta = Path(str(target) + ".meta")
    if not meta.exists():
        meta.write_text("fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\n" +
            ("folderAsset: yes\n" if target.is_dir() else "") + "DefaultImporter:\n  externalObjects: {}\n", encoding="utf-8")
print(json.dumps(manifest["variants"], indent=2))
