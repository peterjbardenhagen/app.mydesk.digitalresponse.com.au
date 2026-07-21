/**
 * Network connectivity hook (Task 91)
 * Monitors online/offline and reflects into the sync slice.
 */

import { useEffect } from 'react';
import { AppState } from 'react-native';
import NetInfo from '@react-native-community/netinfo';
import { useDispatch, useSelector } from 'react-redux';
import { AppDispatch, RootState } from '@store/store';
import { setOfflineMode } from '@store/slices/syncSlice';

export function useNetwork(): boolean {
  const dispatch = useDispatch<AppDispatch>();
  const offlineMode = useSelector((state: RootState) => state.sync.offlineMode);

  useEffect(() => {
    const unsub = NetInfo.addEventListener((state) => {
      dispatch(setOfflineMode(!state.isConnected));
    });
    AppState.addEventListener('change', () => {
      NetInfo.fetch().then((state) => dispatch(setOfflineMode(!state.isConnected)));
    });
    return () => unsub();
  }, [dispatch]);

  return offlineMode;
}
