/**
 * Expense form validation (Task 27)
 * Pure, testable. No React Native deps.
 */

export interface ExpenseFormValues {
  amount: string;
  currency: string;
  category: string;
  description: string;
}

export type ExpenseFormErrors = Partial<Record<keyof ExpenseFormValues, string>>;

const CURRENCY_RE = /^[A-Z]{3}$/;

export function validateExpenseForm(values: ExpenseFormValues): ExpenseFormErrors {
  const errors: ExpenseFormErrors = {};

  const amt = parseFloat(values.amount);
  if (!values.amount.trim()) {
    errors.amount = 'Amount is required';
  } else if (Number.isNaN(amt)) {
    errors.amount = 'Amount must be a number';
  } else if (amt <= 0) {
    errors.amount = 'Amount must be greater than zero';
  }

  if (!values.currency.trim()) {
    errors.currency = 'Currency is required';
  } else if (!CURRENCY_RE.test(values.currency.trim().toUpperCase())) {
    errors.currency = 'Currency must be a 3-letter code (e.g. AUD)';
  }

  if (!values.category.trim()) {
    errors.category = 'Category is required';
  }

  if (!values.description.trim()) {
    errors.description = 'Description is required';
  } else if (values.description.trim().length > 500) {
    errors.description = 'Description must be 500 characters or fewer';
  }

  return errors;
}
