'use strict';

/**
 * Encerra instâncias órfãs da API antes de `npm run dev`.
 * Evita bloqueio de ProductStore.Api.exe durante o build (MSB3026).
 *
 * Nota: se já houver outro `npm run dev` saudável, este passo mata a API
 * desse processo também — não deve haver dois dev servers ao mesmo tempo.
 */

const { execFileSync, spawnSync } = require('child_process');

function killWindows() {
  try {
    execFileSync(
      'taskkill',
      ['/IM', 'ProductStore.Api.exe', '/F', '/T'],
      { stdio: 'ignore' }
    );
  } catch {
    // 128 = processo não encontrado
  }
}

function killUnixPort() {
  spawnSync(
    'sh',
    [
      '-c',
      'P=$(lsof -ti:5127 2>/dev/null); [ -n "$P" ] && kill -9 $P 2>/dev/null; exit 0',
    ],
    { stdio: 'ignore' }
  );
}

if (process.platform === 'win32') {
  killWindows();
} else {
  killUnixPort();
}
