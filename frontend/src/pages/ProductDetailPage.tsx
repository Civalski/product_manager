import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  Pencil,
  ArrowLeft,
  Trash2,
  Info,
  Tag,
  DollarSign,
  Package,
  FileText,
  AlertCircle,
} from 'lucide-react'
import { deleteProduct, fetchProduct } from '../api/productsApi'
import { getApiErrorMessage } from '../lib/apiClient'
import type { CosmosGtinProductDto } from '../types/cosmos'

import type { ProductResponse, RealSkuFromCosmos } from '../types/product'

const money = (n: number) =>
  n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

function hasRealSkuData(r: RealSkuFromCosmos): boolean {
  return Object.values(r).some((v) => v !== null && v !== undefined && v !== '')
}

function RealSkuSection({ r }: { r: RealSkuFromCosmos }) {
  const hasDim =
    (r.length != null && r.length > 0) ||
    (r.width != null && r.width > 0) ||
    (r.height != null && r.height > 0)

  return (
    <div className="detail-section">
      <div className="detail-section-header">
        <Package size={14} />
        SKU real — Bluesoft Cosmos
      </div>
      <div style={{ padding: '14px 20px' }}>
        {r.thumbnail ? (
          <div style={{ marginBottom: 16 }}>
            <img
              src={r.thumbnail}
              alt=""
              style={{
                maxWidth: 160,
                maxHeight: 160,
                objectFit: 'contain',
                borderRadius: 8,
                border: '1px solid var(--border)',
              }}
            />
          </div>
        ) : null}
        <dl className="detail-grid">
          {r.commercialDescription ? (
            <>
              <dt>Descrição comercial</dt>
              <dd>{r.commercialDescription}</dd>
            </>
          ) : null}
          {r.gtin ? (
            <>
              <dt>GTIN</dt>
              <dd>
                <code className="text-mono">{r.gtin}</code>
              </dd>
            </>
          ) : null}
          {r.brandName || r.brandPicture ? (
            <>
              <dt>Marca</dt>
              <dd style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                {r.brandPicture ? (
                  <img
                    src={r.brandPicture}
                    alt=""
                    style={{
                      maxHeight: 40,
                      maxWidth: 120,
                      objectFit: 'contain',
                    }}
                  />
                ) : null}
                {r.brandName ? <span>{r.brandName}</span> : null}
              </dd>
            </>
          ) : null}
          {r.avgPrice != null && Number.isFinite(r.avgPrice) ? (
            <>
              <dt>Preço médio (mercado)</dt>
              <dd>{money(r.avgPrice)}</dd>
            </>
          ) : null}
          {r.maxPrice != null && Number.isFinite(r.maxPrice) ? (
            <>
              <dt>Preço máximo</dt>
              <dd>{money(r.maxPrice)}</dd>
            </>
          ) : null}
          {r.minPrice != null && Number.isFinite(r.minPrice) ? (
            <>
              <dt>Preço mínimo</dt>
              <dd>{money(r.minPrice)}</dd>
            </>
          ) : null}
          {r.priceLabel ? (
            <>
              <dt>Preço (rótulo)</dt>
              <dd>{r.priceLabel}</dd>
            </>
          ) : null}
          {r.ncmCode || r.ncmDescription ? (
            <>
              <dt>NCM</dt>
              <dd>
                {r.ncmCode ? <code className="text-mono">{r.ncmCode}</code> : null}
                {r.ncmCode && r.ncmDescription ? ' — ' : null}
                {r.ncmDescription}
              </dd>
            </>
          ) : null}
          {r.gpcCode || r.gpcDescription ? (
            <>
              <dt>GPC</dt>
              <dd>
                {r.gpcCode ? <code className="text-mono">{r.gpcCode}</code> : null}
                {r.gpcCode && r.gpcDescription ? ' — ' : null}
                {r.gpcDescription}
              </dd>
            </>
          ) : null}
          {r.netWeightGrams != null && r.netWeightGrams > 0 ? (
            <>
              <dt>Peso líquido</dt>
              <dd>{r.netWeightGrams} g</dd>
            </>
          ) : null}
          {r.grossWeightGrams != null && r.grossWeightGrams > 0 ? (
            <>
              <dt>Peso bruto</dt>
              <dd>{r.grossWeightGrams} g</dd>
            </>
          ) : null}
          {hasDim ? (
            <>
              <dt>Dimensões (C × L × A)</dt>
              <dd>
                {r.length ?? '—'} × {r.width ?? '—'} × {r.height ?? '—'}
                <div style={{ fontSize: '0.8rem', opacity: 0.75, marginTop: 4 }}>
                  Valores conforme retorno da API Cosmos (unidade do cadastro).
                </div>
              </dd>
            </>
          ) : null}
        </dl>
      </div>
    </div>
  )
}

function CosmosMetadataSection({ meta }: { meta: CosmosGtinProductDto }) {
  return (
    <div className="detail-section">
      <div className="detail-section-header">
        <Package size={14} />
        Dados Bluesoft Cosmos
      </div>
      <div style={{ padding: '14px 20px' }}>
        {meta.thumbnail ? (
          <div style={{ marginBottom: 16 }}>
            <img
              src={meta.thumbnail}
              alt=""
              style={{
                maxWidth: 160,
                maxHeight: 160,
                objectFit: 'contain',
                borderRadius: 8,
                border: '1px solid var(--border)',
              }}
            />
          </div>
        ) : null}
        <dl className="detail-grid">
          {meta.description ? (
            <>
              <dt>Descrição comercial</dt>
              <dd>{meta.description}</dd>
            </>
          ) : null}
          {meta.gtin != null && (
            <>
              <dt>GTIN</dt>
              <dd>
                <code className="text-mono">{String(meta.gtin)}</code>
              </dd>
            </>
          )}
          {meta.brand?.name || meta.brand?.picture ? (
            <>
              <dt>Marca</dt>
              <dd style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                {meta.brand?.picture ? (
                  <img
                    src={meta.brand.picture}
                    alt=""
                    style={{ maxHeight: 40, maxWidth: 120, objectFit: 'contain' }}
                  />
                ) : null}
                {meta.brand?.name ? <span>{meta.brand.name}</span> : null}
              </dd>
            </>
          ) : null}
          {meta.ncm && (meta.ncm.code || meta.ncm.description) ? (
            <>
              <dt>NCM</dt>
              <dd>
                {meta.ncm.code ? <code className="text-mono">{meta.ncm.code}</code> : null}
                {meta.ncm.code && meta.ncm.description ? ' — ' : null}
                {meta.ncm.description}
              </dd>
            </>
          ) : null}
          {meta.gpc && (meta.gpc.code || meta.gpc.description) ? (
            <>
              <dt>GPC</dt>
              <dd>
                {meta.gpc.code ? <code className="text-mono">{meta.gpc.code}</code> : null}
                {meta.gpc.code && meta.gpc.description ? ' — ' : null}
                {meta.gpc.description}
              </dd>
            </>
          ) : null}
          {meta.net_weight != null && meta.net_weight > 0 ? (
            <>
              <dt>Peso líquido</dt>
              <dd>{meta.net_weight} g</dd>
            </>
          ) : null}
          {meta.gross_weight != null && meta.gross_weight > 0 ? (
            <>
              <dt>Peso bruto</dt>
              <dd>{meta.gross_weight} g</dd>
            </>
          ) : null}
          {meta.avg_price != null ? (
            <>
              <dt>Preço médio (Cosmos)</dt>
              <dd>{money(meta.avg_price)}</dd>
            </>
          ) : null}
          {meta.max_price != null ? (
            <>
              <dt>Preço máximo</dt>
              <dd>{money(meta.max_price)}</dd>
            </>
          ) : null}
          {meta.min_price != null ? (
            <>
              <dt>Preço mínimo</dt>
              <dd>{money(meta.min_price)}</dd>
            </>
          ) : null}
          {meta.width != null || meta.height != null || meta.length != null ? (
            <>
              <dt>Dimensões (C × L × A)</dt>
              <dd>
                {meta.length ?? '—'} × {meta.width ?? '—'} × {meta.height ?? '—'}
              </dd>
            </>
          ) : null}
          {meta.price ? (
            <>
              <dt>Preço (rótulo Cosmos)</dt>
              <dd>{meta.price}</dd>
            </>
          ) : null}
        </dl>
      </div>
    </div>
  )
}

function StockBadge({ stock }: { stock: number }) {
  if (stock === 0) return <span className="badge badge-danger">Esgotado</span>
  if (stock <= 5) return <span className="badge badge-warning">{stock} unidades</span>
  return <span className="badge badge-success">{stock} unidades</span>
}

export function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [product, setProduct] = useState<ProductResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showDelete, setShowDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        const p = await fetchProduct(id)
        if (!cancelled) setProduct(p)
      } catch (e) {
        if (!cancelled) setError(getApiErrorMessage(e))
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  const handleDelete = async () => {
    if (!product) return
    setDeleting(true)
    try {
      await deleteProduct(product.id)
      navigate('/')
    } catch (e) {
      setError(getApiErrorMessage(e))
      setDeleting(false)
      setShowDelete(false)
    }
  }

  if (loading)
    return (
      <div className="loading-container">
        <div className="loading-text">
          <div className="spinner" />
          Carregando produto…
        </div>
      </div>
    )

  if (error)
    return (
      <div className="alert error">
        <AlertCircle />
        {error}
      </div>
    )

  if (!product) return null

  return (
    <div className="page">
      <div className="breadcrumb">
        <Link to="/">Produtos</Link>
        <span className="breadcrumb-separator">/</span>
        <span>{product.name}</span>
      </div>

      <header className="page-header">
        <div className="page-header-left">
          <h1>{product.name}</h1>
          <span className="badge badge-default">{product.category}</span>
        </div>
        <div className="header-actions">
          <Link className="btn" to={`/products/${product.id}/edit`}>
            <Pencil size={14} />
            Editar
          </Link>
          <button
            type="button"
            className="btn danger"
            onClick={() => setShowDelete(true)}
          >
            <Trash2 size={14} />
            Excluir
          </button>
          <Link className="btn ghost" to="/">
            <ArrowLeft size={14} />
            Voltar
          </Link>
        </div>
      </header>

      <div className="detail-section">
        <div className="detail-section-header">
          <Info size={14} />
          Informações Gerais
        </div>
        <dl className="detail-grid">
          <dt>
            <Tag size={13} style={{ marginRight: '6px', opacity: 0.5 }} />
            SKU
          </dt>
          <dd><code className="text-mono">{product.sku}</code></dd>
          <dt>
            <Package size={13} style={{ marginRight: '6px', opacity: 0.5 }} />
            Categoria
          </dt>
          <dd><span className="badge badge-accent">{product.category}</span></dd>
          <dt>
            <DollarSign size={13} style={{ marginRight: '6px', opacity: 0.5 }} />
            Preço
          </dt>
          <dd style={{ fontWeight: 600 }}>{money(product.price)}</dd>
          <dt>
            <Package size={13} style={{ marginRight: '6px', opacity: 0.5 }} />
            Estoque
          </dt>
          <dd><StockBadge stock={product.stock} /></dd>
        </dl>
      </div>

      {product.description && (
        <div className="detail-section">
          <div className="detail-section-header">
            <FileText size={14} />
            Descrição
          </div>
          <div style={{ padding: '14px 20px', fontSize: '0.88rem', lineHeight: 1.7, color: 'var(--text-secondary)' }}>
            {product.description}
          </div>
        </div>
      )}

      {product.realSku && hasRealSkuData(product.realSku) ? (
        <RealSkuSection r={product.realSku} />
      ) : product.cosmosMetadata ? (
        <CosmosMetadataSection meta={product.cosmosMetadata} />
      ) : null}

      {showDelete && (
        <div className="modal-overlay" onClick={() => !deleting && setShowDelete(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">Excluir Produto</div>
            <div className="modal-body">
              Tem certeza que deseja excluir <strong>{product.name}</strong> ({product.sku})?
              Esta ação não pode ser desfeita.
            </div>
            <div className="modal-footer">
              <button
                type="button"
                className="btn"
                onClick={() => setShowDelete(false)}
                disabled={deleting}
              >
                Cancelar
              </button>
              <button
                type="button"
                className="btn danger"
                onClick={handleDelete}
                disabled={deleting}
              >
                {deleting ? 'Excluindo…' : 'Excluir'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
