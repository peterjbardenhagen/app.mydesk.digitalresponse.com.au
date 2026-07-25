import React, { useEffect } from 'react';
import { View, StyleSheet, FlatList, RefreshControl } from 'react-native';
import { Text as PaperText, Card, Button, ProgressBar, Chip, Divider } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation, useRoute } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import {
  fetchNotifications,
  fetchNotificationPreferences,
} from '@store/slices/notificationsSlice';
import { Ionicons } from '@expo/vector-icons';

const NotificationsScreen: React.FC<any> = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { items: notifications, isLoading, unreadCount } = useSelector((state: RootState) => state.notifications);
  const { user } = useSelector((state: RootState) => state.auth);

  useEffect(() => {
    dispatch(fetchNotifications());
    dispatch(fetchNotificationPreferences());
  }, [dispatch]);

  const renderNotificationItem = (item: any) => {
    const isRead = item.isRead;
    const type = item.type;
    const title = item.title;
    const body = item.body;

    const getTypeColor = () => {
      switch (type) {
        case 'ApprovalRequired':
          return '#ff9800';
        case 'ApprovalDecision':
          return '#4caf50';
        case 'ExpenseSubmitted':
          return '#4caf50';
        case 'PaymentProcessing':
          return '#2196f3';
        case 'System':
          return '#607d8b';
        case 'Alert':
          return '#f44336';
        default:
          return '#9e9e9e';
      }
    };

    const getIcon = () => {
      switch (type) {
        case 'ApprovalRequired':
          return 'alert-circle';
        case 'ApprovalDecision':
          return 'checkmark-circle';
        case 'ExpenseSubmitted':
          return 'receipt';
        case 'PaymentProcessing':
          return 'cash';
        case 'System':
          return 'settings';
        case 'Alert':
          return 'alert-circle';
        default:
          return 'information-circle';
      }
    };

    return (
      <Card style={styles.notificationCard}>
        <Card.Content onPress={() => {
          if (item.deepLink && item.deepLink.includes('/expenses/')) {
            const expenseId = item.deepLink.split('/')[2];
            navigation.navigate('ExpenseDetail', { expenseId: parseInt(expenseId) });
          }
        }}>
          <View style={styles.notificationHeader}>
            <View style={styles.notificationIconContainer}>
              <Ionicons name={getIcon()} size={24} color={getTypeColor()} />
            </View>
            <View style={styles.notificationInfo}>
              <PaperText variant="titleSmall" style={styles.title}>{title}</PaperText>
            </View>
            <View style={styles.notificationTime}>
              <PaperText variant="labelSmall" style={styles.time}>
                {item.createdAt ? new Date(item.createdAt).toLocaleTimeString() : 'Just now'}
              </PaperText>
            </View>
          </View>

          <PaperText variant="bodySmall" style={styles.body}>
            {body}
          </PaperText>

          <View style={styles.notificationFooter}>
            <Chip style={styles.typeChip} textStyle={{ fontSize: 12 }}
              chipColor={getTypeColor()}>
              {type}
            </Chip>
            <View style={styles.typeBadge}>
              {isRead ? 'Read' : 'Unread'}
            </View>
          </View>
        </View>
      </Card>
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <PaperText variant="headlineMedium">Notifications</PaperText>
        {unreadCount > 0 && (
          <View style={styles.unreadBadge}>
            <PaperText variant="labelSmall" style={styles.unreadBadgeText}>
              {unreadCount} unread
            </PaperText>
          </View>
        )}
      </View>

      <FlatList
        data={notifications}
        renderItem={({ item }) => renderNotificationItem(item)}
        keyExtractor={(item) => item.notificationId.toString()}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={isLoading} onRefresh={() => dispatch(fetchNotifications())} />}
        ListEmptyComponent={
          <View style={styles.empty}>
            <PaperText variant="bodyMedium} style={styles.emptyText}>
              No notifications
            </PaperText>
          </View>
        }
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
  unreadBadge: {
    backgroundColor: '#f44336',
    borderRadius: 12,
    paddingHorizontal: 8,
    paddingVertical: 4,
    alignSelf: 'flex-start',
    marginTop: 8,
  },
  unreadBadgeText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 12,
  },
  notificationCard: {
    marginBottom: 12,
    backgroundColor: '#fff',
    borderRadius: 12,
    elevation: 2,
  },
  notificationHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  notificationIconContainer: {
    marginRight: 12,
  },
  notificationInfo: {
    flex: 1,
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  body: {
    fontSize: 14,
    color: '#666',
    marginTop: 4,
  },
  typeChip: {
    height: 28,
    marginTop: 8,
  },
  typeBadge: {
    marginTop: 8,
    paddingHorizontal: 8,
    paddingVertical: 4,
    backgroundColor: '#f5f5f5',
    borderRadius: 4,
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
    fontSize: 16,
  },
});

export default NotificationsScreen;