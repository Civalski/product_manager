import type { CosmosGtinProductDto } from '../types/cosmos'
import type { ProductResponse, RealSkuFromCosmos } from '../types/product'

/** Produto vinculado a dados Bluesoft Cosmos (GTIN real). */
export function isCosmosBackedProduct(p: ProductResponse): boolean {
  if (p.cosmosMetadata != null && typeof p.cosmosMetadata === 'object') return true
  const r = p.realSku
  if (!r) return false
  return Boolean(
    (r.gtin != null && String(r.gtin).length > 0) ||
      (r.commercialDescription != null && r.commercialDescription.length > 0) ||
      (r.thumbnail != null && r.thumbnail.length > 0) ||
      (r.brandName != null && r.brandName.length > 0),
  )
}

export function hasCosmosPreviewData(d: CosmosGtinProductDto | null | undefined): boolean {
  if (!d) return false
  return Boolean(
    d.description ||
      d.gtin != null ||
      d.thumbnail ||
      d.brand?.name ||
      d.brand?.picture ||
      d.avg_price != null ||
      d.max_price != null ||
      d.min_price != null ||
      d.price ||
      d.ncm?.code ||
      d.ncm?.description ||
      d.gpc?.code ||
      d.gpc?.description ||
      (d.net_weight != null && d.net_weight > 0) ||
      (d.gross_weight != null && d.gross_weight > 0) ||
      (d.width != null && d.width > 0) ||
      (d.height != null && d.height > 0) ||
      (d.length != null && d.length > 0),
  )
}

function realSkuToCosmosDto(r: RealSkuFromCosmos): CosmosGtinProductDto | null {
  const gtinStr = r.gtin != null ? String(r.gtin).replace(/\D/g, '') : ''
  const gtinNum =
    gtinStr.length >= 8 && gtinStr.length <= 14 ? Number(gtinStr) : undefined

  return {
    description: r.commercialDescription ?? undefined,
    gtin: gtinNum,
    thumbnail: r.thumbnail ?? undefined,
    brand:
      r.brandName || r.brandPicture
        ? { name: r.brandName ?? undefined, picture: r.brandPicture ?? undefined }
        : undefined,
    avg_price: r.avgPrice ?? undefined,
    max_price: r.maxPrice ?? undefined,
    min_price: r.minPrice ?? undefined,
    price: r.priceLabel ?? undefined,
    ncm:
      r.ncmCode || r.ncmDescription
        ? { code: r.ncmCode ?? undefined, description: r.ncmDescription ?? undefined }
        : undefined,
    gpc:
      r.gpcCode || r.gpcDescription
        ? { code: r.gpcCode ?? undefined, description: r.gpcDescription ?? undefined }
        : undefined,
    gross_weight: r.grossWeightGrams ?? undefined,
    net_weight: r.netWeightGrams ?? undefined,
    width: r.width ?? undefined,
    height: r.height ?? undefined,
    length: r.length ?? undefined,
  }
}

/** Monta DTO de preview a partir da resposta do produto (realSku ou JSON bruto). */
export function cosmosDtoFromProductResponse(p: ProductResponse): CosmosGtinProductDto | null {
  if (p.realSku) {
    const fromReal = realSkuToCosmosDto(p.realSku)
    if (fromReal && hasCosmosPreviewData(fromReal)) return fromReal
  }
  if (p.cosmosMetadata != null && typeof p.cosmosMetadata === 'object') {
    return p.cosmosMetadata as CosmosGtinProductDto
  }
  return null
}

/** Texto auxiliar para o campo descrição (alinhado ao resumo gravado no backend). */
type CosmosMetaLoose = {
  thumbnail?: string
  brand?: { name?: string; picture?: string }
}

/** Miniatura a partir de realSku ou do JSON bruto (produtos antigos só com cosmosMetadata). */
export function cosmosThumbnailFromProduct(p: ProductResponse): string | undefined {
  const t = p.realSku?.thumbnail
  if (t) return t
  const m = p.cosmosMetadata as CosmosMetaLoose | null | undefined
  return m?.thumbnail
}

export function cosmosBrandNameFromProduct(p: ProductResponse): string | undefined {
  const n = p.realSku?.brandName
  if (n) return n
  const m = p.cosmosMetadata as CosmosMetaLoose | null | undefined
  return m?.brand?.name
}

export function buildCosmosDescriptionDraft(dto: CosmosGtinProductDto): string {
  const lines: string[] = []
  if (dto.brand?.name?.trim()) lines.push(`Marca: ${dto.brand.name.trim()}`)
  if (dto.ncm && (dto.ncm.code || dto.ncm.description)) {
    const ncmLine = `NCM: ${dto.ncm.code ?? ''} — ${dto.ncm.description ?? ''}`.trim()
    lines.push(ncmLine.replace(/\s+—\s*$/, '').trim())
  }
  if (dto.gpc && (dto.gpc.code || dto.gpc.description)) {
    const gpcLine = `GPC: ${dto.gpc.code ?? ''} — ${dto.gpc.description ?? ''}`.trim()
    lines.push(gpcLine.replace(/\s+—\s*$/, '').trim())
  }
  if (dto.net_weight != null && dto.net_weight > 0)
    lines.push(`Peso líquido: ${dto.net_weight} g`)
  if (dto.gross_weight != null && dto.gross_weight > 0)
    lines.push(`Peso bruto: ${dto.gross_weight} g`)
  if (
    (dto.length != null && dto.length > 0) ||
    (dto.width != null && dto.width > 0) ||
    (dto.height != null && dto.height > 0)
  )
    lines.push(
      `Dimensões (L×A×C): ${dto.length ?? 0} × ${dto.width ?? 0} × ${dto.height ?? 0}`.trim(),
    )
  if (dto.price?.trim()) lines.push(`Preço referência (Cosmos): ${dto.price.trim()}`)
  if (dto.thumbnail?.trim()) lines.push(`Miniatura: ${dto.thumbnail.trim()}`)
  return lines.filter(Boolean).join('\n')
}
