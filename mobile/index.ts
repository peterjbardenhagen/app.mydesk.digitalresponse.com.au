/**
 * App entry point
 * Initializes Redux store, persistence, and root navigation
 * Part of Phase 7: Mobile Applications
 */

import * as SplashScreen from 'expo-splash-screen';
import React, { useEffect } from 'react';
import { PaperProvider } from 'react-native-paper';
import { Provider as ReduxProvider } from 'react-redux';
import { PersistGate } from 'redux-persist/integration/react';
import * as Notifications from 'expo-notifications';

import RootNavigator from './app';
import { store, persistor } from '@store/store';
import { ActivityIndicator, View } from 'react-native';

// Keep splash screen visible while loading
SplashScreen.preventAutoHideAsync();

// Configure notifications
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

const App: React.FC = () => {
  useEffect(() => {
    // Hide splash screen after store is rehydrated
    const timer = setTimeout(() => {
      SplashScreen.hideAsync();
    }, 1000);

    return () => clearTimeout(timer);
  }, []);

  return (
    <ReduxProvider store={store}>
      <PersistGate
        loading={
          <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
            <ActivityIndicator size="large" />
          </View>
        }
        persistor={persistor}
      >
        <PaperProvider>
          <RootNavigator />
        </PaperProvider>
      </PersistGate>
    </ReduxProvider>
  );
};

export default App;
