import { apiJson } from '../lib/apiClient'

import type { CosmosGtinProductDto } from '../types/cosmos'

/** Consulta GTIN na API Bluesoft (preview no formulário). Exige Cosmos__Token no backend. */
export async function fetchCosmosGtin(gtin: string): Promise<CosmosGtinProductDto> {
  const digits = gtin.replace(/\D/g, '')
  return apiJson<CosmosGtinProductDto>(
    `/api/cosmos/gtins/${encodeURIComponent(digits)}`,
  ) as Promise<CosmosGtinProductDto>
}
