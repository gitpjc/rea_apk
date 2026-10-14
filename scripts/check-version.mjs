// Offline release consistency check; no third-party packages or device access.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const semver = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$/;
try {
  if (process.argv.length > 3) throw new Error('Usage: node scripts/check-version.mjs [publish-folder]');
  const props = readFileSync('Directory.Build.props', 'utf8');
  const versions = [...props.matchAll(/<Version>([^<]+)<\/Version>/g)];
  if (versions.length !== 1 || !semver.test(versions[0][1]))
    throw new Error('Directory.Build.props must define one semantic Version.');
  const version = versions[0][1];
  const source = readFileSync('assets/version_history.json', 'utf8');
  const entries = JSON.parse(source);
  if (!Array.isArray(entries) || entries.length === 0 || entries[0]?.version !== version)
    throw new Error('Version history must start with the current build version.');
  const seen = new Set();
  for (const entry of entries) {
    if (!entry || typeof entry.version !== 'string' || !semver.test(entry.version) || seen.has(entry.version))
      throw new Error('History versions must be valid and unique.');
    seen.add(entry.version);
    if (typeof entry.changedAt !== 'string' || !/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:\d{2})$/.test(entry.changedAt) || !Number.isFinite(Date.parse(entry.changedAt)))
      throw new Error(`Invalid date with time zone for ${entry.version}.`);
    for (const field of ['summary', 'details'])
      if (typeof entry[field] !== 'string' || !entry[field].trim())
        throw new Error(`Missing ${field} for ${entry.version}.`);
  }
  if (process.argv[2]) {
    const folder = resolve(process.argv[2]);
    if (readFileSync(resolve(folder, 'assets/version_history.json'), 'utf8') !== source)
      throw new Error('Published history differs from the source history.');
    // deps.json records the version compiled into the shipped application.
    const deps = JSON.parse(readFileSync(resolve(folder, 'Iamp.Desktop.deps.json'), 'utf8'));
    if (!deps.libraries?.[`Iamp.Desktop/${version}`])
      throw new Error('Published application version differs from the source version.');
  }
  console.log(`Version ${version}: ${entries.length} history entries verified${process.argv[2] ? ', published application and history match' : ''}.`);
} catch (error) {
  console.error(`Version check failed: ${error.message}`);
  process.exitCode = 1;
}
