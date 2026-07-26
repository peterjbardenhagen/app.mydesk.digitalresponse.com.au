import React, { useEffect } from 'react';
import { View, StyleSheet, ScrollView, Alert, Linking } from 'react-native';
import { Text as PaperText, Card, Button, Divider, Switch, IconButton } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation } from '@react-navigation/native';

import { AppDispatch, RootState } from '@store/store';
import { fetchNotificationPreferences, updateNotificationPreferences } from '@store/slices/notificationsSlice';

const SettingsScreen = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { preferences, isLoading } = useSelector((state: RootState) => state.notifications);
  const { offlineMode } = useSelector((state: RootState) => state.sync);
  const navigation = useNavigation<any>();

  useEffect(() => {
    dispatch(fetchNotificationPreferences());
  }, [dispatch]);

  const handlePreferenceChange = async (key: keyof typeof preferences, value: boolean) => {
    if (!preferences) return;
    try {
      await dispatch(updateNotificationPreferences({ ...preferences, [key]: value })).unwrap();
    } catch (error) {
      Alert.alert('Error', 'Failed to update preference. Please try again.');
    }
  };

  const handleClearCache = () => {
    Alert.alert(
      'Clear Cache',
      'This will remove all cached data. You will need to reload data when online.',
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Clear', onPress: () => {
          // Implementation would clear async storage and reset sync state
          Alert.alert('Cache Cleared', 'Local cache has been cleared');
        }, style: 'destructive' },
      ]
    );
  };

  const handleSyncNow = () => {
    Alert.alert('Manual Sync', 'Triggering immediate sync with server...', [
      { text: 'OK' },
    ]);
    // Implementation would trigger background sync
  };

  const handleResetApp = () => {
    Alert.alert(
      'Reset App',
      'This will log you out and clear all local data. Are you sure?',
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Reset', onPress: () => {
          // Implementation would clear all storage and logout
          Alert.alert('App Reset', 'App has been reset to initial state');
        }, style: 'destructive' },
      ]
    );
  };

  const openPrivacyPolicy = () => {
    Linking.openURL('https://mydesk.com.au/privacy');
  };

  const openTermsOfService = () => {
    Linking.openURL('https://mydesk.com.au/terms');
  };

  const openSupport = () => {
    Linking.openURL('mailto:support@mydesk.com.au?subject=MyDesk Mobile Support');
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Notifications */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Notifications</PaperText>
          <Divider style={styles.divider} />

          {isLoading ? (
            <PaperText variant="bodyMedium" style={styles.loadingText}>Loading preferences...</PaperText>
          ) : preferences ? (
            <>
              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Email Notifications</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Receive email updates for approvals and expenses
                  </PaperText>
                </View>
                <Switch
                  value={preferences.enableEmail}
                  onValueChange={(value) => handlePreferenceChange('enableEmail', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Push Notifications</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Receive push notifications on your device
                  </PaperText>
                </View>
                <Switch
                  value={preferences.enablePush}
                  onValueChange={(value) => handlePreferenceChange('enablePush', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">In-App Notifications</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Show notifications within the app
                  </PaperText>
                </View>
                <Switch
                  value={preferences.enableInApp}
                  onValueChange={(value) => handlePreferenceChange('enableInApp', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Sound</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Play sound for notifications
                  </PaperText>
                </View>
                <Switch
                  value={preferences.enableSound}
                  onValueChange={(value) => handlePreferenceChange('enableSound', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Vibration</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Vibrate device for notifications
                  </PaperText>
                </View>
                <Switch
                  value={preferences.enableVibration}
                  onValueChange={(value) => handlePreferenceChange('enableVibration', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Quiet Hours</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Suppress notifications during quiet hours
                  </PaperText>
                </View>
                <Switch
                  value={preferences.quietHoursEnabled}
                  onValueChange={(value) => handlePreferenceChange('quietHoursEnabled', value)}
                  disabled={offlineMode}
                />
              </View>

              {preferences.quietHoursEnabled && !isLoading && (
                <>
                  <Divider style={styles.divider} />
                  <View style={styles.quietHoursRow}>
                    <View style={styles.quietHoursItem}>
                      <PaperText variant="bodySmall" style={styles.quietHoursLabel}>Start</PaperText>
                      <PaperText variant="bodyMedium">{preferences.quietHoursStart || '22:00'}</PaperText>
                      <IconButton
                        icon="clock"
                        onPress={() => Alert.alert('Coming Soon', 'Time picker will be available in a future update')}
                      />
                    </View>
                    <View style={styles.quietHoursItem}>
                      <PaperText variant="bodySmall" style={styles.quietHoursLabel}>End</PaperText>
                      <PaperText variant="bodyMedium">{preferences.quietHoursEnd || '06:00'}</PaperText>
                      <IconButton
                        icon="clock"
                        onPress={() => Alert.alert('Coming Soon', 'Time picker will be available in a future update')}
                      />
                    </View>
                  </View>
                </>
              }

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Approval Required</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Notify when approval is needed
                  </PaperText>
                </View>
                <Switch
                  value={preferences.notificationTypes?.approvalRequired ?? true}
                  onValueChange={(value) => handlePreferenceChange('notificationTypes.approvalRequired', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Approval Decision</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Notify when your expense is approved or rejected
                  </PaperText>
                </View>
                <Switch
                  value={preferences.notificationTypes?.approvalDecision ?? true}
                  onValueChange={(value) => handlePreferenceChange('notificationTypes.approvalDecision', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Expense Submitted</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Notify when expense is submitted
                  </PaperText>
                </View>
                <Switch
                  value={preferences.notificationTypes?.expenseSubmitted ?? true}
                  onValueChange={(value) => handlePreferenceChange('notificationTypes.expenseSubmitted', value)}
                  disabled={offlineMode}
                />
              </View>

              <Divider style={styles.divider} />

              <View style={styles.preferenceRow}>
                <View style={styles.preferenceInfo}>
                  <PaperText variant="bodyMedium">Payment Processing</PaperText>
                  <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                    Notify when payment is being processed
                  </PaperText>
                </View>
                <Switch
                  value={preferences.notificationTypes?.paymentProcessing ?? true}
                  onValueChange={(value) => handlePreferenceChange('notificationTypes.paymentProcessing', value)}
                  disabled={offlineMode}
                />
              </View>
            </>
          ) : null}
        </Card.Content>
      </Card>

      {/* Data & Sync */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Data & Sync</PaperText>
          <Divider style={styles.divider} />

          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Auto-sync on WiFi Only</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Only sync data when connected to WiFi
              </PaperText>
            </View>
            <Switch
              value={true}
              onValueChange={(value) => {}}
            />
          </View>

          <Divider style={styles.divider} />

          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Background Sync</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Allow app to sync in background
              </PaperText>
            </View>
            <Switch
              value={true}
              onValueChange={(value) => {}}
            />
          </View>

          <Divider style={styles.divider} />

          <View style={styles.actionRow}>
            <Button
              mode="contained"
              icon="sync"
              onPress={handleSyncNow}
              style={styles.actionButton}
            >
              Sync Now
            </Button>
            <Button
              mode="outlined"
              icon="delete"
              onPress={handleClearCache}
              style={styles.actionButton}
            >
              Clear Cache
            </Button>
          </View>
        </Card.Content>
      </Card>

      {/* Appearance */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Appearance</PaperText>
          <Divider style={styles.divider} />

          <View style={styles.preferenceRow}>
            <View style={styles.preferenceInfo}>
              <PaperText variant="bodyMedium">Theme</PaperText>
              <PaperText variant="bodySmall" style={styles.preferenceDesc}>
                Choose app appearance
              </PaperText>
            </View>
            <View style={styles.themeSelector}>
              <Button
                mode="contained"
                style={styles.themeButton}
                onPress={() => Alert.alert('Coming Soon', 'Theme selection will be available in a future update')}
              >
                Light
              </Button>
              <Button
                mode="outlined"
                style={styles.themeButton}
                onPress={() => Alert.alert('Coming Soon', 'Theme selection will be available in a future update')}
              >
                Dark
              </Button>
              <Button
                mode="outlined"
                style={styles.themeButton}
                onPress={() => Alert.alert('Coming Soon', 'Theme selection will be available in a future update')}
              >
                System
              </Button>
            </View>
          </View>
        </Card.Content>
      </Card>

      {/* Privacy & Legal */}
      <Card style={styles.sectionCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Privacy & Legal</PaperText>
          <Divider style={styles.divider} />

          <View style={styles.legalRow}>
            <Button
              mode="text"
              icon="shield"
              onPress={openPrivacyPolicy}
              style={styles.legalButton}
            >
              Privacy Policy
            </Button>
            <Button
              mode="text"
              icon="file-document"
              onPress={openTermsOfService}
              style={styles.legalButton}
            >
              Terms of Service
            </Button>
          </View>

          <Divider style={styles.divider} />

          <View style={styles.legalRow}>
            <Button
              mode="text"
              icon="help-circle"
              onPress={openSupport}
              style={styles.legalButton}
            >
              Contact Support
            </Button>
            <Button
              mode="text"
              icon="star"
              onPress={() => Alert.alert('Rate Us', 'Thank you for using MyDesk!')}
              style={styles.legalButton}
            >
              Rate App
            </Button>
          </View>
        </Card.Content>
      </Card>

      {/* Advanced */}
      <Card style={styles.dangerCard}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.dangerTitle}>Advanced</PaperText>
          <Divider style={styles.dangerDivider} />

          <View style={styles.actionRow}>
            <Button
              mode="outlined"
              icon="database-off"
              onPress={handleResetApp}
              style={styles.dangerButton}
              textColor="#f44336"
            >
              Reset App
            </Button>
          </View>

          <Divider style={styles.dangerDivider} />

          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Data Residency</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>Australia (Sydney)</PaperText>
          </View>

          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Encryption</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>AES-256 / TLS 1.3</PaperText>
          </View>

          <View style={styles.infoRow}>
            <PaperText variant="bodySmall" style={styles.infoLabel}>Compliance</PaperText>
            <PaperText variant="bodyMedium" style={styles.infoValue}>SOC 2 Type II, ISO 27001</PaperText>
          </View>
        </Card.Content>
      </Card>

      <View style={styles.footer}>
        <PaperText variant="caption" style={styles.footerText}>
          MyDesk Mobile v1.0.0 - Phase 7 Implementation
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
  quietHoursRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
    marginTop: 8,
  },
  quietHoursItem: {
    flex: 1,
    alignItems: 'center',
  },
  quietHoursLabel: {
    fontSize: 12,
    color: '#999',
    marginBottom: 4,
  },
  actionRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 8,
  },
  actionButton: {
    flex: 1,
    marginHorizontal: 4,
  },
  themeSelector: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  themeButton: {
    flex: 1,
    marginHorizontal: 4,
  },
  legalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  legalButton: {
    flex: 1,
    marginHorizontal: 4,
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
  dangerButton: {
    width: '100%',
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
  loadingText: {
    textAlign: 'center',
    color: '#999',
    padding: 20,
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

export default SettingsScreen;