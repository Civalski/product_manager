import { apiJson } from '../lib/apiClient'

import type {

  PagedProductsResponse,

  ProductResponse,

  ProductWritePayload,

} from '../types/product'



export interface ProductListParams {

  page?: number

  pageSize?: number

  /** Busca em nome, SKU e descrição (API ignora maiúsculas) */

  search?: string

  sku?: string

  name?: string

  categoryId?: string

  minPrice?: number

  maxPrice?: number

  /** available | out | low */

  stockFilter?: string

}



function buildQuery(params: ProductListParams): string {

  const q = new URLSearchParams()

  if (params.page != null) q.set('page', String(params.page))

  if (params.pageSize != null) q.set('pageSize', String(params.pageSize))

  if (params.search) q.set('search', params.search)

  if (params.sku) q.set('sku', params.sku)

  if (params.name) q.set('name', params.name)

  if (params.categoryId) q.set('categoryId', params.categoryId)

  if (params.minPrice != null) q.set('minPrice', String(params.minPrice))

  if (params.maxPrice != null) q.set('maxPrice', String(params.maxPrice))

  if (params.stockFilter) q.set('stockFilter', params.stockFilter)

  const s = q.toString()

  return s ? `?${s}` : ''

}



export async function fetchProducts(

  params: ProductListParams,

): Promise<PagedProductsResponse> {

  const res = await apiJson<PagedProductsResponse>(`/api/products${buildQuery(params)}`)

  return res as PagedProductsResponse

}

const LIST_PAGE_SIZE = 100

/** Agrega todos os SKUs cadastrados (paginação na API, máx. 100 por página). */
export async function fetchAllExistingSkus(): Promise<Set<string>> {
  const skus = new Set<string>()
  let page = 1
  for (;;) {
    const res = await fetchProducts({ page, pageSize: LIST_PAGE_SIZE })
    for (const p of res.items) {
      skus.add(p.sku.trim())
    }
    if (res.items.length < LIST_PAGE_SIZE || page * LIST_PAGE_SIZE >= res.totalCount) break
    page += 1
  }
  return skus
}

export async function fetchProduct(id: string): Promise<ProductResponse> {

  return apiJson<ProductResponse>(`/api/products/${id}`) as Promise<ProductResponse>

}



export async function createProduct(

  payload: ProductWritePayload,

): Promise<ProductResponse> {

  return apiJson<ProductResponse>('/api/products', {

    method: 'POST',

    body: JSON.stringify(payload),

  }) as Promise<ProductResponse>

}



export async function updateProduct(

  id: string,

  payload: ProductWritePayload,

): Promise<ProductResponse> {

  return apiJson<ProductResponse>(`/api/products/${id}`, {

    method: 'PUT',

    body: JSON.stringify(payload),

  }) as Promise<ProductResponse>

}



export async function deleteProduct(id: string): Promise<void> {

  await apiJson(`/api/products/${id}`, { method: 'DELETE' })

}



export interface ProductExportResult {

  fileName: string

  productCount: number

  exportedAtUtc: string

  json: string

}



export async function exportProductsToJson(): Promise<ProductExportResult> {

  return apiJson<ProductExportResult>('/api/products/export', {

    method: 'POST',

  }) as Promise<ProductExportResult>

}

