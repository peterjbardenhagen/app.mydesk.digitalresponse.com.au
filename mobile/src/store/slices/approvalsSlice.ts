/**
 * Redux Approvals Slice
 * Manages approval data and operations
 */

import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { Approval, ApprovalAction, ApprovalStats } from '@types';
import { apiClient } from '@services/ApiClient';

interface ApprovalsState {
  items: Approval[];
  isLoading: boolean;
  error?: string;
  stats?: ApprovalStats;
}

const initialState: ApprovalsState = {
  items: [],
  isLoading: false,
};

export const fetchPendingApprovals = createAsyncThunk(
  'approvals/fetchPendingApprovals',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<Approval[]>('/api/approvals/pending');
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data || [];
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const getApprovalStats = createAsyncThunk(
  'approvals/getStats',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<ApprovalStats>('/api/approvals/stats');
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const submitApprovalAction = createAsyncThunk(
  'approvals/submitAction',
  async (action: ApprovalAction, { rejectWithValue }) => {
    try {
      const response = await apiClient.post(`/api/approvals/${action.approvalId}/action`, {
        action: action.action,
        comment: action.comment,
      });
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return action.approvalId;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

const approvalsSlice = createSlice({
  name: 'approvals',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPendingApprovals.pending, (state) => {
        state.isLoading = true;
        state.error = undefined;
      })
      .addCase(fetchPendingApprovals.fulfilled, (state, action) => {
        state.isLoading = false;
        state.items = action.payload;
      })
      .addCase(fetchPendingApprovals.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(getApprovalStats.fulfilled, (state, action) => {
        state.stats = action.payload;
      });

    builder
      .addCase(submitApprovalAction.fulfilled, (state, action) => {
        state.items = state.items.filter((a) => a.approvalId !== action.payload);
      });
  },
});

export const { clearError } = approvalsSlice.actions;
export default approvalsSlice.reducer;
