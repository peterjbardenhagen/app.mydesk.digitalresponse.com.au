import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text as PaperText, Button } from 'react-native-paper';

const ForgotPasswordScreen = ({ navigation }: any) => (
  <View style={styles.container}>
    <PaperText variant="headlineMedium">Forgot Password</PaperText>
    <PaperText variant="bodyMedium" style={styles.placeholder}>
      Password reset flow to be implemented
    </PaperText>
    <Button mode="text" onPress={() => navigation.navigate('Login')}>
      Back to Login
    </Button>
  </View>
);

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, justifyContent: 'center' },
  placeholder: { marginVertical: 16, color: '#666' },
});

export default ForgotPasswordScreen;
