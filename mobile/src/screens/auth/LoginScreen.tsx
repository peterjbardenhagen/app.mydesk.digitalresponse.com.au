/**
 * Login Screen
 * User authentication entry point
 * Part of Phase 7: Mobile Applications - Task 7 (Authentication Flow)
 */

import React, { useState } from 'react';
import { View, StyleSheet, ScrollView, KeyboardAvoidingView, Platform } from 'react-native';
import { TextInput, Button, Text as PaperText, ActivityIndicator } from 'react-native-paper';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useDispatch, useSelector } from 'react-redux';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { login } from '@store/slices/authSlice';
import { AppDispatch, RootState } from '@store/store';

type LoginScreenProps = {
  navigation: NativeStackNavigationProp<any>;
};

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginFormData = z.infer<typeof loginSchema>;

const LoginScreen: React.FC<LoginScreenProps> = ({ navigation }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { isLoading, error } = useSelector((state: RootState) => state.auth);
  const [showPassword, setShowPassword] = useState(false);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await dispatch(login(data)).unwrap();
    } catch (err) {
      // Error handled by Redux state
    }
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      style={styles.container}
    >
      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.header}>
          <PaperText variant="displayMedium" style={styles.title}>
            MyDesk
          </PaperText>
          <PaperText variant="bodyMedium" style={styles.subtitle}>
            Expense Management
          </PaperText>
        </View>

        <View style={styles.form}>
          {error && (
            <View style={styles.errorContainer}>
              <PaperText style={styles.errorText}>{error}</PaperText>
            </View>
          )}

          <Controller
            control={control}
            name="email"
            render={({ field: { value, onChange } }) => (
              <TextInput
                label="Email Address"
                value={value}
                onChangeText={onChange}
                mode="outlined"
                keyboardType="email-address"
                autoCapitalize="none"
                style={styles.input}
                error={!!errors.email}
                editable={!isLoading}
              />
            )}
          />
          {errors.email && (
            <PaperText style={styles.fieldErrorText}>{errors.email.message}</PaperText>
          )}

          <Controller
            control={control}
            name="password"
            render={({ field: { value, onChange } }) => (
              <TextInput
                label="Password"
                value={value}
                onChangeText={onChange}
                mode="outlined"
                secureTextEntry={!showPassword}
                style={styles.input}
                error={!!errors.password}
                right={
                  <TextInput.Icon
                    icon={showPassword ? 'eye-off' : 'eye'}
                    onPress={() => setShowPassword(!showPassword)}
                  />
                }
                editable={!isLoading}
              />
            )}
          />
          {errors.password && (
            <PaperText style={styles.fieldErrorText}>{errors.password.message}</PaperText>
          )}

          <Button
            mode="contained"
            onPress={handleSubmit(onSubmit)}
            style={styles.loginButton}
            loading={isLoading}
            disabled={isLoading}
          >
            {isLoading ? 'Logging in...' : 'Log In'}
          </Button>

          <PaperText
            style={styles.forgotPassword}
            onPress={() => navigation.navigate('ForgotPassword')}
          >
            Forgot Password?
          </PaperText>
        </View>

        <View style={styles.footer}>
          <PaperText style={styles.footerText}>Don't have an account? </PaperText>
          <PaperText
            style={[styles.footerText, styles.signupLink]}
            onPress={() => navigation.navigate('Signup')}
          >
            Sign Up
          </PaperText>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: 20,
    paddingVertical: 40,
    justifyContent: 'space-between',
  },
  header: {
    alignItems: 'center',
    marginBottom: 40,
  },
  title: {
    fontWeight: 'bold',
    marginBottom: 8,
  },
  subtitle: {
    color: '#666',
  },
  form: {
    width: '100%',
  },
  input: {
    marginBottom: 12,
  },
  fieldErrorText: {
    color: '#d32f2f',
    fontSize: 12,
    marginBottom: 8,
  },
  errorContainer: {
    backgroundColor: '#ffebee',
    borderRadius: 4,
    padding: 12,
    marginBottom: 16,
  },
  errorText: {
    color: '#d32f2f',
  },
  loginButton: {
    marginTop: 20,
    paddingVertical: 8,
  },
  forgotPassword: {
    marginTop: 16,
    textAlign: 'center',
    color: '#1976d2',
    textDecorationLine: 'underline',
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 20,
  },
  footerText: {
    color: '#666',
  },
  signupLink: {
    color: '#1976d2',
    fontWeight: 'bold',
  },
});

export default LoginScreen;
