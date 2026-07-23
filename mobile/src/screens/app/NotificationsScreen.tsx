import React, { useEffect, useState } from 'react';
import { View, StyleSheet, FlatList, RefreshControl, TouchableOpacity, Alert, Platform } from 'react-native';
import { Text as PaperText, Card, Button, Chip, Divider, ProgressBar, Icon } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation, useRoute } from '@react-navigation/native';
import * as Notifications from 'expo-notifications';
import { Ionicons } from '@expo/vector-icons';

import { AppDispatch, RootState } from '@store/store';
import { fetchNotifications, markNotificationAsRead, clearNotification } from '@store/slices/notificationsSlice';

const NotificationsScreen = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const dispatch = useDispatch<AppDispatch>();
  const { items: notifications, isLoading, unreadCount, preferences } = useSelector((state: RootState) => state.notifications);
  const { offlineMode } = useSelector((state: RootState) => state.sync);

  const [selected, setSelected] = useState<number[]>([]);
  const [filter, setFilter] = useState<'all' | 'approval' | 'expense' | 'system'>('all');
  const [showRead, setShowRead] = useState(true);

  // Configure notification handler
  useEffect(() => {
    // Register for notifications
    Notifications.setNotificationHandler({
      handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: true,
      }),
    });

    // Request permissions
    (async () => {
      const { status } = await Notifications.requestPermissionsAsync();
      if (!status.granted) {
        console.warn('Notification permissions not granted');
      }
    })();

    // Fetch initial notifications
    dispatch(fetchNotifications());

    // Handle incoming notifications
    const subscription = Notifications.addNotificationReceivedListener(notification => {
      // Notification received in foreground
      console.log('Notification received:', notification);
    });

    // Handle notification tapped
    const responseSubscription = Notifications.addNotificationResponseReceivedListener(response => {
      // User tapped on notification
      const { data } = response.notification.request.content;
      if (data?.type === 'approval_required' && data?.approvalId) {
        navigation.navigate('ApprovalDetail', { approvalId: data.approvalId });
      } else if (data?.type === 'expense_submitted' && data?.expenseId) {
        navigation.navigate('ExpenseDetail', { expenseId: data.expenseId });
      }
    });

    return () => {
      subscription.remove();
      responseSubscription.remove();
    };
  }, [dispatch, navigation]);

  const handleMarkAsRead = async (notificationId: number) => {
    try {
      await dispatch(markNotificationAsRead(notificationId)).unwrap();
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  };

  const handleClearNotification = async (notificationId: number) => {
    try {
      await dispatch(clearNotification(notificationId)).unwrap();
    } catch (error) {
      console.error('Failed to clear notification:', error);
    }
  };

  const handleBulkMarkAsRead = () => {
    // Mark all visible notifications as read
    const idsToMark = notifications
      .filter(n => (!showRead || !n.isRead) && 
                   (filter === 'all' || n.type === filter))
      .map(n => n.notificationId);
    
    idsToMark.forEach(id => handleMarkAsRead(id));
  };

  const renderNotificationItem = ({ item }: any) => {
    const isVisible = showRead || !item.isRead;
    const matchesFilter = filter === 'all' || item.type === filter;
    
    if (!isVisible || !matchesFilter) return null;

    // Get icon based on notification type
    const getNotificationIcon = () => {
      switch (item.type) {
        case 'ApprovalRequired': return 'alert-circle';
        case 'ApprovalDecision': return 'checkmark-circle';
        case 'ExpenseSubmitted': return 'receipt';
        case 'PaymentProcessing': return 'cash';
        case 'System': return 'settings';
        case 'Alert': return 'alert';
        default: return 'information-circle';
      }
    };

    const getNotificationColor = () => {
      switch (item.type) {
        case 'ApprovalRequired': return '#ff9800'; // orange
        case 'ApprovalDecision': return '#4caf50'; // green
        case 'ExpenseSubmitted': return '#2196f3'; // blue
        case 'PaymentProcessing': return '#9c27b0'; // purple
        case 'System': return '#607d8b'; // blue-grey
        case 'Alert': return '#f44336'; // red
        default: return '#9e9e9e'; // grey
      }
    };

    return (
      <Card style={styles.notificationCard}>
        <Card.Content onPress={() => {
          // Handle tap to navigate
          if (item.data) {
            if (item.data.type === 'approval_required' && item.data.approvalId) {
              navigation.navigate('ApprovalDetail', { approvalId: item.data.approvalId });
            } else if (item.data.type === 'expense_submitted' && item.data.expenseId) {
              navigation.navigate('ExpenseDetail', { expenseId: item.data.expenseId });
            } else if (item.data.type === 'payment_processing' && item.data.expenseId) {
              navigation.navigate('ExpenseDetail', { expenseId: item.data.expenseId });
            }
          }
        }}>
          <View style={styles.notificationHeader}>
            <View style={styles.notificationIconContainer}>
              <Ionicons
                name={getNotificationIcon()}
                size={24}
                color={getNotificationColor()}
                style={{ marginRight: 8 }}
              />
              <PaperText variant="titleSmall" style={{ color: getNotificationColor() }}>
                {item.title}
              </PaperText>
            </View>
            {!item.isRead && (
              <View style={styles.unreadBadge}>
                <Text style={styles.unreadBadgeText}>●</Text>
              </View>
            )}
          </View>

          <View style={styles.notificationBody}>
            <PaperText variant="bodySmall">{item.body}</PaperText>
            {item.data && (
              <View style={styles.notificationData}>
                {item.data.expenseId && (
                  <PaperText variant="caption">Expense: #{item.data.expenseId}</PaperText>
                )}
                {item.data.amount && (
                  <PaperText variant="caption">Amount: A${item.data.amount}</PaperText>
                )}
                {item.data.approvalId && (
                  <PaperText variant="caption">Approval: #{item.data.approvalId}</PaperText>
                )}
              </View>
            )}
          </View>

          <View style={styles.notificationFooter}>
            <PaperText variant="caption" style={styles.notificationTime}>
              {new Date(item.createdAt).toLocaleTimeString()} • 
              {new Date(item.createdAt).toLocaleDateString()}
            </PaperText>
            
            <View style={styles.notificationActions}>
              {!item.isRead && (
                <Button
                  mode="text"
                  onPress={() => handleMarkAsRead(item.notificationId)}
                  style={styles.markReadButton}
                >
                  Mark as Read
                </Button>
              )}
              <Button
                mode="text"
                onPress={() => handleClearNotification(item.notificationId)}
                style={styles.clearButton}
              >
                Clear
              </Button>
            </View>
          </View>
        </Card.Content>
      </Card>
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <PaperText variant="headlineMedium">Notifications</PaperText>
          {unreadCount > 0 && (
            <View style={styles.unreadCountBadge}>
              <PaperText variant="labelSmall" style={styles.unreadCountText}>
                {unreadCount}
              </PaperText>
            </View>
          )}
        </View>
        <View style={styles.headerRight}>
          <TouchableOpacity onPress={handleBulkMarkAsRead}>
            <PaperText variant="labelSmall" style={styles.markAllButton}>
              Mark All as Read
            </PaperText>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.filterContainer}>
        <View style={styles.filterSection}>
          <PaperText variant="labelSmall" style={styles.filterLabel}>Type:</PaperText>
          <View style={styles.filterButtons}>
            <TouchableOpacity
              style={[styles.filterButton, filter === 'all' && styles.filterButtonActive]}
              onPress={() => setFilter('all')}
            >
              <PaperText style={styles.filterText}>All</PaperText>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.filterButton, filter === 'approval' && styles.filterButtonActive]}
              onPress={() => setFilter('approval')}
            >
              <PaperText style={styles.filterText}>Approval</PaperText>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.filterButton, filter === 'expense' && styles.filterButtonActive]}
              onPress={() => setFilter('expense')}
            >
              <PaperText style={styles.filterText}>Expense</PaperText>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.filterButton, filter === 'system' && styles.filterButtonActive]}
              onPress={() => setFilter('system')}
            >
              <PaperText style={styles.filterText}>System</PaperText>
            </TouchableOpacity>
          </View>
        </View>
        <View style={styles.filterSection}>
          <PaperText variant="labelSmall" style={styles.filterLabel}>Show:</PaperText>
          <View style={styles.toggleButtons}>
            <TouchableOpacity
              style={[styles.toggleButton, showRead && styles.toggleButtonActive]}
              onPress={() => setShowRead(true)}
            >
              <PaperText style={styles.toggleText}>Unread Only</PaperText>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.toggleButton, !showRead && styles.toggleButtonActive]}
              onPress={() => setShowRead(false)}
            >
              <PaperText style={styles.toggleText}>All</PaperText>
            </TouchableOpacity>
          </View>
        </View>
      </View>

      <FlatList
        data={notifications}
        renderItem={renderNotificationItem}
        keyExtractor={(item) => item.notificationId.toString()}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={isLoading} onRefresh={() => dispatch(fetchNotifications())} />}
        ListEmptyComponent={
          <View style={styles.empty}>
            <PaperText variant="bodyMedium" style={styles.emptyText}>
              No notifications
            </PaperText>
            {(filter !== 'all' || !showRead) && (
              <View style={styles.emptyHint}>
                <PaperText variant="caption" style={styles.emptyHintText}>
                  Try adjusting filters to see more notifications
                </PaperText>
              </View>
            )}
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
            buttonColor="#2196f3"
            onPress={handleBulkMarkAsRead}
            style={styles.bulkButton}
          >
            Mark Selected as Read
          </Button>
          <Button
            mode="outlined"
            onPress={() => setSelected([])}
            style={styles.bulkButton}
          >
            Clear Selection
          </Button>
        </View>
      )}

      {offlineMode && (
        <View style={styles.offlineBanner}>
          <PaperText variant="labelSmall" style={styles.offlineText}>
            📶 Offline - Notifications will sync when online
          </PaperText>
        </View>
      )}

      <Fab
        icon="refresh"
        label="Refresh"
        style={styles.fab}
        onPress={() => dispatch(fetchNotifications())}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  headerRight: {
    flexDirection: 'row',
  },
  unreadCountBadge: {
    backgroundColor: '#f44336',
    borderRadius: 12,
    paddingHorizontal: 8,
    paddingVertical: 4,
    minWidth: 24,
    textAlign: 'center',
  },
  unreadCountText: {
    color: '#fff',
    fontWeight: '600',
    fontSize: 12,
  },
  filterContainer: {
    padding: 16,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  filterSection: {
    marginBottom: 12,
  },
  filterLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 4,
  },
  filterButtons: {
    flexDirection: 'row',
  },
  filterButton: {
    flex: 1,
    padding: 10,
    alignItems: 'center',
    borderRadius: 6,
  },
  filterButtonActive: {
    backgroundColor: '#e3f2fd',
  },
  filterText: {
    color: '#1976d2',
    fontSize: 14,
    fontWeight: '500',
  },
  toggleButtons: {
    flexDirection: 'row',
    gap: 8,
  },
  toggleButton: {
    padding: 8,
    borderRadius: 6,
    backgroundColor: '#f5f5f5',
  },
  toggleButtonActive: {
    backgroundColor: '#e3f2fd',
  },
  toggleText: {
    fontSize: 12,
    color: '#666',
  },
  list: {
    padding: 16,
  },
  empty: {
    alignItems: 'center',
    padding: 40,
  },
  emptyText: {
    color: '#999',
    fontSize: 16,
    marginBottom: 16,
  },
  emptyHint: {
    marginTop: 8,
  },
  emptyHintText: {
    color: '#666',
    fontSize: 12,
    textAlign: 'center',
  },
  bulkActionsBar: {
    position: 'absolute',
    bottom: 20,
    left: 20,
    right: 20,
    backgroundColor: '#fff',
    borderRadius: 12,
    elevation: 3,
    padding: 16,
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  selectedCount: {
    color: '#2196f3',
    fontWeight: 'bold',
  },
  bulkButton: {
    marginRight: 8,
  },
  fab: {
    position: 'absolute',
    right: 16,
    bottom: 16,
  },
  notificationCard: {
    marginBottom: 12,
    backgroundColor: '#fff',
    borderRadius: 12,
    elevation: 3,
  },
  notificationHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  notificationIconContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  notificationBody: {
    marginVertical: 8,
  },
  notificationData: {
    marginTop: 4,
  },
  notificationFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  notificationTime: {
    fontSize: 12,
    color: '#666',
  },
  notificationActions: {
    flexDirection: 'row',
    gap: 8,
  },
  markReadButton: {
    color: '#2196f3',
    fontSize: 12,
  },
  clearButton: {
    color: '#9e9e9e',
    fontSize: 12,
  },
  offlineBanner: {
    backgroundColor: '#ff9800',
    padding: 12,
    alignItems: 'center',
  },
  offlineText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '500',
  },
});

export default NotificationsScreen;
