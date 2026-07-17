import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text as PaperText } from 'react-native-paper';

const ExpenseDetailScreen = () => (
  <View style={styles.container}>
    <PaperText variant="headlineMedium">Expense Detail</PaperText>
    <PaperText variant="bodyMedium" style={styles.placeholder}>Task 22 - To be implemented</PaperText>
  </View>
);

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, justifyContent: 'center' },
  placeholder: { marginVertical: 16, color: '#666' },
});

export default ExpenseDetailScreen;
