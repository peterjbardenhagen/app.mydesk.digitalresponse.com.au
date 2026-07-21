/**
 * Signup Screen
 * User registration
 * Part of Phase 7: Mobile Applications
 */

import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text as PaperText, Button } from 'react-native-paper';

const SignupScreen = ({ navigation }: any) => {
  return (
    <View style={styles.container}>
      <PaperText variant="headlineMedium">Sign Up</PaperText>
      <PaperText variant="bodyMedium" style={styles.placeholder}>
        Signup flow to be implemented (Task 21 - Foundation)
      </PaperText>
      <Button mode="text" onPress={() => navigation.navigate('Login')}>
        Back to Login
      </Button>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    justifyContent: 'center',
  },
  placeholder: {
    marginVertical: 16,
    color: '#666',
  },
});

export default SignupScreen;
