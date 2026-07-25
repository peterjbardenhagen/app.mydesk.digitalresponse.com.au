import React, { useEffect, useState } from 'react';
import { View, StyleSheet, ScrollView, Image, TouchableOpacity, Alert } from 'react-native';
import { Text as PaperText, Card, Button, Divider, ProgressBar, Chip } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useRoute, RouteProp, useNavigation } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import { getApprovalDetail, approveExpense, rejectExpense } from '@store/slices/approvalsSlice';

type ApprovalDetailRoute = RouteProp<{ ApprovalDetail: { approvalId: number; expenseId: number } }, 'ApprovalDetail'>;

const ApprovalDetailScreen = () => {
  const route = useRoute<ApprovalDetailRoute>();
  const navigation = useNavigation<any>();
  const dispatch = useDispatch<AppDispatch>();
  const { approvalId, expenseId } = route.params;

  const { currentApproval, isLoading } = useSelector((state: RootState) => state.approvals);
  const { user } = useSelector((state: RootState) => state.auth);

  useEffect(() => {
    dispatch(getApprovalDetail(approvalId));
  }, [dispatch, approvalId]);

  const handleApprove = async () => {
    try {
      await dispatch(approveExpense({ approvalId, expenseId })).unwrap();
      Alert.alert('Success', 'Expense approved!', [{ text: 'OK', onPress: () => navigation.goBack() }]);
    } catch (error) {
      Alert.alert('Error', 'Failed to approve expense');
    }
  };

  const handleReject = () => {
    Alert.prompt('Reject Expense', 'Please provide a reason:', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Reject', onPress: (reason) => {
        dispatch(rejectExpense({ approvalId, expenseId, comment: reason }));
        Alert.alert('Success', 'Expense rejected');
        navigation.goBack();
      }}
    ]);
  };

  if (isLoading || !currentApproval) {
    return (
      <View style={styles.container}>
        <PaperText variant="bodyMedium">Loading approval details...</PaperText>
      </View>
    );
  }

  const approval = currentApproval;
  const slaProgress = approval.slaHoursRemaining ? Math.min(1, approval.slaHoursRemaining / 72) : 0;
  const isOverdue = slaProgress <= 0;

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.title}>{approval.expense?.description || 'Expense'}</PaperText>
          <View style={styles.amountRow}>
            <PaperText variant="h5" style={styles.amount}>
              {approval.expense?.currency || 'AUD'} {approval.expense?.amount?.toFixed(2) || '0.00'}
            </PaperText>
            <Chip mode="outlined" textStyle={{ color: getStatusColor(approval.status) }}>
              {approval.status}
            </Chip>
          </View>
          {approval.expense?.receiptUrl && (
            <Image source={{ uri: approval.expense.receiptUrl }} style={styles.receipt} resizeMode="contain" />
          )}
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <PaperText variant="h6" style={styles.sectionTitle}>Approval Details</PaperText>
          <View style={styles.detailRow}>
            <PaperText variant="bodySmall">Submitter: </PaperText>
            <PaperText variant="bodySmall">{approval.expense?.submitterName || 'Unknown'}</PaperText>
          </View>
          <View style={styles.detailRow}>
            <PaperText variant="bodySmall">Required By: </PaperText>
            <PaperText variant="bodySmall">{new Date(approval.requiredAt).toLocaleString()}</PaperText>
          </View>
          {approval.slaUrgency && (
            <View style={styles.slaContainer}>
              <ProgressBar progress={slaProgress} />
              <PaperText variant="bodySmall">{isOverdue ? 'OVERDUE' : `${Math.ceil(approval.slaHoursRemaining || 0)}h remaining`}</PaperText>
            </View>
          )}
        </Card.Content>
      </Card>

      <View style={styles.actions}>
        <Button mode="contained" buttonColor="#4caf50" onPress={handleApprove}>Approve</Button>
        <Button mode="contained" buttonColor="#f44336" onPress={handleReject}>Reject</Button>
      </View>
    </ScrollView>
  );
};

const getStatusColor = (status: string) => {
  switch (status) {
    case 'Approved': return '#4caf50';
    case 'Rejected': return '#f44336';
    case 'Pending': case 'Submitted': return '#ff9800';
    default: return '#999';
  }
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f5f5f5' },
  card: { margin: 16, backgroundColor: '#fff' },
  title: { fontWeight: 'bold', marginBottom: 8 },
  amountRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 },
  amount: { fontWeight: 'bold', color: '#1976d2' },
  receipt: { width: '100%', height: 150, marginTop: 8 },
  sectionTitle: { fontWeight: 'bold', marginBottom: 8 },
  detailRow: { flexDirection: 'row', marginBottom: 4 },
  slaContainer: { marginTop: 8 },
  actions: { flexDirection: 'row', justifyContent: 'space-around', padding: 16 },
});

export default ApprovalDetailScreen;