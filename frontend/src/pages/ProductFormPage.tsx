import { useEffect, useMemo, useRef, useState } from 'react'

import { Link, useNavigate, useParams } from 'react-router-dom'

import { ArrowLeft, Save, AlertCircle, Package, Search } from 'lucide-react'

import { fetchCosmosGtin } from '../api/cosmosApi'

import { createCategory, fetchCategories } from '../api/categoriesApi'

import {
  createProduct,
  fetchAllExistingSkus,
  fetchProduct,
  updateProduct,
} from '../api/productsApi'

import { CosmosPreviewPanel } from '../components/CosmosPreviewPanel'
import { getApiErrorMessage } from '../lib/apiClient'
import {
  buildCosmosDescriptionDraft,
  cosmosDtoFromProductResponse,
  hasCosmosPreviewData,
  isCosmosBackedProduct,
} from '../lib/productCosmos'

import type { CosmosGtinProductDto } from '../types/cosmos'
import type { CategoryResponse, ProductWritePayload } from '../types/product'

const empty: ProductWritePayload = {
  sku: '',
  name: '',
  description: '',
  price: 0,
  stock: 1,
  categoryId: '',
  skuSource: 'internal',
}

/** Alinha com o backend: mesma categoria independente de maiúsculas e acentos. */
function normalizeName(s: string) {
  return s
    .trim()
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
}

function gtinDigitCount(s: string) {
  const digits = s.replace(/\D/g, '')
  return digits.length
}

/** SKUs internos: exatamente 6 dígitos (000001 … 999999), únicos no banco + reservados da sessão. */
function pickNextInternalSku(existing: ReadonlySet<string>, reserved: Set<string>): string {
  const used = new Set<string>([...existing, ...reserved])
  let max = 0
  for (const s of used) {
    if (/^\d{6}$/.test(s)) {
      const n = parseInt(s, 10)
      if (!Number.isNaN(n)) max = Math.max(max, n)
    }
  }
  for (let n = max + 1; n <= 999_999; n++) {
    const code = String(n).padStart(6, '0')
    if (!used.has(code)) {
      reserved.add(code)
      return code
    }
  }
  throw new Error('Limite de códigos internos (6 dígitos) atingido.')
}

export function ProductFormPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isEdit = Boolean(id)

  const [form, setForm] = useState<ProductWritePayload>(empty)
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [categoryInput, setCategoryInput] = useState('')
  const [categorySuggestionsOpen, setCategorySuggestionsOpen] = useState(false)
  const [loading, setLoading] = useState(isEdit)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [cosmosLoading, setCosmosLoading] = useState(false)
  const [cosmosPreview, setCosmosPreview] = useState<CosmosGtinProductDto | null>(null)

  const existingSkusRef = useRef<Set<string> | null>(null)
  const reservedInternalSkusRef = useRef<Set<string>>(new Set())

  const ensureExistingSkus = async (): Promise<Set<string>> => {
    if (existingSkusRef.current == null) {
      try {
        existingSkusRef.current = await fetchAllExistingSkus()
      } catch {
        existingSkusRef.current = new Set()
      }
    }
    return existingSkusRef.current
  }

  useEffect(() => {
    reservedInternalSkusRef.current = new Set()
    existingSkusRef.current = null
    setCosmosPreview(null)
  }, [id])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const list = await fetchCategories()
        if (!cancelled) setCategories(list)
      } catch {
        if (!cancelled) setCategories([])
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!id) return
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        const p = await fetchProduct(id)
        if (cancelled) return
        setForm({
          sku: p.sku,
          name: p.name,
          description: p.description ?? '',
          price: p.price,
          stock: p.stock,
          categoryId: p.categoryId,
          skuSource: isCosmosBackedProduct(p) ? 'cosmosGtin' : 'internal',
        })
        setCategoryInput(p.category)
        setCosmosPreview(cosmosDtoFromProductResponse(p))
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

  useEffect(() => {
    if (isEdit) return
    let cancelled = false
    ;(async () => {
      try {
        const existing = await fetchAllExistingSkus()
        if (cancelled) return
        existingSkusRef.current = existing
        setForm((f) => {
          if (f.sku !== '' || f.skuSource !== 'internal') return f
          const sku = pickNextInternalSku(existing, reservedInternalSkusRef.current)
          return { ...f, sku }
        })
      } catch {
        if (cancelled) return
        setForm((f) => {
          if (f.sku !== '' || f.skuSource !== 'internal') return f
          const sku = pickNextInternalSku(new Set(), reservedInternalSkusRef.current)
          return { ...f, sku }
        })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [isEdit])

  const filteredCategories = useMemo(() => {
    const q = normalizeName(categoryInput)
    if (!q) return []
    return categories
      .filter((c) => normalizeName(c.name).startsWith(q))
      .slice(0, 3)
  }, [categories, categoryInput])

  const onChange =
    (field: keyof ProductWritePayload) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
      const v = e.target.value
      if (field === 'price') setForm((f) => ({ ...f, price: v === '' ? 0 : Number(v) }))
      else if (field === 'stock')
        setForm((f) => ({ ...f, stock: v === '' ? 0 : Number.parseInt(v, 10) }))
      else if (field === 'categoryId') setForm((f) => ({ ...f, categoryId: v }))
      else if (field === 'skuSource') return
      else setForm((f) => ({ ...f, [field]: v }))
    }

  const pickCategory = (c: CategoryResponse) => {
    setCategoryInput(c.name)
    setForm((f) => ({ ...f, categoryId: c.id }))
    setCategorySuggestionsOpen(false)
  }

  const onCategoryInputChange = (value: string) => {
    setCategoryInput(value)
    const match = categories.find((c) => normalizeName(c.name) === normalizeName(value))
    setForm((f) => ({ ...f, categoryId: match?.id ?? '' }))
  }

  const applyCosmosPreview = (dto: Awaited<ReturnType<typeof fetchCosmosGtin>>) => {
    setCosmosPreview(dto)
    setForm((f) => {
      const gtinStr = dto.gtin != null ? String(dto.gtin) : f.sku.replace(/\D/g, '')
      const draftDesc = buildCosmosDescriptionDraft(dto)
      return {
        ...f,
        sku: gtinStr,
        name: (dto.description?.trim() || f.name).trim(),
        price:
          dto.avg_price != null && Number.isFinite(dto.avg_price) && dto.avg_price > 0
            ? dto.avg_price
            : f.price,
        description: f.description.trim() ? f.description : draftDesc,
      }
    })
  }

  const handleCosmosLookup = async () => {
    setError(null)
    const n = gtinDigitCount(form.sku)
    if (n < 8 || n > 14) {
      setError('Digite um código de barras com 8 a 14 dígitos antes de consultar.')
      return
    }
    setCosmosLoading(true)
    try {
      const dto = await fetchCosmosGtin(form.sku)
      applyCosmosPreview(dto)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setCosmosLoading(false)
    }
  }

  const selectSkuSource = (src: 'internal' | 'cosmosGtin') => {
    if (src === 'cosmosGtin') {
      reservedInternalSkusRef.current.clear()
      setCosmosPreview(null)
      setForm((f) =>
        f.skuSource === 'cosmosGtin' ? f : { ...f, skuSource: 'cosmosGtin', sku: '' },
      )
      return
    }
    void (async () => {
      reservedInternalSkusRef.current.clear()
      setCosmosPreview(null)
      const existing = await ensureExistingSkus()
      const sku = pickNextInternalSku(existing, reservedInternalSkusRef.current)
      setForm((f) => ({ ...f, skuSource: 'internal', sku }))
    })()
  }

  const generateInternalSku = () => {
    void (async () => {
      const existing = await ensureExistingSkus()
      const sku = pickNextInternalSku(existing, reservedInternalSkusRef.current)
      setForm((f) => ({ ...f, sku, skuSource: 'internal' }))
    })()
  }

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError(null)

    let categoryId = form.categoryId
    const typed = categoryInput.trim()

    try {
      if (!categoryId && typed) {
        const match = categories.find((c) => normalizeName(c.name) === normalizeName(typed))
        if (match) {
          categoryId = match.id
        } else {
          const created = await createCategory(typed)
          categoryId = created.id
          setCategories((prev) => [...prev, created].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR')))
        }
      }

      if (!categoryId) {
        setError('Informe uma categoria (digite um nome novo ou escolha uma existente na lista).')
        setSaving(false)
        return
      }

      const payload: ProductWritePayload = {
        ...form,
        categoryId,
        description: form.description.trim() ? form.description : '',
        skuSource: form.skuSource,
      }

      if (isEdit && id) {
        await updateProduct(id, payload)
        navigate(`/products/${id}`)
      } else {
        const created = await createProduct(payload)
        navigate(`/products/${created.id}`)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  if (loading)
    return (
      <div className="loading-container">
        <div className="loading-text">
          <div className="spinner" />
          Carregando dados…
        </div>
      </div>
    )

  return (
    <div className="page">
      <div className="breadcrumb">
        <Link to="/">Produtos</Link>
        <span className="breadcrumb-separator">/</span>
        {isEdit && id ? (
          <>
            <Link to={`/products/${id}`}>{form.name || 'Produto'}</Link>
            <span className="breadcrumb-separator">/</span>
            <span>Editar</span>
          </>
        ) : (
          <span>Novo Produto</span>
        )}
      </div>

      <header className="page-header">
        <div className="page-header-left">
          <h1>{isEdit ? 'Editar Produto' : 'Novo Produto'}</h1>
        </div>
        <div className="header-actions">
          <Link className="btn ghost" to={isEdit && id ? `/products/${id}` : '/'}>
            <ArrowLeft size={14} />
            Voltar
          </Link>
        </div>
      </header>

      {error && (
        <div className="alert error">
          <AlertCircle />
          <div style={{ whiteSpace: 'pre-wrap' }}>{error}</div>
        </div>
      )}

      <form onSubmit={onSubmit}>
        <div className="detail-section">
          <div className="detail-section-header">
            <Package size={14} />
            Informações do Produto
          </div>
          <div style={{ padding: '20px' }}>
            <div className="form-grid">
              <div className="full">
                <span
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    color: 'var(--text-secondary)',
                    display: 'block',
                    marginBottom: 8,
                  }}
                >
                  Código do produto (SKU){' '}
                  <span style={{ color: 'var(--danger)', fontSize: '0.7rem' }}>*</span>
                </span>
                <div
                  className="sku-source-toggle"
                  role="radiogroup"
                  aria-label="Como informar o código"
                >
                  <label className="sku-source-option">
                    <input
                      type="radio"
                      name="skuSource"
                      checked={form.skuSource === 'internal'}
                      onChange={() => selectSkuSource('internal')}
                    />
                    <span>Interno</span>
                  </label>
                  <label className="sku-source-option">
                    <input
                      type="radio"
                      name="skuSource"
                      checked={form.skuSource === 'cosmosGtin'}
                      onChange={() => selectSkuSource('cosmosGtin')}
                    />
                    <span>SKU Real</span>
                  </label>
                </div>
                <div className="sku-field-row">
                  <input
                    name="sku"
                    value={form.sku}
                    onChange={onChange('sku')}
                    required
                    autoComplete="off"
                    placeholder={
                      form.skuSource === 'cosmosGtin'
                        ? 'Digite o GTIN (8 a 14 dígitos)'
                        : '000001'
                    }
                  />
                  {form.skuSource === 'internal' && (
                    <button
                      type="button"
                      className="btn ghost sku-action"
                      onClick={generateInternalSku}
                    >
                      Gerar
                    </button>
                  )}
                  {form.skuSource === 'cosmosGtin' && (
                    <button
                      type="button"
                      className="btn sku-action"
                      disabled={cosmosLoading}
                      onClick={() => void handleCosmosLookup()}
                    >
                      <Search size={14} />
                      {cosmosLoading ? 'Consultando…' : 'Consultar'}
                    </button>
                  )}
                </div>
                {form.skuSource === 'cosmosGtin' ? (
                  cosmosPreview && hasCosmosPreviewData(cosmosPreview) ? (
                    <CosmosPreviewPanel dto={cosmosPreview} />
                  ) : (
                    <p
                      style={{
                        marginTop: 14,
                        fontSize: '0.85rem',
                        color: 'var(--text-secondary)',
                        lineHeight: 1.5,
                      }}
                    >
                      Use <strong>Consultar</strong> para carregar na Cosmos marca, preços de mercado, NCM,
                      GPC, pesos e dimensões. Os dados aparecem aqui antes de salvar.
                    </p>
                  )
                ) : null}
              </div>

              <label>
                Nome <span style={{ color: 'var(--danger)', fontSize: '0.7rem' }}>*</span>
                <input
                  name="name"
                  value={form.name}
                  onChange={onChange('name')}
                  required
                  placeholder="Nome do produto"
                />
              </label>

              <label className="category-combobox-label">
                Categoria <span style={{ color: 'var(--danger)', fontSize: '0.7rem' }}>*</span>
                <div className="category-combobox">
                  <input
                    type="text"
                    name="categoryName"
                    value={categoryInput}
                    onChange={(e) => onCategoryInputChange(e.target.value)}
                    onFocus={() => setCategorySuggestionsOpen(true)}
                    onBlur={() => {
                      window.setTimeout(() => setCategorySuggestionsOpen(false), 180)
                    }}
                    autoComplete="off"
                    placeholder="Digite ou escolha uma categoria"
                    aria-autocomplete="list"
                    aria-expanded={categorySuggestionsOpen}
                  />
                  {categorySuggestionsOpen && filteredCategories.length > 0 && (
                    <ul className="category-suggestions" role="listbox">
                      {filteredCategories.map((c) => (
                        <li
                          key={c.id}
                          role="option"
                          className="category-suggestion"
                          onMouseDown={(ev) => {
                            ev.preventDefault()
                            pickCategory(c)
                          }}
                        >
                          {c.name}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
                <span className="category-combobox-hint">
                  Nomes novos são criados ao salvar o produto.
                </span>
              </label>

              <label>
                Preço (R$) <span style={{ color: 'var(--danger)', fontSize: '0.7rem' }}>*</span>
                <input
                  name="price"
                  type="number"
                  step="0.01"
                  min="0"
                  value={form.price}
                  onChange={onChange('price')}
                  required
                  placeholder="0.00"
                />
              </label>

              <label>
                Estoque <span style={{ color: 'var(--danger)', fontSize: '0.7rem' }}>*</span>
                <input
                  name="stock"
                  type="number"
                  min="1"
                  value={form.stock}
                  onChange={onChange('stock')}
                  required
                  placeholder="1"
                />
              </label>

              <label className="full">
                Descrição
                <textarea
                  name="description"
                  rows={4}
                  value={form.description}
                  onChange={onChange('description')}
                  placeholder="Descrição opcional…"
                />
              </label>
            </div>
          </div>
        </div>

        <div className="form-actions mt-3">
          <button type="submit" className="btn primary" disabled={saving}>
            <Save size={14} />
            {saving ? 'Salvando…' : 'Salvar Produto'}
          </button>
          <Link className="btn" to={isEdit && id ? `/products/${id}` : '/'}>
            Cancelar
          </Link>
        </div>
      </form>
    </div>
  )
}
