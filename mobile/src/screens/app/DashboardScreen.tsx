/**
 * Dashboard Screen
 * Personal expense summary and analytics
 * Part of Phase 7: Mobile Applications - Task 76 (Personal Dashboard)
 */

import React, { useEffect, useMemo } from 'react';
import { View, StyleSheet, ScrollView, RefreshControl, Dimensions } from 'react-native';
import { Text as PaperText, Card, ProgressBar } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { LineChart, PieChart } from 'recharts-native';

import { AppDispatch, RootState } from '@store/store';
import { fetchExpenses } from '@store/slices/expensesSlice';

const DashboardScreen: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { items: expenses, isLoading } = useSelector((state: RootState) => state.expenses);

  useEffect(() => {
    dispatch(fetchExpenses());
  }, [dispatch]);

  const monthToDate = expenses.reduce((sum, e) => sum + e.amount, 0);
  const approved = expenses.filter((e) => e.status === 'Approved').length;
  const pending = expenses.filter((e) => e.status === 'Submitted').length;

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={isLoading} onRefresh={() => dispatch(fetchExpenses())} />}
    >
      <View style={styles.header}>
        <PaperText variant="headlineMedium">Dashboard</PaperText>
      </View>

      <Card style={styles.card}>
        <Card.Content>
          <PaperText variant="labelSmall" style={styles.label}>
            Month to Date
          </PaperText>
          <PaperText variant="displaySmall" style={styles.amount}>
            A${monthToDate.toFixed(2)}
          </PaperText>
        </Card.Content>
      </Card>

      <View style={styles.statsRow}>
        <Card style={[styles.card, styles.statCard]}>
          <Card.Content>
            <PaperText variant="labelSmall">Approved</PaperText>
            <PaperText variant="headlineSmall">{approved}</PaperText>
          </Card.Content>
        </Card>
        <Card style={[styles.card, styles.statCard]}>
          <Card.Content>
            <PaperText variant="labelSmall">Pending</PaperText>
            <PaperText variant="headlineSmall">{pending}</PaperText>
          </Card.Content>
        </Card>
      </View>

      <PaperText variant="titleMedium" style={styles.sectionTitle}>
        Budget Status
      </PaperText>
      <Card style={styles.card}>
        <Card.Content>
          <View style={styles.budgetRow}>
            <PaperText variant="bodyMedium">Remaining Budget</PaperText>
            <PaperText variant="bodyMedium" style={styles.positive}>
              A$25,000
            </PaperText>
          </View>
          <ProgressBar progress={0.75} style={styles.progressBar} />
          <PaperText variant="labelSmall" style={styles.budgetLabel}>
            75% used • A$75,000 of A$100,000
          </PaperText>
        </Card.Content>
      </Card>

      <PaperText variant="labelSmall" style={styles.placeholder}>
        Full dashboard analytics and charts coming in next iteration
      </PaperText>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 16,
  },
  header: {
    marginBottom: 20,
  },
  card: {
    marginBottom: 12,
    backgroundColor: '#fff',
  },
  label: {
    color: '#666',
    marginBottom: 4,
  },
  amount: {
    fontWeight: 'bold',
    color: '#1976d2',
  },
  statsRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 20,
  },
  statCard: {
    flex: 1,
  },
  sectionTitle: {
    marginTop: 20,
    marginBottom: 12,
    color: '#333',
  },
  budgetRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  positive: {
    color: '#4caf50',
    fontWeight: 'bold',
  },
  progressBar: {
    marginVertical: 8,
    height: 4,
  },
  budgetLabel: {
    color: '#999',
    marginTop: 4,
  },
  placeholder: {
    color: '#999',
    marginTop: 20,
    textAlign: 'center',
  },
});

export default DashboardScreen;
