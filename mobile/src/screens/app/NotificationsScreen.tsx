import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text as PaperText } from 'react-native-paper';

const NotificationsScreen = () => (
  <View style={styles.container}>
    <PaperText variant="headlineMedium">NotificationsScreen</PaperText>
    <PaperText variant="bodyMedium" style={styles.placeholder}>Phase 7 implementation in progress</PaperText>
  </View>
);

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, justifyContent: 'center' },
  placeholder: { marginVertical: 16, color: '#666' },
});

export default NotificationsScreen;
