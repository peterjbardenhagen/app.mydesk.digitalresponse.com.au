/**
 * Expenses Screen
 * List of user expenses
 * Part of Phase 7: Mobile Applications - Task 21 (Expenses List)
 */

import React, { useEffect } from 'react';
import { View, StyleSheet, FlatList, RefreshControl } from 'react-native';
import { Text as PaperText, Card, Button, FAB } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';

import { AppDispatch, RootState } from '@store/store';
import { fetchExpenses } from '@store/slices/expensesSlice';

const ExpensesScreen: React.FC<any> = ({ navigation }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { items: expenses, isLoading } = useSelector((state: RootState) => state.expenses);

  useEffect(() => {
    dispatch(fetchExpenses());
  }, [dispatch]);

  const renderExpenseItem = ({ item }: any) => (
    <Card
      style={styles.expenseCard}
      onPress={() => navigation.navigate('ExpenseDetail', { expenseId: item.expenseId })}
    >
      <Card.Content>
        <View style={styles.expenseHeader}>
          <PaperText variant="titleSmall">{item.description}</PaperText>
          <PaperText variant="titleSmall" style={styles.amount}>
            A${item.amount.toFixed(2)}
          </PaperText>
        </View>
        <View style={styles.expenseFooter}>
          <PaperText variant="bodySmall" style={styles.category}>
            {item.category}
          </PaperText>
          <PaperText
            variant="labelSmall"
            style={[styles.status, getStatusStyle(item.status)]}
          >
            {item.status}
          </PaperText>
        </View>
      </Card.Content>
    </Card>
  );

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <PaperText variant="headlineMedium">Expenses</PaperText>
      </View>

      <FlatList
        data={expenses}
        renderItem={renderExpenseItem}
        keyExtractor={(item) => item.expenseId.toString()}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={isLoading} onRefresh={() => dispatch(fetchExpenses())} />}
        ListEmptyComponent={
          <View style={styles.empty}>
            <PaperText variant="bodyMedium" style={styles.emptyText}>
              No expenses yet
            </PaperText>
            <Button mode="contained" style={styles.createButton}>
              Create First Expense
            </Button>
          </View>
        }
      />

      <FAB
        icon="plus"
        label="New Expense"
        style={styles.fab}
        onPress={() => navigation.navigate('CreateExpense')}
      />
    </View>
  );
};

const getStatusStyle = (status: string) => {
  switch (status) {
    case 'Approved':
      return { color: '#4caf50' };
    case 'Rejected':
      return { color: '#f44336' };
    case 'Pending':
    case 'Submitted':
      return { color: '#ff9800' };
    default:
      return { color: '#999' };
  }
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    padding: 16,
    backgroundColor: '#fff',
  },
  list: {
    padding: 16,
  },
  expenseCard: {
    marginBottom: 12,
    backgroundColor: '#fff',
  },
  expenseHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  amount: {
    fontWeight: 'bold',
    color: '#1976d2',
  },
  expenseFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  category: {
    color: '#666',
  },
  status: {
    fontWeight: 'bold',
  },
  empty: {
    alignItems: 'center',
    paddingVertical: 40,
  },
  emptyText: {
    color: '#999',
    marginBottom: 16,
  },
  createButton: {
    marginTop: 8,
  },
  fab: {
    position: 'absolute',
    right: 16,
    bottom: 16,
  },
});

export default ExpensesScreen;
