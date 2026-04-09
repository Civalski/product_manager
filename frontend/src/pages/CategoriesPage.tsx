import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { AlertCircle, ArrowLeft, Plus, Trash2, Tags } from 'lucide-react'
import {
  createCategoryField,
  deleteCategoryField,
  fetchCategories,
  fetchCategoryFields,
} from '../api/categoriesApi'
import { getApiErrorMessage } from '../lib/apiClient'
import type { CategoryFieldResponse, CategoryResponse } from '../types/product'

export function CategoriesPage() {
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [fieldsByCategory, setFieldsByCategory] = useState<Record<string, CategoryFieldResponse[]>>({})
  const [newFieldNameByCategory, setNewFieldNameByCategory] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [savingCategoryId, setSavingCategoryId] = useState<string | null>(null)
  const [deletingFieldKey, setDeletingFieldKey] = useState<string | null>(null)

  const loadCategories = useCallback(async () => {
    setError(null)
    try {
      const list = await fetchCategories()
      setCategories(list)
      const entries = await Promise.all(
        list.map(async (c) => {
          try {
            const fields = await fetchCategoryFields(c.id)
            return [c.id, fields] as const
          } catch {
            return [c.id, [] as CategoryFieldResponse[]] as const
          }
        }),
      )
      setFieldsByCategory(Object.fromEntries(entries))
    } catch (e) {
      setError(getApiErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadCategories()
  }, [loadCategories])

  const addField = async (categoryId: string) => {
    const name = (newFieldNameByCategory[categoryId] ?? '').trim()
    if (!name) return
    setSavingCategoryId(categoryId)
    setError(null)
    try {
      const created = await createCategoryField(categoryId, name)
      setFieldsByCategory((prev) => ({
        ...prev,
        [categoryId]: [...(prev[categoryId] ?? []), created].sort(
          (a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'pt-BR'),
        ),
      }))
      setNewFieldNameByCategory((prev) => ({ ...prev, [categoryId]: '' }))
    } catch (e) {
      setError(getApiErrorMessage(e))
    } finally {
      setSavingCategoryId(null)
    }
  }

  const removeField = async (categoryId: string, fieldId: string) => {
    const key = `${categoryId}:${fieldId}`
    setDeletingFieldKey(key)
    setError(null)
    try {
      await deleteCategoryField(categoryId, fieldId)
      setFieldsByCategory((prev) => ({
        ...prev,
        [categoryId]: (prev[categoryId] ?? []).filter((f) => f.id !== fieldId),
      }))
    } catch (e) {
      setError(getApiErrorMessage(e))
    } finally {
      setDeletingFieldKey(null)
    }
  }

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-text">
          <div className="spinner" />
          Carregando categorias…
        </div>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="breadcrumb">
        <Link to="/">Produtos</Link>
        <span className="breadcrumb-separator">/</span>
        <span>Categorias</span>
      </div>

      <header className="page-header">
        <div className="page-header-left">
          <h1>Categorias</h1>
          <p className="text-muted" style={{ margin: '6px 0 0', maxWidth: 520 }}>
            Defina campos personalizados por categoria. Eles aparecem ao criar ou editar produtos dessa
            categoria.
          </p>
        </div>
        <div className="header-actions">
          <Link className="btn ghost" to="/">
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

      {categories.length === 0 ? (
        <div className="detail-section">
          <div className="detail-section-header">
            <Tags size={14} />
            Nenhuma categoria
          </div>
          <div style={{ padding: '20px' }} className="text-muted">
            Crie categorias ao cadastrar um produto ou importe dados. Depois volte aqui para configurar os
            campos.
          </div>
        </div>
      ) : (
        <div className="categories-page-grid">
          {categories.map((c) => {
            const fields = fieldsByCategory[c.id] ?? []
            const newName = newFieldNameByCategory[c.id] ?? ''
            const saving = savingCategoryId === c.id
            return (
              <div key={c.id} className="detail-section category-card">
                <div className="detail-section-header">
                  <Tags size={14} />
                  {c.name}
                </div>
                <div style={{ padding: '16px 20px' }}>
                  {fields.length === 0 ? (
                    <p className="text-muted" style={{ margin: '0 0 12px', fontSize: '0.9rem' }}>
                      Nenhum campo extra. Adicione abaixo.
                    </p>
                  ) : (
                    <ul className="category-fields-list">
                      {fields.map((f) => {
                        const delKey = `${c.id}:${f.id}`
                        return (
                          <li key={f.id} className="category-field-row">
                            <span className="category-field-name">{f.name}</span>
                            <button
                              type="button"
                              className="btn ghost danger btn-icon"
                              title="Remover campo"
                              disabled={deletingFieldKey === delKey}
                              onClick={() => void removeField(c.id, f.id)}
                            >
                              <Trash2 size={14} />
                            </button>
                          </li>
                        )
                      })}
                    </ul>
                  )}
                  <div className="category-add-field-row">
                    <input
                      type="text"
                      value={newName}
                      onChange={(e) =>
                        setNewFieldNameByCategory((prev) => ({ ...prev, [c.id]: e.target.value }))
                      }
                      placeholder="Nome do novo campo"
                      maxLength={128}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          void addField(c.id)
                        }
                      }}
                    />
                    <button
                      type="button"
                      className="btn primary"
                      disabled={saving || !newName.trim()}
                      onClick={() => void addField(c.id)}
                    >
                      <Plus size={14} />
                      {saving ? 'Salvando…' : 'Adicionar'}
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
