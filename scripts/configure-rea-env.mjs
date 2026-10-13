// Preserve unrelated TOML text while configuring only the existing REA server.
import { readFile, writeFile, rename, access } from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { parse } from '../.tools/rea/node_modules/smol-toml/dist/index.js';
const root = path.resolve(import.meta.dirname, '..');
const config = path.join(process.env.CODEX_HOME ?? path.join(os.homedir(), '.codex'), 'config.toml');
const text = await readFile(config, 'utf8');
const server = parse(text).mcp_servers?.rea;
if (!server) throw new Error('Run scoped REA setup first');
const required = { JAVA_HOME: process.env.JAVA_HOME,
  REA_JADX_MCP_JAR: path.join(root, '.tools/jadx/jadx-headless-mcp-0.7.1-all.jar') };
if (!required.JAVA_HOME) throw new Error('JAVA_HOME missing');
if (Object.entries(required).every(([k, v]) => server.env?.[k] === v)) {
  console.log('REA Android environment already configured');
} else {
  if (server.env && Object.keys(server.env).length) throw new Error('Existing REA env needs a reviewed merge; no changes made');
  if (Object.values(required).some(v => /['\r\n]/.test(v))) throw new Error('Unsupported configuration path characters');
  const block = '\n[mcp_servers.rea.env]\n' + Object.entries(required).map(([k,v]) => `${k} = '${v}'\n`).join('');
  const updated = text.trimEnd() + '\n' + block;
  parse(updated);
  const backup = config + '.iamp-env.backup';
  try { await access(backup); } catch { await writeFile(backup, text, { flag: 'wx' }); }
  const temporary = config + '.iamp-env.tmp';
  await writeFile(temporary, updated, { flag: 'wx' });
  await rename(temporary, config);
  console.log(`Configured REA Android environment; backup: ${backup}`);
}
