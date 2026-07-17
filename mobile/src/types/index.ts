/**
 * Core TypeScript type definitions for MyDesk Mobile
 * Part of Phase 7: Mobile Applications
 */

// User & Authentication
export interface User {
  userId: number;
  tenantId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  department?: string;
  team?: string;
  profilePhotoUrl?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export type UserRole = 'Employee' | 'Manager' | 'Director' | 'Admin';

export interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken?: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken?: string;
  user: User;
  expiresIn: number;
}

// Expenses
export interface Expense {
  expenseId: number;
  tenantId: number;
  userId: number;
  amount: number;
  currency: string;
  category: string;
  description: string;
  status: ExpenseStatus;
  submittedAt?: string;
  approvedAt?: string;
  paidAt?: string;
  receiptUrl?: string;
  receiptOcrData?: ReceiptOcrData;
  costCenter?: string;
  departmentId?: number;
  approvalLevel?: number;
  createdAt: string;
  updatedAt: string;
  draftSyncAt?: string; // Offline draft sync timestamp
}

export type ExpenseStatus = 'Draft' | 'Submitted' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Paid';

export interface ReceiptOcrData {
  supplierName?: string;
  transactionDate?: string;
  grossAmount?: number;
  gstAmount?: number;
  currency?: string;
  extractionConfidence?: number;
  extractedAt?: string;
  rawText?: string;
}

export interface ExpenseFilter {
  status?: ExpenseStatus[];
  category?: string[];
  dateRange?: {
    startDate: string;
    endDate: string;
  };
  minAmount?: number;
  maxAmount?: number;
}

export interface ExpenseSortOption {
  field: 'date' | 'amount' | 'status';
  order: 'asc' | 'desc';
}

// Approvals
export interface Approval {
  approvalId: number;
  expenseId: number;
  userId: number;
  approverUserId: number;
  status: ApprovalStatus;
  level: number;
  comment?: string;
  requiredAt?: string;
  respondedAt?: string;
  expense?: Expense;
  approver?: User;
}

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'MoreInfoNeeded' | 'Escalated';

export interface ApprovalAction {
  approvalId: number;
  action: 'Approve' | 'Reject' | 'MoreInfo';
  comment?: string;
  escalate?: boolean;
}

export interface ApprovalStats {
  pendingCount: number;
  totalThisMonth: number;
  averageTimeToApprove: number; // minutes
  approvalRate: number; // percentage
  slaBreach: number;
}

// Notifications
export interface Notification {
  notificationId: number;
  userId: number;
  title: string;
  body: string;
  type: NotificationType;
  data?: Record<string, any>;
  deepLink?: string;
  isRead: boolean;
  createdAt: string;
  expireAt?: string;
}

export type NotificationType =
  | 'ApprovalRequired'
  | 'ApprovalDecision'
  | 'ExpenseSubmitted'
  | 'PaymentProcessing'
  | 'System'
  | 'Alert';

export interface NotificationPreferences {
  enableEmail: boolean;
  enablePush: boolean;
  enableInApp: boolean;
  enableSound: boolean;
  enableVibration: boolean;
  quietHoursStart?: string; // HH:mm format
  quietHoursEnd?: string;
  notificationTypes?: {
    approvalRequired: boolean;
    approvalDecision: boolean;
    expenseSubmitted: boolean;
    paymentProcessing: boolean;
  };
}

// Dashboard & Analytics
export interface DashboardData {
  monthToDate: number;
  approved: number;
  pending: number;
  paid: number;
  pendingApprovals: number;
  reimbursementDue?: string;
}

export interface SpendingTrend {
  month: string;
  amount: number;
}

export interface CategoryBreakdown {
  category: string;
  amount: number;
  percentage: number;
}

export interface BudgetStatus {
  departmentBudget: number;
  spent: number;
  remaining: number;
  percentage: number;
  status: 'Healthy' | 'Warning' | 'Critical';
}

// Sync & Offline
export interface SyncStatus {
  isSyncing: boolean;
  lastSyncAt?: string;
  syncError?: string;
  pendingChanges: number;
  offlineMode: boolean;
}

export interface OfflineQueue {
  id: string;
  action: 'CREATE' | 'UPDATE' | 'DELETE';
  entityType: 'Expense' | 'Approval';
  entityId?: number;
  payload: any;
  createdAt: string;
  status: 'Pending' | 'Syncing' | 'Failed';
  retryCount: number;
  lastError?: string;
}

// API Responses
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: any;
  };
  timestamp: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  code: string;
  message: string;
  statusCode: number;
  details?: any;
  timestamp: string;
}

// Navigation
export type RootStackParamList = {
  Root: undefined;
  NotFound: undefined;
};

export type AuthStackParamList = {
  Login: undefined;
  Signup: undefined;
  ForgotPassword: undefined;
};

export type AppStackParamList = {
  Dashboard: undefined;
  Expenses: undefined;
  ExpenseDetail: { expenseId: number };
  CreateExpense: { draftId?: number };
  Approvals: undefined;
  ApprovalDetail: { approvalId: number };
  Notifications: undefined;
  Profile: undefined;
  Settings: undefined;
};

// App State
export interface AppState {
  auth: AuthState;
  expenses: {
    items: Expense[];
    isLoading: boolean;
    error?: string;
    currentExpense?: Expense;
    filter: ExpenseFilter;
    sort: ExpenseSortOption;
  };
  approvals: {
    items: Approval[];
    isLoading: boolean;
    error?: string;
    stats?: ApprovalStats;
  };
  notifications: {
    items: Notification[];
    unreadCount: number;
    preferences?: NotificationPreferences;
    isLoading: boolean;
  };
  sync: SyncStatus;
  ui: {
    theme: 'light' | 'dark' | 'auto';
    language: string;
    toast?: {
      message: string;
      type: 'success' | 'error' | 'info' | 'warning';
      duration: number;
    };
  };
}

// Network
export interface NetworkState {
  isConnected: boolean;
  type?: 'wifi' | 'cellular' | 'none';
  isInternetReachable?: boolean;
}
