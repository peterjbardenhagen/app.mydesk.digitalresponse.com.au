import React, { useEffect, useState } from 'react';
import { View, StyleSheet, FlatList, RefreshControl, TouchableOpacity, Alert } from 'react-native';
import { Text as PaperText, Card, Button, Fab, Chip, Divider, ProgressBar } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import { fetchApprovals, approveExpense, rejectExpense } from '@store/slices/approvalsSlice';

const ApprovalsScreen = () => {
  const navigation = useNavigation<any>();
  const dispatch = useDispatch<AppDispatch>();
  const { items: approvals, isLoading, stats } = useSelector((state: RootState) => state.approvals);
  const { user } = useSelector((state: RootState) => state.auth);

  const [selected, setSelected] = useState<number[]>([]);
  const [filter, setFilter] = useState<'pending' | 'all'>('pending');

  useEffect(() => {
    dispatch(fetchApprovals());
  }, [dispatch]);

  const handleApprove = async (approvalId: number, expenseId: number, comment?: string) => {
    try {
      await dispatch(approveExpense({ approvalId, expenseId, comment })).unwrap();
      // Show success toast or update local state
    } catch (error) {
      Alert.alert('Error', 'Failed to approve expense');
    }
  };

  const handleReject = async (approvalId: number, expenseId: number, comment: string) => {
    try {
      await dispatch(rejectExpense({ approvalId, expenseId, comment })).unwrap();
      // Show success toast or update local state
    } catch (error) {
      Alert.alert('Error', 'Failed to reject expense');
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Approved': return '#4caf50';
      case 'Rejected': return '#f44336';
      case 'Pending': case 'Submitted': case 'MoreInfoNeeded': return '#ff9800';
      default: return '#999';
    }
  };

  const renderApprovalItem = ({ item }: any) => {
    const canApprove = item.approverUserId === user?.userId || 
                      (item.approverPermissions && item.approverPermissions.approve);
    const canReject = item.approverUserId === user?.userId ||
                     (item.approverPermissions && item.approverPermissions.reject);

    return (
      <Card style={styles.approvalCard}>
        <Card.Content onTouch={() => {}}>
          <View style={styles.approvalHeader}>
            <View style={styles.approvalInfo}>
              <PaperText variant="titleSmall">{item.description}</PaperText>
              <PaperText variant="bodySmall" style={styles.amount}>
                A${item.amount?.toFixed(2)}
              </PaperText>
              </PaperText>
            </View>
            <View style={styles.approvalRight}>
              <Chip
                style={[styles.statusChip, { backgroundColor: getStatusColor(item.status) }]}
                textStyle={{ color: '#fff', fontSize: 12 }}
              >
                {item.status}
              </Chip>
              {item.slaUrgency && (
                <View style={styles.urgencyBadge}>
                  <Text style={styles.urgencyText}>Urgent</Text>
                </View>
              )}
            </View>
          </View>

          <View style={styles.approvalMeta}>
<PaperText variant="labelSmall" style={styles.metaText}>
                  By: {item.approverName}
              </PaperText>
              <PaperText variant="labelSmall" style={styles.metaText}>
                  {new Date(item.requiredAt).toLocaleDateString()}
              </PaperText>
          </View>

          <Divider style={styles.divider} />

          <View style={styles.approvalActions}>
            <Button
              mode="contained"
              onPress={() => navigation.navigate('ApprovalDetail', { approvalId: item.approvalId, expenseId: item.expenseId })}
              style={styles.detailButton}
            >
              View Details
            </Button>

            {canApprove && (
              <Button
                mode="contained"
                buttonColor="#4caf50"
                onPress={() => handleApprove(item.approvalId, item.expenseId)}
                style={styles.actionButton}
              >
                Approve
              </Button>
            )}

            {canReject && (
              <Button
                mode="contained"
                buttonColor="#f44336"
                onPress={() => {
                  Alert.prompt('Reject Reason', 'Please provide a reason for rejection:', [
                    {
                      text: 'Cancel',
                      style: 'cancel',
                    },
                    {
                      text: 'Reject',
                      onPress: (text) => handleReject(item.approvalId, item.expenseId, text || 'No reason provided'),
                    },
                  ]);
                }}
                style={styles.actionButton}
              >
                Reject
              </Button>
            )}

            {selected.includes(item.approvalId) && (
              <Button
                mode="outlined"
                onPress={() => setSelected(selected.filter(id => id !== item.approvalId))}
                style={styles.deselectButton}
              >
                Deselect
              </Button>
            )}
          </View>
        </Card.Content>
      </Card>
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <PaperText variant="headlineMedium">Approvals</PaperText>
        {stats && (
          <View style={styles.statsContainer}>
            <View style={styles.statItem}>
              <PaperText variant="titleLarge" style={{ color: '#1976d2' }}>{stats.pendingCount}</PaperText>
              <PaperText variant="labelSmall">Pending</PaperText>
            </View>
            <View style={styles.statItem}>
              <PaperText variant="titleLarge" style={{ color: '#4caf50' }}>{stats.approvalRate}%</PaperText>
              <PaperText variant="labelSmall">Approval Rate</PaperText>
            </View>
            <View style={styles.statItem}>
              <PaperText variant="titleLarge" style={{ color: '#ff9800' }}>{stats.slaBreach}</PaperText>
              <PaperText variant="labelSmall">SLAs Breathed</PaperText>
            </View>
          </View>
        )}
      </View>

      <View style={styles.filterContainer}>
        <TouchableOpacity
          style={[styles.filterButton, filter === 'pending' && styles.filterButtonActive]}
          onPress={() => setFilter('pending')}
        >
          <PaperText style={styles.filterText}>Pending</PaperText>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.filterButton, filter === 'all' && styles.filterButtonActive]}
          onPress={() => setFilter('all')}
        >
          <PaperText style={styles.filterText}>All</PaperText>
        </TouchableOpacity>
      </View>

      <FlatList
        data={approvals}
        renderItem={renderApprovalItem}
        keyExtractor={(item) => item.approvalId.toString()}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={isLoading} onRefresh={() => dispatch(fetchApprovals())} />}
        ListEmptyComponent={
          <View style={styles.empty}>
            <PaperText variant="bodyMedium" style={styles.emptyText}>
              No approvals pending
            </PaperText>
          </View>
        }
      />

      {selected.length > 0 && (
        <View style={styles.bulkActionsBar}>
          <PaperText variant="labelSmall" style={styles.selectedCount}>
            {selected.length} selected
          </PaperText>
          <Button
            mode="contained"
            buttonColor="#4caf50"
            onPress={() => {
              // Handle bulk approve
              Alert.alert('Bulk Approve', 'This will approve all selected expenses');
              setSelected([]);
            }}
            style={styles.bulkButton}
          >
            Approve Selected
          </Button>
          <Button
            mode="contained"
            buttonColor="#f44336"
            onPress={() => {
              // Handle bulk reject
              Alert.prompt('Reject Selected', 'Provide a reason:', [
                { text: 'Cancel', style: 'cancel' },
                { text: 'Reject', onPress: () => setSelected([]) },
              ]);
            }}
            style={styles.bulkButton}
          >
            Reject Selected
          </Button>
          <Button
            mode="outlined"
            onPress={() => setSelected([])}
            style={styles.bulkButton}
          >
            Clear
          </Button>
        </View>
      )}

      <Fab
        icon="plus"
        label="Add Approval"
        style={styles.fab}
        onPress={() => {
          // Navigation to create approval - though this would likely be done by an approver
          // For now, just show an alert or navigate based on requirements
          Alert.alert('Functionality', 'Approval creation typically managed through admin interface');
        }}
      />
    </View>
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
    marginTop: 40,
  },
  statsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginTop: 16,
    padding: 12,
    backgroundColor: '#fff',
    borderRadius: 8,
    elevation: 2,
  },
  statItem: {
    alignItems: 'center',
  },
  approvalCard: {
    marginBottom: 12,
    backgroundColor: '#fff',
    elevation: 3,
  },
  approvalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  approvalInfo: {
    flex: 1,
  },
  approvalRight: {
    alignItems: 'flex-end',
  },
  statusChip: {
    marginTop: 8,
  },
  metaText: {
    color: '#666',
    marginVertical: 4,
    fontSize: 12,
  },
  divider: {
    marginVertical: 12,
  },
  approvalActions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 8,
  },
  detailButton: {
    flex: 1,
    marginRight: 8,
  },
  actionButton: {
    flex: 1,
    marginHorizontal: 4,
  },
  deselectButton: {
    flex: 1,
    marginLeft: 8,
    backgroundColor: '#fff',
  },
  urgencyBadge: {
    backgroundColor: '#ffebee',
    padding: 4,
    borderRadius: 4,
    marginTop: 8,
    alignSelf: 'flex-end',
  },
  urgencyText: {
    color: '#c62828',
    fontSize: 10,
    fontWeight: 'bold',
  },
  filterContainer: {
    flexDirection: 'row',
    marginBottom: 16,
    backgroundColor: '#fff',
    borderRadius: 8,
    elevation: 1,
  },
  filterButton: {
    flex: 1,
    padding: 12,
    alignItems: 'center',
    borderRightWidth: 1,
    borderColor: '#eee',
  },
  filterButtonActive: {
    backgroundColor: '#e3f2fd',
  },
  filterText: {
    color: '#1976d2',
    fontSize: 14,
    fontWeight: '500',
  },
  list: {
    paddingBottom: 80,
  },
  empty: {
    alignItems: 'center',
    padding: 40,
  },
  emptyText: {
    color: '#999',
    marginTop: 16,
  },
  bulkActionsBar: {
    position: 'absolute',
    bottom: 20,
    left: 20,
    right: 20,
    backgroundColor: '#fff',
    borderRadius: 12,
    elevation: 5,
    padding: 16,
    flexDirection: 'column',
    gap: 8,
  },
  selectedCount: {
    color: '#1976d2',
    fontWeight: 'bold',
    marginBottom: 8,
    textAlign: 'center',
  },
  bulkButton: {
    marginBottom: 8,
  },
  fab: {
    position: 'absolute',
    right: 16,
    bottom: 16,
  },
  amount: {
    fontWeight: 'bold',
    color: '#1976d2',
    fontSize: 16,
  },
});

export default ApprovalsScreen;
