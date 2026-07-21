/**
 * Offline queue helper (Tasks 12, 94, 100)
 * Wraps syncSlice actions and produces a durable OfflineQueue entry.
 */

import { v4 as uuidv4 } from 'uuid';
import { AppDispatch } from '@store/store';
import { addToQueue } from '@store/slices/syncSlice';
import { OfflineQueue } from '@types';

export function enqueueOffline(
  dispatch: AppDispatch,
  entry: Omit<OfflineQueue, 'id' | 'createdAt' | 'status' | 'retryCount'>
): OfflineQueue {
  const item: OfflineQueue = {
    ...entry,
    id: uuidv4(),
    createdAt: new Date().toISOString(),
    status: 'Pending',
    retryCount: 0,
  };
  dispatch(addToQueue(item));
  return item;
}
