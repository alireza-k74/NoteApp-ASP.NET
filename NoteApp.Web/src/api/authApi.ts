import { apiRequest } from './client'
import type { TokenResponse, UserResponse } from '../types/api'

export function register(
  username: string,
  email: string,
  password: string,
): Promise<TokenResponse> {
  return apiRequest<TokenResponse>('/api/auth/register', {
    method: 'POST',
    skipAuth: true,
    body: JSON.stringify({ username, email, password }),
  })
}

export function login(
  email: string,
  password: string,
): Promise<TokenResponse> {
  return apiRequest<TokenResponse>('/api/auth/login', {
    method: 'POST',
    skipAuth: true,
    body: JSON.stringify({ email, password }),
  })
}

export function logout(refreshToken: string): Promise<void> {
  return apiRequest<void>('/api/auth/logout', {
    method: 'POST',
    skipAuth: true,
    skipRefresh: true,
    body: JSON.stringify({ refreshToken }),
  })
}

export function getMe(): Promise<UserResponse> {
  return apiRequest<UserResponse>('/api/auth/me')
}
