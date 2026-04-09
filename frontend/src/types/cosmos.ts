/** Resposta da Bluesoft Cosmos (JSON com chaves em snake_case). */
export interface CosmosGtinProductDto {
  avg_price?: number
  max_price?: number
  min_price?: number
  brand?: { name?: string; picture?: string }
  description?: string
  gpc?: { code?: string; description?: string }
  gross_weight?: number
  gtin?: number
  height?: number
  length?: number
  ncm?: { code?: string; description?: string; full_description?: string }
  net_weight?: number
  price?: string
  thumbnail?: string
  width?: number
}
