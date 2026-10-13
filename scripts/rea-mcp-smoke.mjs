import { Client } from '../.tools/rea/node_modules/@modelcontextprotocol/client/dist/index.mjs';
import { StdioClientTransport } from '../.tools/rea/node_modules/@modelcontextprotocol/client/dist/stdio.mjs';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
const root = path.resolve(import.meta.dirname, '..');
const output = path.join(root, 'analysis/generated');
await mkdir(output, { recursive: true });
const transport = new StdioClientTransport({
  command: process.execPath,
  args: [path.join(root, '.tools/rea/node_modules/rea-agents/scripts/rea.mjs'), 'mcp'],
  env: Object.fromEntries(Object.entries(process.env).filter(([, v]) => typeof v === 'string')),
  stderr: 'pipe'
});
const client = new Client({ name: 'iamp-setup-verification', version: '0.1.0' });
try {
  await client.connect(transport);
  const catalog = await client.listTools();
  await writeFile(path.join(output, 'rea-mcp-catalog.json'), JSON.stringify(catalog, null, 2));
  const tool = catalog.tools.find(t => t.name === 'inspect_android_package');
  if (!tool) throw new Error('Android inspection absent from connected MCP catalog');
  const result = await client.callTool({ name: tool.name, arguments: {
    path: path.join(root, 'apk/extracted/base.apk')
  } }, undefined, { timeout: 180000 });
  await writeFile(path.join(output, 'phone-base-mcp.json'), JSON.stringify(result, null, 2));
  if (result.isError) throw new Error('MCP Android analysis failed; inspect phone-base-mcp.json');
  console.log(`MCP smoke test passed: ${catalog.tools.length} tools; phone base APK inspected.`);
  console.log(`Flutter tools: ${catalog.tools.filter(t => /flutter|dart.*aot/.test(t.name)).map(t => t.name).join(', ') || 'none in this release'}`);
} finally {
  await client.close();
  await transport.close();
}
