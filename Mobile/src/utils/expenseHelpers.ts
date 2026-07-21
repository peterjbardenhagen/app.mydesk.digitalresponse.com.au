/**
 * Expense helpers (Tasks 32-35, 36-40, 45)
 * Pure, testable functions for analytics, search, sort, grouping, dedup.
 */

import { Expense, ExpenseFilter, ExpenseSortOption, SpendingTrend, CategoryBreakdown } from '@types';

export function filterExpenses(expenses: Expense[], filter?: ExpenseFilter): Expense[] {
  if (!filter) return expenses;
  let result = expenses;
  if (filter.status?.length) {
    result = result.filter((e) => filter.status!.includes(e.status));
  }
  if (filter.category?.length) {
    result = result.filter((e) => filter.category!.includes(e.category));
  }
  if (filter.dateRange) {
    const start = new Date(filter.dateRange.startDate).getTime();
    const end = new Date(filter.dateRange.endDate).getTime();
    result = result.filter((e) => {
      const t = new Date(e.createdAt).getTime();
      return t >= start && t <= end;
    });
  }
  if (filter.minAmount != null) result = result.filter((e) => e.amount >= filter.minAmount!);
  if (filter.maxAmount != null) result = result.filter((e) => e.amount <= filter.maxAmount!);
  return result;
}

export function searchExpenses(expenses: Expense[], query: string): Expense[] {
  const q = query.trim().toLowerCase();
  if (!q) return expenses;
  return expenses.filter(
    (e) =>
      e.description.toLowerCase().includes(q) ||
      e.category.toLowerCase().includes(q) ||
      String(e.amount).includes(q) ||
      (e.receiptOcrData?.supplierName ?? '').toLowerCase().includes(q)
  );
}

export function sortExpenses(expenses: Expense[], sort: ExpenseSortOption): Expense[] {
  const sorted = [...expenses];
  sorted.sort((a, b) => {
    let cmp = 0;
    if (sort.field === 'date') {
      cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    } else if (sort.field === 'amount') {
      cmp = a.amount - b.amount;
    } else {
      cmp = a.status.localeCompare(b.status);
    }
    return sort.order === 'asc' ? cmp : -cmp;
  });
  return sorted;
}

export function spendingTrend(expenses: Expense[], months = 6): SpendingTrend[] {
  const buckets: Record<string, number> = {};
  const now = new Date();
  for (let i = months - 1; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const key = d.toLocaleDateString('en-AU', { month: 'short', year: '2-digit' });
    buckets[key] = 0;
  }
  expenses.forEach((e) => {
    const d = new Date(e.createdAt);
    const key = d.toLocaleDateString('en-AU', { month: 'short', year: '2-digit' });
    if (key in buckets) buckets[key] += e.amount;
  });
  return Object.entries(buckets).map(([month, amount]) => ({ month, amount }));
}

export function categoryBreakdown(expenses: Expense[]): CategoryBreakdown[] {
  const totals: Record<string, number> = {};
  let grand = 0;
  expenses.forEach((e) => {
    totals[e.category] = (totals[e.category] || 0) + e.amount;
    grand += e.amount;
  });
  return Object.entries(totals)
    .map(([category, amount]) => ({
      category,
      amount,
      percentage: grand > 0 ? (amount / grand) * 100 : 0,
    }))
    .sort((a, b) => b.amount - a.amount);
}

export function expenseInsights(expenses: Expense[]): {
  average: number;
  topCategory: string;
  trend: 'up' | 'down' | 'flat';
} {
  if (expenses.length === 0) return { average: 0, topCategory: '-', trend: 'flat' };
  const average = expenses.reduce((s, e) => s + e.amount, 0) / expenses.length;
  const breakdown = categoryBreakdown(expenses);
  const trend = spendingTrend(expenses, 6);
  let dir: 'up' | 'down' | 'flat' = 'flat';
  if (trend.length >= 2) {
    const last = trend[trend.length - 1].amount;
    const prev = trend[trend.length - 2].amount;
    if (last > prev * 1.05) dir = 'up';
    else if (last < prev * 0.95) dir = 'down';
  }
  return { average, topCategory: breakdown[0]?.category ?? '-', trend: dir };
}

export function findDuplicates(
  expenses: Expense[],
  candidate: { amount: number; createdAt: string },
  amountTolerancePct = 5,
  dayWindow = 2
): Expense[] {
  const candTime = new Date(candidate.createdAt).getTime();
  const lo = candidate.amount * (1 - amountTolerancePct / 100);
  const hi = candidate.amount * (1 + amountTolerancePct / 100);
  return expenses.filter((e) => {
    const t = new Date(e.createdAt).getTime();
    const dayDiff = Math.abs((t - candTime) / 86400000);
    return dayDiff <= dayWindow && e.amount >= lo && e.amount <= hi;
  });
}

export type ExpenseGroup = { key: string; items: Expense[] };

export function groupByDate(expenses: Expense[]): ExpenseGroup[] {
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  const startOfWeek = startOfToday - (now.getDay() === 0 ? 6 : now.getDay() - 1) * 86400000;
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1).getTime();
  const groups: Record<string, Expense[]> = { Today: [], 'This Week': [], 'This Month': [], Older: [] };
  expenses.forEach((e) => {
    const t = new Date(e.createdAt).getTime();
    if (t >= startOfToday) groups.Today.push(e);
    else if (t >= startOfWeek) groups['This Week'].push(e);
    else if (t >= startOfMonth) groups['This Month'].push(e);
    else groups.Older.push(e);
  });
  return Object.entries(groups)
    .filter(([, items]) => items.length > 0)
    .map(([key, items]) => ({ key, items }));
}
