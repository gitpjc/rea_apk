// Bounded read-only resource catalog investigation; no app or device execution.
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir, readdir } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
const root = path.resolve(import.meta.dirname, '..');
const out = path.join(root, 'analysis/generated/catalog');
const jar = path.join(process.env.JAVA_HOME ?? 'D:/JAVA_STUFF/OpenJDK/jdk-21.0.1', 'bin/jar.exe');
const sha = data => createHash('sha256').update(data).digest('hex');
await mkdir(out, { recursive: true });
const results = [];
for (const [label, relative] of [['primary-code42', 'apk/MOOER_iAMP_V1.6.1_2026.06.26.apk'], ['phone-code43', 'apk/extracted/base.apk']]) {
  const apk = path.join(root, relative);
  const entries = execFileSync(jar, ['tf', apk], { encoding: 'utf8', maxBuffer: 8 * 1024 * 1024 }).split(/\r?\n/).filter(Boolean);
  const assets = entries.filter(name => name.startsWith('assets/flutter_assets/') && !name.endsWith('/'));
  const nonMedia = assets.filter(name => !/\.(png|jpe?g|gif|ttf|otf|webp|svg|mp3|wav|m4a|mp4|frag)$/i.test(name));
  const selected = nonMedia.filter(name => /\/(?:AssetManifest\.bin|FontManifest\.json|NativeAssetsManifest\.json)$/.test(name));
  const folder = path.join(out, label);
  await mkdir(folder, { recursive: true });
  execFileSync(jar, ['xf', apk, ...selected], { cwd: folder });
  const metadata = [];
  for (const name of selected) {
    const data = await readFile(path.join(folder, name));
    metadata.push({ member: name, bytes: data.length, sha256: sha(data),
      json: name.endsWith('.json') ? JSON.parse(data.toString('utf8')) : undefined,
      limitation: name.endsWith('.bin') ? 'Flutter binary asset manifest; not decoded as a parameter schema' : undefined });
  }
  const counts = {};
  for (const name of assets) { const extension = path.extname(name).toLowerCase() || '(none)'; counts[extension] = (counts[extension] ?? 0) + 1; }
  results.push({ label, apk: relative, apk_sha256: sha(await readFile(apk)), total_entries: entries.length, flutter_asset_count: assets.length,
    extension_counts: counts, non_media_members: nonMedia, extracted_metadata: metadata,
    effect_image_members: assets.filter(name => /\/(?:f40_fx_(?:amp|cab|delay|dyna|eq|filter|mod|od|reverb)|.*(?:param|model|preset).*?)\.[A-Za-z0-9]+$/i.test(name)) });
}
const vendor = 'C:/Program Files (x86)/Mooer Audio Corp/Mooer Studio For Future Series';
async function walk(folder) {
  const files = [];
  for (const entry of await readdir(folder, { withFileTypes: true })) {
    const full = path.join(folder, entry.name);
    if (entry.isDirectory()) files.push(...await walk(full));
    else if (entry.isFile()) files.push(full);
  }
  return files;
}
const vendorFiles = await walk(vendor);
const vendorResources = [];
for (const file of vendorFiles.filter(file => /\.(xml|json|csv|txt|ya?ml|db|sqlite)$/i.test(file))) {
  const relative = path.relative(vendor, file).replaceAll('\\', '/');
  const data = await readFile(file);
  const destination = path.join(out, 'vendor', relative);
  await mkdir(path.dirname(destination), { recursive: true });
  await writeFile(destination, data);
  vendorResources.push({ path: relative, bytes: data.length, sha256: sha(data), content: data.toString('utf8') });
}
const report = { method: 'Java jar entry listing and explicitly selected extraction; Node standard-library hashes and JSON/XML inventory',
  limitations: ['No cloud/account/network requests, app/device execution or hardware writes.', 'Search covers packaged Flutter asset members and vendor external XML/JSON/CSV/text/YAML/database filename inventory.', 'Not a Dart AOT/firmware/compiled Qt resource decompilation; static filenames are not wire IDs.', 'No new Python interpreter installed or used for this investigation.'],
  apks: results, vendor_directory: vendor, vendor_file_count: vendorFiles.length, vendor_text_resources: vendorResources };
await writeFile(path.join(out, 'inventory.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ apks: results.map(item => ({ label: item.label, entries: item.total_entries, flutter_assets: item.flutter_asset_count,
  extensions: item.extension_counts, non_media: item.non_media_members, metadata: item.extracted_metadata })), vendor_text_resources: vendorResources }, null, 2));
