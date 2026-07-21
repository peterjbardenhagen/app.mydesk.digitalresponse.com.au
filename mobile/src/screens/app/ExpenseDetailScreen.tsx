import React, { useEffect, useState } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Image,
  Linking,
  Alert,
} from 'react-native';
import {
  Text as PaperText,
  Card,
  Button,
  Chip,
  ActivityIndicator,
  Divider,
} from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import {
  getExpenseDetail,
  deleteExpense,
} from '@store/slices/expensesSlice';
import { Expense, ExpenseStatus } from '@types';

type DetailRoute = RouteProp<{ ExpenseDetail: { expenseId: number } }, 'ExpenseDetail'>;

const statusColor: Record<ExpenseStatus, string> = {
  Draft: '#9e9e9e',
  Submitted: '#ff9800',
  PendingApproval: '#ff9800',
  Approved: '#4caf50',
  Rejected: '#f44336',
  Paid: '#1976d2',
};

const ExpenseDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<DetailRoute>();
  const dispatch = useDispatch<AppDispatch>();
  const { expenseId } = route.params;

  const { currentExpense, isLoading, error } = useSelector(
    (state: RootState) => state.expenses
  );

  const [expense, setExpense] = useState<Expense | null>(null);

  useEffect(() => {
    dispatch(getExpenseDetail(expenseId));
  }, [dispatch, expenseId]);

  useEffect(() => {
    if (currentExpense && currentExpense.expenseId === expenseId) {
      setExpense(currentExpense);
    }
  }, [currentExpense, expenseId]);

  const handleDelete = () => {
    Alert.alert('Delete expense', 'This cannot be undone.', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: () => {
          dispatch(deleteExpense(expenseId));
          navigation.goBack();
        },
      },
    ]);
  };

  if (isLoading && !expense) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (error || !expense) {
    return (
      <View style={styles.centered}>
        <PaperText variant="bodyMedium" style={styles.errorText}>
          {error || 'Expense not found'}
        </PaperText>
        <Button mode="outlined" onPress={() => navigation.goBack()}>
          Back
        </Button>
      </View>
    );
  }

  const ocr = expense.receiptOcrData;
  const canEdit = expense.status === 'Draft';

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <View style={styles.headerRow}>
            <PaperText variant="headlineSmall">{expense.description}</PaperText>
            <Chip
              textStyle={{ color: '#fff' }}
              style={[styles.statusChip, { backgroundColor: statusColor[expense.status] }]}
            >
              {expense.status}
            </Chip>
          </View>
          <PaperText variant="displaySmall" style={styles.amount}>
            {expense.currency} {expense.amount.toFixed(2)}
          </PaperText>
          <View style={styles.metaRow}>
            <PaperText variant="bodyMedium">{expense.category}</PaperText>
            <PaperText variant="bodySmall" style={styles.muted}>
              {new Date(expense.createdAt).toLocaleDateString()}
            </PaperText>
          </View>
          {expense.costCenter && (
            <PaperText variant="bodySmall" style={styles.muted}>
              Cost center: {expense.costCenter}
            </PaperText>
          )}
        </Card.Content>
      </Card>

      {expense.receiptUrl && (
        <Card style={styles.card}>
          <Card.Content>
            <PaperText variant="titleMedium" style={styles.sectionTitle}>
              Receipt
            </PaperText>
            <Image
              source={{ uri: expense.receiptUrl }}
              style={styles.receiptImage}
              resizeMode="contain"
            />
            {ocr && (
              <View style={styles.ocrBox}>
                <Divider style={styles.divider} />
                <PaperText variant="labelSmall" style={styles.muted}>
                  Extracted (confidence:{' '}
                  {Math.round((ocr.extractionConfidence ?? 0) * 100)}%)
                </PaperText>
                {ocr.supplierName && (
                  <PaperText variant="bodySmall">Supplier: {ocr.supplierName}</PaperText>
                )}
                {ocr.transactionDate && (
                  <PaperText variant="bodySmall">Date: {ocr.transactionDate}</PaperText>
                )}
                {ocr.gstAmount != null && (
                  <PaperText variant="bodySmall">
                    GST: {expense.currency} {ocr.gstAmount.toFixed(2)}
                  </PaperText>
                )}
              </View>
            )}
          </Card.Content>
        </Card>
      )}

      <View style={styles.actions}>
        {canEdit && (
          <Button
            mode="contained"
            onPress={() => navigation.navigate('CreateExpense', { draftId: expense.expenseId })}
            style={styles.actionButton}
          >
            Edit
          </Button>
        )}
        {expense.receiptUrl && (
          <Button
            mode="outlined"
            onPress={() => Linking.openURL(expense.receiptUrl!)}
            style={styles.actionButton}
          >
            Open Receipt
          </Button>
        )}
        {canEdit && (
          <Button mode="text" textColor="#f44336" onPress={handleDelete}>
            Delete
          </Button>
        )}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f5f5f5', padding: 16 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  card: { marginBottom: 12, backgroundColor: '#fff' },
  headerRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  statusChip: { height: 28 },
  amount: { fontWeight: 'bold', color: '#1976d2', marginVertical: 8 },
  metaRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  muted: { color: '#999' },
  sectionTitle: { marginBottom: 8 },
  receiptImage: { width: '100%', height: 260, borderRadius: 8, backgroundColor: '#eee' },
  ocrBox: { marginTop: 8 },
  divider: { marginVertical: 8 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'flex-start', marginTop: 4 },
  actionButton: { marginRight: 8, marginBottom: 8 },
  errorText: { color: '#f44336', marginBottom: 16, textAlign: 'center' },
});

export default ExpenseDetailScreen;
