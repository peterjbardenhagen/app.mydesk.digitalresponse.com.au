/**
 * Redux Auth Slice
 * Manages authentication state and user information
 */

import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { AuthState, User, LoginRequest, LoginResponse } from '@types';
import { apiClient } from '@services/ApiClient';

const initialState: AuthState = {
  user: null,
  token: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  error: undefined,
};

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: LoginRequest, { rejectWithValue }) => {
    try {
      const response = await apiClient.post<LoginResponse>('/api/auth/login', credentials);
      if (!response.success || !response.data) {
        return rejectWithValue(response.error?.message || 'Login failed');
      }
      await apiClient.setToken(response.data.accessToken);
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message || 'Login failed');
    }
  }
);

export const logout = createAsyncThunk(
  'auth/logout',
  async (_, { rejectWithValue }) => {
    try {
      await apiClient.post('/api/auth/logout');
      await apiClient.clearToken();
      return null;
    } catch (error: any) {
      return rejectWithValue(error.message || 'Logout failed');
    }
  }
);

export const refreshToken = createAsyncThunk(
  'auth/refreshToken',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.post<LoginResponse>('/api/auth/refresh');
      if (!response.success || !response.data) {
        return rejectWithValue('Token refresh failed');
      }
      await apiClient.setToken(response.data.accessToken);
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message || 'Token refresh failed');
    }
  }
);

export const getCurrentUser = createAsyncThunk(
  'auth/getCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<User>('/api/auth/me');
      if (!response.success || !response.data) {
        return rejectWithValue('Failed to fetch user');
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message || 'Failed to fetch user');
    }
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = undefined;
    },
    setUser: (state, action: PayloadAction<User>) => {
      state.user = action.payload;
    },
  },
  extraReducers: (builder) => {
    // Login
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = undefined;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.token = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    // Logout
    builder
      .addCase(logout.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(logout.fulfilled, (state) => {
        state.isLoading = false;
        state.isAuthenticated = false;
        state.user = null;
        state.token = null;
        state.refreshToken = null;
      })
      .addCase(logout.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    // Refresh Token
    builder
      .addCase(refreshToken.fulfilled, (state, action) => {
        state.token = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;
      })
      .addCase(refreshToken.rejected, (state) => {
        state.isAuthenticated = false;
        state.user = null;
        state.token = null;
      });

    // Get Current User
    builder
      .addCase(getCurrentUser.fulfilled, (state, action) => {
        state.user = action.payload;
        state.isAuthenticated = true;
      })
      .addCase(getCurrentUser.rejected, (state) => {
        state.isAuthenticated = false;
        state.user = null;
      });
  },
});

export const { clearError, setUser } = authSlice.actions;
export default authSlice.reducer;
