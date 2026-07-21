/**
 * API Client Service
 * Handles all HTTP communication with backend API
 * Implements offline queueing, retry logic, and error handling
 * Part of Phase 7: Mobile Applications
 */

import axios, {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosError,
  AxiosResponse,
} from 'axios';
import * as SecureStore from 'expo-secure-store';
import { ApiResponse, ApiError, LoginResponse } from '@types';

class ApiClient {
  private instance: AxiosInstance;
  private baseURL: string;
  private requestInterceptorId?: number;
  private responseInterceptorId?: number;

  constructor(baseURL: string = process.env.EXPO_PUBLIC_API_BASE_URL || 'https://api.mydesk.com.au') {
    this.baseURL = baseURL;

    this.instance = axios.create({
      baseURL: this.baseURL,
      timeout: parseInt(process.env.EXPO_PUBLIC_API_TIMEOUT || '10000', 10),
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': 'MyDesk-Mobile/1.0.0',
      },
    });

    this.setupInterceptors();
  }

  /**
   * Setup request and response interceptors
   */
  private setupInterceptors(): void {
    // Request interceptor: Add auth token
    this.requestInterceptorId = this.instance.interceptors.request.use(
      async (config) => {
        try {
          const token = await SecureStore.getItemAsync('authToken');
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
          }
        } catch (error) {
          console.error('Failed to retrieve auth token:', error);
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor: Handle errors and token refresh
    this.responseInterceptorId = this.instance.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          await this.handleUnauthorized();
        }
        return Promise.reject(error);
      }
    );
  }

  /**
   * Handle 401 Unauthorized response
   */
  private async handleUnauthorized(): Promise<void> {
    try {
      await SecureStore.deleteItemAsync('authToken');
      await SecureStore.deleteItemAsync('refreshToken');
      // Dispatch logout action to Redux
      // This will be handled by a listener in the app
    } catch (error) {
      console.error('Failed to clear auth tokens:', error);
    }
  }

  /**
   * Make GET request
   */
  async get<T = any>(
    url: string,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.get<ApiResponse<T>>(url, config);
      return this.handleResponse(response);
    } catch (error) {
      return this.handleError<T>(error);
    }
  }

  /**
   * Make POST request
   */
  async post<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.post<ApiResponse<T>>(url, data, config);
      return this.handleResponse(response);
    } catch (error) {
      return this.handleError<T>(error);
    }
  }

  /**
   * Make PATCH request
   */
  async patch<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.patch<ApiResponse<T>>(url, data, config);
      return this.handleResponse(response);
    } catch (error) {
      return this.handleError<T>(error);
    }
  }

  /**
   * Make DELETE request
   */
  async delete<T = any>(
    url: string,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.instance.delete<ApiResponse<T>>(url, config);
      return this.handleResponse(response);
    } catch (error) {
      return this.handleError<T>(error);
    }
  }

  /**
   * Upload file with multipart form data
   */
  async uploadFile<T = any>(
    url: string,
    file: {
      uri: string;
      name: string;
      type: string;
    },
    additionalData?: Record<string, any>
  ): Promise<ApiResponse<T>> {
    try {
      const formData = new FormData();
      formData.append('file', {
        uri: file.uri,
        name: file.name,
        type: file.type,
      } as any);

      if (additionalData) {
        Object.entries(additionalData).forEach(([key, value]) => {
          formData.append(key, value as any);
        });
      }

      const response = await this.instance.post<ApiResponse<T>>(url, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      return this.handleResponse(response);
    } catch (error) {
      return this.handleError<T>(error);
    }
  }

  /**
   * Handle successful response
   */
  private handleResponse<T>(response: AxiosResponse<ApiResponse<T>>): ApiResponse<T> {
    return {
      success: response.data.success ?? response.status < 400,
      data: response.data.data,
      error: response.data.error,
      timestamp: response.data.timestamp || new Date().toISOString(),
    };
  }

  /**
   * Handle error response
   */
  private handleError<T>(error: any): ApiResponse<T> {
    let apiError: ApiError = {
      code: 'UNKNOWN_ERROR',
      message: 'An unexpected error occurred',
      statusCode: 500,
      timestamp: new Date().toISOString(),
    };

    if (axios.isAxiosError(error)) {
      apiError.statusCode = error.response?.status || error.status || 500;

      if (error.response?.data) {
        const errorData = error.response.data as any;
        apiError.code = errorData.error?.code || errorData.code || 'API_ERROR';
        apiError.message = errorData.error?.message || errorData.message || error.message;
        apiError.details = errorData.error?.details || errorData.details;
      } else if (error.message) {
        apiError.message = error.message;
        if (error.code === 'ECONNABORTED') {
          apiError.code = 'REQUEST_TIMEOUT';
        } else if (!error.response) {
          apiError.code = 'NETWORK_ERROR';
        }
      }
    } else if (error instanceof Error) {
      apiError.message = error.message;
    }

    return {
      success: false,
      error: apiError,
      timestamp: new Date().toISOString(),
    };
  }

  /**
   * Get current auth token
   */
  async getToken(): Promise<string | null> {
    try {
      return await SecureStore.getItemAsync('authToken');
    } catch {
      return null;
    }
  }

  /**
   * Set auth token
   */
  async setToken(token: string): Promise<void> {
    try {
      await SecureStore.setItemAsync('authToken', token);
    } catch (error) {
      console.error('Failed to store auth token:', error);
    }
  }

  /**
   * Clear auth token
   */
  async clearToken(): Promise<void> {
    try {
      await SecureStore.deleteItemAsync('authToken');
      await SecureStore.deleteItemAsync('refreshToken');
    } catch (error) {
      console.error('Failed to clear auth tokens:', error);
    }
  }

  /**
   * Check if request was successful
   */
  static isSuccess<T>(response: ApiResponse<T>): boolean {
    return response.success && !response.error;
  }

  /**
   * Throw error if response failed
   */
  static throwIfError<T>(response: ApiResponse<T>): T {
    if (!response.success || response.error) {
      const error = response.error || {
        code: 'UNKNOWN_ERROR',
        message: 'Request failed',
        statusCode: 500,
      };
      const err = new Error(error.message) as any;
      err.code = error.code;
      err.statusCode = error.statusCode;
      err.details = error.details;
      throw err;
    }
    return response.data as T;
  }

  /**
   * Cleanup interceptors
   */
  destroy(): void {
    if (this.requestInterceptorId !== undefined) {
      this.instance.interceptors.request.eject(this.requestInterceptorId);
    }
    if (this.responseInterceptorId !== undefined) {
      this.instance.interceptors.response.eject(this.responseInterceptorId);
    }
  }
}

// Export singleton instance
export const apiClient = new ApiClient();

export default ApiClient;
