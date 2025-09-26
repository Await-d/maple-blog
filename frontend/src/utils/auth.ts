/**
 * 认证工具函数
 * 提供获取和管理认证token的功能
 */

/**
 * 获取当前的认证token
 * @returns 认证token或null
 */
export function getAuthToken(): string | null {
  return localStorage.getItem('authToken');
}

/**
 * 设置认证token
 * @param token 认证token
 */
export function setAuthToken(token: string): void {
  localStorage.setItem('authToken', token);
}

/**
 * 移除认证token
 */
export function removeAuthToken(): void {
  localStorage.removeItem('authToken');
}

/**
 * 检查是否已认证
 * @returns 是否已认证
 */
export function isAuthenticated(): boolean {
  return getAuthToken() !== null;
}