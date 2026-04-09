'use strict';

/**
 * Um único comando: restaura dotnet-ef, instala dependências (raiz + frontend),
 * cria .env a partir dos exemplos se ainda não existirem, e inicia API + Vite.
 */

const { execSync } = require('node:child_process');
const { existsSync, copyFileSync } = require('node:fs');
const { join } = require('node:path');

const root = join(__dirname, '..');
const shell = process.platform === 'win32';

function run(title, cmd, cwd = root) {
  console.log(`\n━━ ${title} ━━\n`);
  execSync(cmd, { stdio: 'inherit', cwd, shell });
}

console.log('\nProductStore — a preparar ambiente e a iniciar…\n');

run('dotnet tool restore', 'dotnet tool restore');
run('npm install (raiz)', 'npm install');
run('npm install (frontend)', 'npm install', join(root, 'frontend'));

const envRoot = join(root, '.env');
const envExample = join(root, '.env.example');
if (!existsSync(envRoot) && existsSync(envExample)) {
  copyFileSync(envExample, envRoot);
  console.log('\n✓ Criado .env a partir de .env.example (edite se precisar de Cosmos/Turnstile).\n');
}

const envFe = join(root, 'frontend', '.env');
const envFeEx = join(root, 'frontend', '.env.example');
if (!existsSync(envFe) && existsSync(envFeEx)) {
  copyFileSync(envFeEx, envFe);
  console.log('✓ Criado frontend/.env a partir de frontend/.env.example\n');
}

run('API + frontend (dev)', 'npm run dev', root);
