/**
 * Redux UI Slice
 * Manages UI state like theme and language
 */

import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface Toast {
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration: number;
}

interface UIState {
  theme: 'light' | 'dark' | 'auto';
  language: string;
  toast?: Toast;
}

const initialState: UIState = {
  theme: 'auto',
  language: 'en',
  toast: undefined,
};

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    setTheme: (state, action: PayloadAction<'light' | 'dark' | 'auto'>) => {
      state.theme = action.payload;
    },
    setLanguage: (state, action: PayloadAction<string>) => {
      state.language = action.payload;
    },
    showToast: (state, action: PayloadAction<Toast>) => {
      state.toast = action.payload;
    },
    hideToast: (state) => {
      state.toast = undefined;
    },
  },
});

export const { setTheme, setLanguage, showToast, hideToast } = uiSlice.actions;
export default uiSlice.reducer;
