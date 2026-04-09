import type { CosmosGtinProductDto } from './cosmos'

/** internal = código interno (000001…); cosmosGtin = GTIN na Bluesoft. */
export type SkuSource = 'internal' | 'cosmosGtin'

/** Colunas do SKU real persistidas / derivadas da Bluesoft Cosmos (camelCase na API). */
export interface RealSkuFromCosmos {
  commercialDescription?: string | null
  gtin?: string | null
  thumbnail?: string | null
  brandName?: string | null
  brandPicture?: string | null
  avgPrice?: number | null
  maxPrice?: number | null
  minPrice?: number | null
  priceLabel?: string | null
  ncmCode?: string | null
  ncmDescription?: string | null
  gpcCode?: string | null
  gpcDescription?: string | null
  grossWeightGrams?: number | null
  netWeightGrams?: number | null
  width?: number | null
  height?: number | null
  length?: number | null
}

export interface ProductResponse {

  id: string

  sku: string

  name: string

  description: string | null

  price: number

  /** Custo de aquisição (valor pago). */
  paidAmount: number

  stock: number

  categoryId: string

  category: string

  /** Metadados da Cosmos quando o produto foi criado/atualizado com GTIN real. */
  cosmosMetadata?: CosmosGtinProductDto | null

  /** Campos normalizados do produto real (preferir na UI em relação ao JSON bruto). */
  realSku?: RealSkuFromCosmos | null

}



export interface PagedProductsResponse {

  items: ProductResponse[]

  page: number

  pageSize: number

  totalCount: number

}



export interface ProductWritePayload {

  sku: string

  name: string

  description: string

  price: number

  paidAmount: number

  stock: number

  categoryId: string

  skuSource: SkuSource

}



export interface CategoryResponse {

  id: string

  name: string

}



export interface ProblemDetails {

  type?: string

  title?: string

  status?: number

  detail?: string

  errors?: Record<string, string[]>

}

