/**
 * Redux Expenses Slice
 * Manages expense data and operations
 */

import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { Expense, ExpenseFilter, ExpenseSortOption } from '@types';
import { apiClient } from '@services/ApiClient';

interface ExpensesState {
  items: Expense[];
  isLoading: boolean;
  error?: string;
  currentExpense?: Expense;
  filter: ExpenseFilter;
  sort: ExpenseSortOption;
}

const initialState: ExpensesState = {
  items: [],
  isLoading: false,
  filter: {},
  sort: { field: 'date', order: 'desc' },
};

export const fetchExpenses = createAsyncThunk(
  'expenses/fetchExpenses',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<Expense[]>('/api/expenses');
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data || [];
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const getExpenseDetail = createAsyncThunk(
  'expenses/getExpenseDetail',
  async (expenseId: number, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<Expense>(`/api/expenses/${expenseId}`);
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const createExpense = createAsyncThunk(
  'expenses/createExpense',
  async (
    expense: Partial<Expense> & { receiptFile?: any },
    { rejectWithValue }
  ) => {
    try {
      const { receiptFile, ...expenseData } = expense;
      const response = await apiClient.post<Expense>('/api/expenses', expenseData);
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const updateExpense = createAsyncThunk(
  'expenses/updateExpense',
  async (
    { id, data }: { id: number; data: Partial<Expense> },
    { rejectWithValue }
  ) => {
    try {
      const response = await apiClient.patch<Expense>(`/api/expenses/${id}`, data);
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const deleteExpense = createAsyncThunk(
  'expenses/deleteExpense',
  async (expenseId: number, { rejectWithValue }) => {
    try {
      await apiClient.delete(`/api/expenses/${expenseId}`);
      return expenseId;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

const expensesSlice = createSlice({
  name: 'expenses',
  initialState,
  reducers: {
    setFilter: (state, action) => {
      state.filter = action.payload;
    },
    setSort: (state, action) => {
      state.sort = action.payload;
    },
    clearError: (state) => {
      state.error = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchExpenses.pending, (state) => {
        state.isLoading = true;
        state.error = undefined;
      })
      .addCase(fetchExpenses.fulfilled, (state, action) => {
        state.isLoading = false;
        state.items = action.payload;
      })
      .addCase(fetchExpenses.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(getExpenseDetail.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(getExpenseDetail.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentExpense = action.payload;
      })
      .addCase(getExpenseDetail.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(createExpense.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(createExpense.fulfilled, (state, action) => {
        state.isLoading = false;
        state.items.unshift(action.payload!);
      })
      .addCase(createExpense.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(updateExpense.fulfilled, (state, action) => {
        const index = state.items.findIndex((e) => e.expenseId === action.payload?.expenseId);
        if (index !== -1 && action.payload) {
          state.items[index] = action.payload;
        }
      });

    builder
      .addCase(deleteExpense.fulfilled, (state, action) => {
        state.items = state.items.filter((e) => e.expenseId !== action.payload);
      });
  },
});

export const { setFilter, setSort, clearError } = expensesSlice.actions;
export default expensesSlice.reducer;
