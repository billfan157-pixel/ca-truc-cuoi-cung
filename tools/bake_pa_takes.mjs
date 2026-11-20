import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const [modulePath, outputDir] = process.argv.slice(2);
if (!modulePath || !outputDir) {
    throw new Error('Usage: node tools/bake_pa_takes.mjs <espeak-ng.js 1.0.2> <output-directory>');
}

const { default: createSynth } = await import(pathToFileURL(resolve(modulePath)).href);
const dialogue = new URL('../Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json', import.meta.url);
const { entries } = JSON.parse(await readFile(dialogue, 'utf8'));
await mkdir(outputDir, { recursive: true });

for (const [id, speed] of [['CH01-PA01', 147], ['CH01-PA02', 138], ['CH01-PA03', 154], ['CH01-PA04', 145]]) {
    const entry = entries.find(item => item.id === id && item.chapter === 'CH01');
    const copy = entry?.copy.match(/“([^”]+)”/u)?.[1];
    if (!copy) throw new Error(`Missing canonical copy for ${id}`);
    const text = copy.replace(/\b101\b/u, 'một linh một').replaceAll('…', ',');
    const wav = `${id}.wav`;
    const synth = await createSynth({
        thisProgram: 'espeak-ng',
        arguments: ['-b', '1', '-v', 'vi', '-s', String(speed), '-p', '43', '-w', wav, text],
    });
    await writeFile(resolve(outputDir, wav), synth.FS.readFile(wav));
    process.stdout.write(`${id}\n`);
}
