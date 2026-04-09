import { Package } from 'lucide-react'
import type { CosmosGtinProductDto } from '../types/cosmos'

const money = (n: number) =>
  n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

type Props = { dto: CosmosGtinProductDto }

/** Pré-visualização somente leitura dos dados retornados pela Bluesoft Cosmos (formulário). */
export function CosmosPreviewPanel({ dto }: Props) {
  const hasDim =
    (dto.length != null && dto.length > 0) ||
    (dto.width != null && dto.width > 0) ||
    (dto.height != null && dto.height > 0)

  return (
    <div className="detail-section" style={{ marginTop: 16 }}>
      <div className="detail-section-header">
        <Package size={14} />
        Dados do SKU real (Cosmos)
      </div>
      <div style={{ padding: '14px 20px' }}>
        {dto.thumbnail ? (
          <div style={{ marginBottom: 16 }}>
            <img
              src={dto.thumbnail}
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
          {dto.description ? (
            <>
              <dt>Descrição comercial</dt>
              <dd>{dto.description}</dd>
            </>
          ) : null}
          {dto.gtin != null ? (
            <>
              <dt>GTIN</dt>
              <dd>
                <code className="text-mono">{String(dto.gtin)}</code>
              </dd>
            </>
          ) : null}
          {dto.brand?.name || dto.brand?.picture ? (
            <>
              <dt>Marca</dt>
              <dd style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                {dto.brand?.picture ? (
                  <img
                    src={dto.brand.picture}
                    alt=""
                    style={{ maxHeight: 40, maxWidth: 120, objectFit: 'contain' }}
                  />
                ) : null}
                {dto.brand?.name ? <span>{dto.brand.name}</span> : null}
              </dd>
            </>
          ) : null}
          {dto.avg_price != null && Number.isFinite(dto.avg_price) ? (
            <>
              <dt>Preço médio</dt>
              <dd>{money(dto.avg_price)}</dd>
            </>
          ) : null}
          {dto.max_price != null && Number.isFinite(dto.max_price) ? (
            <>
              <dt>Preço máximo</dt>
              <dd>{money(dto.max_price)}</dd>
            </>
          ) : null}
          {dto.min_price != null && Number.isFinite(dto.min_price) ? (
            <>
              <dt>Preço mínimo</dt>
              <dd>{money(dto.min_price)}</dd>
            </>
          ) : null}
          {dto.price ? (
            <>
              <dt>Preço (rótulo)</dt>
              <dd>{dto.price}</dd>
            </>
          ) : null}
          {dto.ncm && (dto.ncm.code || dto.ncm.description) ? (
            <>
              <dt>NCM</dt>
              <dd>
                {dto.ncm.code ? <code className="text-mono">{dto.ncm.code}</code> : null}
                {dto.ncm.code && dto.ncm.description ? ' — ' : null}
                {dto.ncm.description}
              </dd>
            </>
          ) : null}
          {dto.gpc && (dto.gpc.code || dto.gpc.description) ? (
            <>
              <dt>GPC</dt>
              <dd>
                {dto.gpc.code ? <code className="text-mono">{dto.gpc.code}</code> : null}
                {dto.gpc.code && dto.gpc.description ? ' — ' : null}
                {dto.gpc.description}
              </dd>
            </>
          ) : null}
          {dto.net_weight != null && dto.net_weight > 0 ? (
            <>
              <dt>Peso líquido</dt>
              <dd>{dto.net_weight} g</dd>
            </>
          ) : null}
          {dto.gross_weight != null && dto.gross_weight > 0 ? (
            <>
              <dt>Peso bruto</dt>
              <dd>{dto.gross_weight} g</dd>
            </>
          ) : null}
          {hasDim ? (
            <>
              <dt>Dimensões (C × L × A)</dt>
              <dd>
                {dto.length ?? '—'} × {dto.width ?? '—'} × {dto.height ?? '—'}
                <div style={{ fontSize: '0.8rem', opacity: 0.75, marginTop: 4 }}>
                  Unidade conforme cadastro na Cosmos.
                </div>
              </dd>
            </>
          ) : null}
        </dl>
      </div>
    </div>
  )
}
