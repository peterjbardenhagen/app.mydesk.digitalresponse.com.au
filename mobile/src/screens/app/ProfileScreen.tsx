import React, { useEffect } from 'react';
import { View, StyleSheet, ScrollView, Image, Alert, Linking } from 'react-native';
import { Text as PaperText, Card, Button, Divider, Chip, Avatar, Switch } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import { logout } from '@store/slices/authSlice';

const ProfileScreen = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { user, isAuthenticated } = useSelector((state: RootState) => state.auth);
  const { offlineMode } = useSelector((state: RootState) => state.sync);
  const navigation = useNavigation<any>();

  const handleLogout = () => {
    Alert.alert('Logout', 'Are you sure you want to logout?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Logout', onPress: () => dispatch(logout()), style: 'destructive' },
    ]);
  };

  const handleEditProfile = () => {
    navigation.navigate('EditProfile');
  };

  const handleChangePassword = () => {
    navigation.navigate('ChangePassword');
  };

  const handleNotificationSettings = () => {
    navigation.navigate('Notifications');
  };

  const getInitials = (firstName?: string, lastName?: string) => {
    return `${firstName?.[0] || ''}${lastName?.[0] || ''}`.toUpperCase() || 'U';
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-AU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  if (!isAuthenticated || !user) {
    return (
      <View style={styles.container}>
        <PaperText variant="bodyMedium" style={styles.centeredText}>
          Please log in to view your profile
        </PaperText>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Profile Header */}
      <View style={styles.headerCard}>
        <View style={styles.headerContent}>
          <Avatar.Image
            size={80}
            source={user.profilePhotoUrl ? { uri: user.profilePhotoUrl } : undefined}
            style={styles.avatar}
          >
            {getInitials(user.firstName, user.lastName)}
          </Avatar.Image>
          <View style={styles.nameContainer}>
            <PaperText variant="headlineSmall" style={styles.name}>
              {user.firstName} {user.lastName}
            </PaperText>
            <PaperText variant="bodyMedium" style={styles.email}>
              {user.email}
            </PaperText>
          </View>
        </View>
        <View style={styles.badgeContainer}>
          <Chip
            mode="contained"
            textStyle={{ fontSize: 12 }}
            style={styles.roleChip}
          >
            {user.role}
          </Chip>
        </View>
      </View>

      {/* Account Info */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Account Information</PaperText>
          <Divider style={styles.divider} />
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Role</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>{user.role}</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Department</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>{user.department || 'Not assigned'}</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Team</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>{user.team || 'Not assigned'}</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Member Since</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>{formatDate(user.createdAt)}</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Last Login</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>{formatDate(user.lastLoginAt)}</PaperText>
          </View>
        </Card.Content>
      </Card>

      {/* Preferences */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Preferences</PaperText>
          <Divider style={styles.divider} />
          
          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Dark Mode</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Use dark theme for the app
              </PaperText>
            </View>
            <Switch
              value={false}
              onValueChange={() => Alert.alert('Coming Soon', 'Dark mode toggle will be available in a future update')}
            />
          </View>
          
          <Divider style={styles.divider} />
          
          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Push Notifications</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Receive push notifications
              </PaperText>
            </View>
            <Switch
              value={true}
              onValueChange={() => navigation.navigate('Notifications')}
            />
          </View>
          
          <Divider style={styles.divider} />
          
          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Auto-sync</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Automatically sync when online
              </PaperText>
            </View>
            <Switch
              value={!offlineMode}
              onValueChange={() => {}}
              disabled
            />
          </View>
        </Card.Content>
      </Card>

      {/* Quick Actions */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Quick Actions</PaperText>
          <Divider style={styles.divider} />
          
          <Button mode="outlined" icon="account-edit" onPress={handleEditProfile} style={styles.actionButton}>
            Edit Profile
          </Button>
          
          <Button mode="outlined" icon="lock" onPress={handleChangePassword} style={styles.actionButton}>
            Change Password
          </Button>
          
          <Button mode="outlined" icon="bell" onPress={handleNotificationSettings} style={styles.actionButton}>
            Notification Settings
          </Button>
        </Card.Content>
      </Card>

      {/* App Info */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>About</PaperText>
          <Divider style={styles.divider} />
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>App Version</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>1.0.0</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Build</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>Phase 7 - Beta</PaperText>
          </View>
          
          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Platform</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>React Native / Expo</PaperText>
          </View>
        </Card.Content>
      </Card>

      {/* Danger Zone */}
      <Card style={styles.dangerCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.dangerTitle}>Danger Zone</PaperText>
          <Divider style={styles.dangerDivider} />
          <Button mode="text" onPress={handleLogout} style={styles.logoutButton} textColor="#f44336">
            Logout
          </Button>
        </Card.Content>
      </Card>

      <View style={styles.footer}>
        <PaperText variant="caption" style={styles.footerText}>
          MyDesk Mobile - Phase 7 Implementation
        </PaperText>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  content: {
    padding: 16,
    paddingBottom: 40,
  },
  centeredText: {
    textAlign: 'center',
    color: '#999',
  },
  headerCard: {
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
    elevation: 3,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  avatar: {
    marginRight: 16,
  },
  nameContainer: {
    flex: 1,
  },
  name: {
    fontWeight: '600',
    color: '#333',
  },
  email: {
    color: '#666',
    marginTop: 2,
  },
  badgeContainer: {
    alignSelf: 'flex-start',
  },
  roleChip: {
    backgroundColor: '#e3f2fd',
  },
  sectionCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    marginBottom: 16,
    elevation: 1,
  },
  sectionTitle: {
    fontWeight: '600',
    marginBottom: 12,
    color: '#333',
  },
  divider: {
    marginVertical: 12,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
  },
  infoLabel: {
    color: '#666',
  },
  infoValue: {
    fontWeight: '500',
    color: '#333',
  },
  preferenceRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 12,
  },
  preferenceInfo: {
    flex: 1,
  },
  preferenceDesc: {
    color: '#999',
    fontSize: 12,
    marginTop: 2,
  },
  actionButton: {
    marginBottom: 8,
    width: '100%',
  },
  dangerCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    marginBottom: 16,
    elevation: 1,
    borderWidth: 1,
    borderColor: '#ffebee',
  },
  dangerTitle: {
    fontWeight: '600',
    marginBottom: 12,
    color: '#c62828',
  },
  dangerDivider: {
    marginVertical: 12,
    backgroundColor: '#ffcdd2',
  },
  logoutButton: {
    width: '100%',
  },
  footer: {
    marginTop: 24,
    alignItems: 'center',
    paddingBottom: 20,
  },
  footerText: {
    color: '#999',
    fontSize: 12,
  },
});

export default ProfileScreen;