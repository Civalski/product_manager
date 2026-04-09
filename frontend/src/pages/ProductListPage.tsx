import { useCallback, useEffect, useState } from 'react'

import { Link, useSearchParams } from 'react-router-dom'

import {

  Plus,

  Search,

  SlidersHorizontal,

  Eye,

  Pencil,

  Trash2,

  ChevronLeft,

  ChevronRight,

  PackageOpen,

  AlertCircle,

  X,

} from 'lucide-react'

import { fetchCategories } from '../api/categoriesApi'

import { deleteProduct, fetchProducts, type ProductListParams } from '../api/productsApi'

import { getApiErrorMessage } from '../lib/apiClient'
import {
  cosmosBrandNameFromProduct,
  cosmosThumbnailFromProduct,
  isCosmosBackedProduct,
} from '../lib/productCosmos'

import type { CategoryResponse, PagedProductsResponse, ProductResponse } from '../types/product'



const money = (n: number) =>

  n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })



function StockBadge({ stock }: { stock: number }) {

  if (stock === 0) return <span className="badge badge-danger">Esgotado</span>

  if (stock <= 5) return <span className="badge badge-warning">{stock} un.</span>

  return <span className="badge badge-success">{stock} un.</span>

}



function ConfirmDialog({

  open,

  title,

  message,

  confirmLabel,

  onConfirm,

  onCancel,

}: {

  open: boolean

  title: string

  message: string

  confirmLabel: string

  onConfirm: () => void

  onCancel: () => void

}) {

  if (!open) return null

  return (

    <div className="modal-overlay" onClick={onCancel}>

      <div className="modal" onClick={(e) => e.stopPropagation()}>

        <div className="modal-header">{title}</div>

        <div className="modal-body">{message}</div>

        <div className="modal-footer">

          <button type="button" className="btn" onClick={onCancel}>

            Cancelar

          </button>

          <button type="button" className="btn danger" onClick={onConfirm}>

            {confirmLabel}

          </button>

        </div>

      </div>

    </div>

  )

}



function TableSkeleton() {

  return (

    <>

      {Array.from({ length: 5 }).map((_, i) => (

        <div className="skeleton-row" key={i}>

          <div className="skeleton skeleton-cell" style={{ width: '80px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '56px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '40px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '160px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '100px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '70px', height: '14px' }} />

          <div className="skeleton skeleton-cell" style={{ width: '50px', height: '14px' }} />

        </div>

      ))}

    </>

  )

}



const filterKeys = [

  'search',

  'categoryId',

  'minPrice',

  'maxPrice',

  'stockFilter',

  'pageSize',

]



export function ProductListPage() {

  const [searchParams, setSearchParams] = useSearchParams()

  const [data, setData] = useState<PagedProductsResponse | null>(null)

  const [categories, setCategories] = useState<CategoryResponse[]>([])

  const [loading, setLoading] = useState(true)

  const [error, setError] = useState<string | null>(null)

  const [filtersOpen, setFiltersOpen] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<ProductResponse | null>(null)

  const [deleting, setDeleting] = useState(false)



  useEffect(() => {

    void fetchCategories().then(setCategories).catch(() => setCategories([]))

  }, [])



  const readFilters = useCallback((): ProductListParams => {

    const page = Number(searchParams.get('page') ?? '1') || 1

    const pageSize = Number(searchParams.get('pageSize') ?? '10') || 10

    const search = searchParams.get('search') ?? ''

    const categoryId = searchParams.get('categoryId') ?? ''

    const minPrice = searchParams.get('minPrice')

    const maxPrice = searchParams.get('maxPrice')

    const stockFilter = searchParams.get('stockFilter') ?? ''

    return {

      page,

      pageSize,

      search: search || undefined,

      categoryId: categoryId || undefined,

      minPrice: minPrice ? Number(minPrice) : undefined,

      maxPrice: maxPrice ? Number(maxPrice) : undefined,

      stockFilter: stockFilter || undefined,

    }

  }, [searchParams])



  const load = useCallback(async () => {

    setLoading(true)

    setError(null)

    try {

      const res = await fetchProducts(readFilters())

      setData(res)

    } catch (e) {

      setData(null)

      setError(getApiErrorMessage(e))

    } finally {

      setLoading(false)

    }

  }, [readFilters])



  useEffect(() => {

    void load()

  }, [load])



  const hasActiveFilters = useCallback(() => {

    return filterKeys.some((k) => {

      const v = searchParams.get(k)

      if (k === 'pageSize') return v != null && v !== '10'

      return Boolean(v?.trim())

    })

  }, [searchParams])



  const applyFilters = (e: React.FormEvent<HTMLFormElement>) => {

    e.preventDefault()

    const fd = new FormData(e.currentTarget)

    const next = new URLSearchParams()

    const setIf = (key: string, value: string) => {

      if (value.trim()) next.set(key, value.trim())

    }

    setIf('search', String(fd.get('search') ?? ''))

    setIf('categoryId', String(fd.get('categoryId') ?? ''))

    setIf('minPrice', String(fd.get('minPrice') ?? ''))

    setIf('maxPrice', String(fd.get('maxPrice') ?? ''))

    const sf = String(fd.get('stockFilter') ?? '')

    if (sf && sf !== 'any') next.set('stockFilter', sf)

    next.set('page', '1')

    next.set('pageSize', String(fd.get('pageSize') ?? '10'))

    setSearchParams(next)

    setFiltersOpen(false)

  }



  const clearFilters = () => {

    setSearchParams(new URLSearchParams())

  }



  const goPage = (p: number) => {

    const next = new URLSearchParams(searchParams)

    next.set('page', String(p))

    setSearchParams(next)

  }



  const confirmDelete = async () => {

    if (!deleteTarget) return

    setDeleting(true)

    try {

      await deleteProduct(deleteTarget.id)

      setDeleteTarget(null)

      await load()

    } catch (e) {

      alert(getApiErrorMessage(e))

    } finally {

      setDeleting(false)

    }

  }



  const f = readFilters()

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1



  const pageNumbers = (() => {

    if (!data) return []

    const pages: number[] = []

    const start = Math.max(1, data.page - 2)

    const end = Math.min(totalPages, data.page + 2)

    for (let i = start; i <= end; i++) pages.push(i)

    return pages

  })()



  const stockFilterValue = f.stockFilter ?? 'any'



  return (

    <div className="page">

      <header className="page-header">

        <div className="page-header-left">

          <h1>Produtos</h1>

          {data && <span className="page-header-count">{data.totalCount}</span>}

        </div>

        <div className="header-actions">

          <button

            type="button"

            className={`btn ${hasActiveFilters() ? 'primary' : ''}`}

            onClick={() => setFiltersOpen(!filtersOpen)}

          >

            <SlidersHorizontal />

            Filtros

            {hasActiveFilters() && <span>•</span>}

          </button>

          <Link className="btn primary" to="/products/new">

            <Plus />

            Novo Produto

          </Link>

        </div>

      </header>



      {filtersOpen && (

        <form className="filters-card" onSubmit={applyFilters} key={searchParams.toString()}>

          <div className="filters-header" style={{ cursor: 'default' }}>

            <span className="filters-title">

              <Search />

              Filtros de Pesquisa

            </span>

            {hasActiveFilters() && (

              <button type="button" className="btn ghost" onClick={clearFilters}>

                <X size={14} />

                Limpar

              </button>

            )}

          </div>

          <div className="filters-grid">

            <label className="full">

              Busca

              <input

                name="search"

                type="text"

                placeholder="Nome, SKU ou descrição (ignora maiúsculas)"

                defaultValue={f.search ?? ''}

              />

            </label>

            <label>

              Categoria

              <select name="categoryId" defaultValue={f.categoryId ?? ''}>

                <option value="">Todas</option>

                {categories.map((c) => (

                  <option key={c.id} value={c.id}>

                    {c.name}

                  </option>

                ))}

              </select>

            </label>

            <label>

              Estoque

              <select name="stockFilter" defaultValue={stockFilterValue}>

                <option value="any">Qualquer</option>

                <option value="available">Com estoque</option>

                <option value="out">Esgotado</option>

                <option value="low">Baixo (1–5 un.)</option>

              </select>

            </label>

            <label>

              Preço mín.

              <input name="minPrice" type="number" step="0.01" placeholder="0.00" defaultValue={f.minPrice ?? ''} />

            </label>

            <label>

              Preço máx.

              <input name="maxPrice" type="number" step="0.01" placeholder="0.00" defaultValue={f.maxPrice ?? ''} />

            </label>

            <label>

              Por página

              <select name="pageSize" defaultValue={String(f.pageSize ?? 10)}>

                <option value="5">5</option>

                <option value="10">10</option>

                <option value="25">25</option>

                <option value="50">50</option>

                <option value="100">100</option>

              </select>

            </label>

          </div>

          <div className="form-actions mt-3">

            <button type="submit" className="btn primary">

              <Search size={14} />

              Aplicar Filtros

            </button>

          </div>

        </form>

      )}



      {error && (

        <div className="alert error">

          <AlertCircle />

          {error}

        </div>

      )}



      <div className="table-card">

        {loading ? (

          <TableSkeleton />

        ) : data && data.items.length > 0 ? (

          <div className="table-wrap">

            <table className="data-table">

              <thead>

                <tr>

                  <th>SKU</th>

                  <th>Origem</th>

                  <th />

                  <th>Nome</th>

                  <th>Marca (Cosmos)</th>

                  <th>Categoria</th>

                  <th>Preço</th>

                  <th>Estoque</th>

                  <th style={{ width: '1%' }} />

                </tr>

              </thead>

              <tbody>

                {data.items.map((p) => {
                  const cosmosThumb = cosmosThumbnailFromProduct(p)
                  const cosmosBrand = cosmosBrandNameFromProduct(p)
                  return (
                  <tr key={p.id}>

                    <td className="cell-mono">{p.sku}</td>

                    <td>
                      {isCosmosBackedProduct(p) ? (
                        <span className="badge badge-accent" title="GTIN / Bluesoft Cosmos">
                          Cosmos
                        </span>
                      ) : (
                        <span className="badge badge-default">Interno</span>
                      )}
                    </td>

                    <td style={{ width: 48 }}>
                      {cosmosThumb ? (
                        <img
                          src={cosmosThumb}
                          alt=""
                          style={{
                            width: 40,
                            height: 40,
                            objectFit: 'contain',
                            borderRadius: 6,
                            border: '1px solid var(--border)',
                          }}
                        />
                      ) : null}
                    </td>

                    <td>

                      <Link className="cell-link" to={`/products/${p.id}`}>

                        {p.name}

                      </Link>

                    </td>

                    <td
                      style={{
                        fontSize: '0.85rem',
                        maxWidth: 140,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                      title={cosmosBrand ?? undefined}
                    >
                      {cosmosBrand ?? '—'}
                    </td>

                    <td>

                      <span className="badge badge-default">{p.category}</span>

                    </td>

                    <td>{money(p.price)}</td>

                    <td>

                      <StockBadge stock={p.stock} />

                    </td>

                    <td>

                      <div className="row-actions">

                        <Link className="btn-icon" to={`/products/${p.id}`} title="Ver detalhes">

                          <Eye />

                        </Link>

                        <Link className="btn-icon" to={`/products/${p.id}/edit`} title="Editar">

                          <Pencil />

                        </Link>

                        <button

                          type="button"

                          className="btn-icon danger"

                          onClick={() => setDeleteTarget(p)}

                          title="Excluir"

                        >

                          <Trash2 />

                        </button>

                      </div>

                    </td>

                  </tr>
                  )
                })}

              </tbody>

            </table>

          </div>

        ) : (

          <div className="empty-state">

            <div className="empty-state-icon">

              <PackageOpen />

            </div>

            <div className="empty-state-title">Nenhum produto encontrado</div>

            <div className="empty-state-text">

              {hasActiveFilters()

                ? 'Tente ajustar os filtros de pesquisa.'

                : 'Adicione um novo produto para começar.'}

            </div>

          </div>

        )}

      </div>



      {!loading && data && data.items.length > 0 && (

        <div className="pagination">

          <span className="pagination-info">

            Mostrando {(data.page - 1) * data.pageSize + 1}–

            {Math.min(data.page * data.pageSize, data.totalCount)} de {data.totalCount} produto(s)

          </span>

          <div className="pagination-controls">

            <button

              type="button"

              className="pagination-btn"

              disabled={data.page <= 1}

              onClick={() => goPage(data.page - 1)}

            >

              <ChevronLeft />

            </button>

            {pageNumbers.map((p) => (

              <button

                key={p}

                type="button"

                className={`pagination-btn ${p === data.page ? 'active' : ''}`}

                onClick={() => goPage(p)}

              >

                {p}

              </button>

            ))}

            <button

              type="button"

              className="pagination-btn"

              disabled={data.page >= totalPages}

              onClick={() => goPage(data.page + 1)}

            >

              <ChevronRight />

            </button>

          </div>

        </div>

      )}



      <ConfirmDialog

        open={!!deleteTarget}

        title="Excluir Produto"

        message={

          deleteTarget

            ? `Tem certeza que deseja excluir "${deleteTarget.name}" (${deleteTarget.sku})? Esta ação não pode ser desfeita.`

            : ''

        }

        confirmLabel={deleting ? 'Excluindo…' : 'Excluir'}

        onConfirm={confirmDelete}

        onCancel={() => !deleting && setDeleteTarget(null)}

      />

    </div>

  )

}

