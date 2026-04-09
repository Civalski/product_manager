import { apiJson } from '../lib/apiClient'

import type { CategoryResponse } from '../types/product'

export async function fetchCategories(): Promise<CategoryResponse[]> {
  return apiJson<CategoryResponse[]>('/api/categories') as Promise<CategoryResponse[]>
}

export async function createCategory(name: string): Promise<CategoryResponse> {
  return apiJson<CategoryResponse>('/api/categories', {
    method: 'POST',
    body: JSON.stringify({ name }),
  }) as Promise<CategoryResponse>
}
