import { mkdir, readFile, writeFile, rename } from 'node:fs/promises';
import { createHash } from 'node:crypto';
const metadata = await (await fetch('https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json')).json();
const version = JSON.parse(await readFile('global.json', 'utf8')).sdk.version;
const release = metadata.releases.find(r => r.sdk?.version === version || r.sdks?.some(s => s.version === version));
if (!release) throw new Error(`Pinned SDK ${version} not present in official metadata`);
const sdk = release.sdk?.version === version ? release.sdk : release.sdks.find(s => s.version === version);
const file = sdk.files.find(f => f.rid === 'win-x64' && f.name.endsWith('.zip'));
if (!file?.url.startsWith('https://builds.dotnet.microsoft.com/')) throw new Error('Unexpected SDK download origin');
// Reuse a retained matching archive; a mismatch must be reviewed, not overwritten.
try {
  const retained = await readFile('.tools/dotnet-sdk.zip');
  const digest = createHash('sha512').update(retained).digest('hex');
  if (digest !== file.hash.toLowerCase()) throw new Error('Existing SDK archive SHA-512 mismatch; not overwritten');
  await writeFile('.tools/dotnet-download.json', JSON.stringify({ version, url: file.url, sha512: digest }, null, 2));
  console.log(`Reused SDK ${version} archive verified against official SHA-512`);
  process.exit(0);
} catch (error) {
  if (error.code !== 'ENOENT') throw error;
}
console.log(`Downloading .NET SDK ${version} (${file.url})`);
const response = await fetch(file.url);
if (!response.ok) throw new Error(`HTTP ${response.status}`);
const data = Buffer.from(await response.arrayBuffer());
const digest = createHash('sha512').update(data).digest('hex');
if (digest !== file.hash.toLowerCase()) throw new Error('SDK SHA-512 mismatch');
await mkdir('.tools/dotnet', { recursive: true });
await writeFile('.tools/dotnet-sdk.zip.partial', data, { flag: 'wx' });
await rename('.tools/dotnet-sdk.zip.partial', '.tools/dotnet-sdk.zip');
await writeFile('.tools/dotnet-download.json', JSON.stringify({ version, url: file.url, sha512: digest }, null, 2));
console.log(`Verified SDK ${version} SHA-512; archive ready for extraction`);
