import { apiJson } from '../lib/apiClient'

import type { CategoryFieldResponse, CategoryResponse } from '../types/product'

export async function fetchCategories(): Promise<CategoryResponse[]> {
  return apiJson<CategoryResponse[]>('/api/categories') as Promise<CategoryResponse[]>
}

export async function createCategory(name: string): Promise<CategoryResponse> {
  return apiJson<CategoryResponse>('/api/categories', {
    method: 'POST',
    body: JSON.stringify({ name }),
  }) as Promise<CategoryResponse>
}

export async function fetchCategoryFields(categoryId: string): Promise<CategoryFieldResponse[]> {
  return apiJson<CategoryFieldResponse[]>(`/api/categories/${categoryId}/fields`) as Promise<
    CategoryFieldResponse[]
  >
}

export async function createCategoryField(
  categoryId: string,
  name: string,
): Promise<CategoryFieldResponse> {
  return apiJson<CategoryFieldResponse>(`/api/categories/${categoryId}/fields`, {
    method: 'POST',
    body: JSON.stringify({ name }),
  }) as Promise<CategoryFieldResponse>
}

export async function deleteCategoryField(categoryId: string, fieldId: string): Promise<void> {
  await apiJson(`/api/categories/${categoryId}/fields/${fieldId}`, { method: 'DELETE' })
}
