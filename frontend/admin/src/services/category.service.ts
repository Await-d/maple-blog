import { ApiService } from './api';
import type {
  Category,
  CreateCategoryInput,
  UpdateCategoryInput,
  QueryParams,
} from '@/types';

class CategoryService {
  private baseUrl = '/categories';

  async getCategories(params?: QueryParams): Promise<Category[]> {
    return ApiService.get<Category[]>(this.baseUrl, params);
  }

  async getCategoryById(categoryId: string): Promise<Category> {
    return ApiService.get<Category>(`${this.baseUrl}/${categoryId}`);
  }

  async createCategory(payload: CreateCategoryInput): Promise<Category> {
    return ApiService.post<Category>(this.baseUrl, payload);
  }

  async updateCategory(categoryId: string, payload: UpdateCategoryInput): Promise<Category> {
    return ApiService.put<Category>(`${this.baseUrl}/${categoryId}`, payload);
  }

  async deleteCategory(categoryId: string): Promise<void> {
    await ApiService.delete(`${this.baseUrl}/${categoryId}`);
  }
}

export const categoryService = new CategoryService();
export default categoryService;
