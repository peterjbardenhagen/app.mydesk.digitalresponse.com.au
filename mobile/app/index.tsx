/**
 * Root Navigation Setup
 * Handles auth stack vs app stack navigation
 * Part of Phase 7: Mobile Applications - Task 4 (Navigation Architecture)
 */

import React, { useEffect } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useSelector, useDispatch } from 'react-redux';
import { ActivityIndicator, View } from 'react-native';

import { RootState, AppDispatch } from '@store/store';
import { getCurrentUser } from '@store/slices/authSlice';

import LoginScreen from '@screens/auth/LoginScreen';
import SignupScreen from '@screens/auth/SignupScreen';
import ForgotPasswordScreen from '@screens/auth/ForgotPasswordScreen';

import DashboardScreen from '@screens/app/DashboardScreen';
import ExpensesScreen from '@screens/app/ExpensesScreen';
import ExpenseDetailScreen from '@screens/app/ExpenseDetailScreen';
import CreateExpenseScreen from '@screens/app/CreateExpenseScreen';
import ApprovalsScreen from '@screens/app/ApprovalsScreen';
import ApprovalDetailScreen from '@screens/app/ApprovalDetailScreen';
import NotificationsScreen from '@screens/app/NotificationsScreen';
import ProfileScreen from '@screens/app/ProfileScreen';
import SettingsScreen from '@screens/app/SettingsScreen';

const Stack = createNativeStackNavigator();

const RootNavigator: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { isAuthenticated, isLoading } = useSelector((state: RootState) => state.auth);

  useEffect(() => {
    // Check if user is already logged in
    dispatch(getCurrentUser());
  }, [dispatch]);

  if (isLoading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Stack.Navigator
        screenOptions={{
          headerShown: false,
        }}
      >
        {!isAuthenticated ? (
          // Auth Stack
          <Stack.Group>
            <Stack.Screen
              name="Login"
              component={LoginScreen}
              options={{ animationEnabled: false }}
            />
            <Stack.Screen
              name="Signup"
              component={SignupScreen}
              options={{
                animationEnabled: true,
                cardStyle: { backgroundColor: '#fff' },
              }}
            />
            <Stack.Screen
              name="ForgotPassword"
              component={ForgotPasswordScreen}
              options={{
                animationEnabled: true,
                cardStyle: { backgroundColor: '#fff' },
              }}
            />
          </Stack.Group>
        ) : (
          // App Stack
          <Stack.Group>
            <Stack.Screen
              name="Dashboard"
              component={DashboardScreen}
              options={{ animationEnabled: false }}
            />
            <Stack.Screen name="Expenses" component={ExpensesScreen} />
            <Stack.Screen name="ExpenseDetail" component={ExpenseDetailScreen} />
            <Stack.Screen name="CreateExpense" component={CreateExpenseScreen} />
            <Stack.Screen name="Approvals" component={ApprovalsScreen} />
            <Stack.Screen name="ApprovalDetail" component={ApprovalDetailScreen} />
            <Stack.Screen name="Notifications" component={NotificationsScreen} />
            <Stack.Screen name="Profile" component={ProfileScreen} />
            <Stack.Screen name="Settings" component={SettingsScreen} />
          </Stack.Group>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
};

export default RootNavigator;
