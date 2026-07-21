/**
 * Receipt camera capture (Task 24)
 * Uses expo-camera / expo-image-picker. Falls back to library if no camera.
 */

import * as ImagePicker from 'expo-image-picker';
import * as MediaLibrary from 'expo-media-library';

export interface CapturedImage {
  uri: string;
  width?: number;
  height?: number;
  cancelled?: boolean;
}

export async function captureReceipt(): Promise<CapturedImage> {
  const { status } = await ImagePicker.requestCameraPermissionsAsync();
  if (status !== 'granted') {
    // Fall back to library selection
    return pickFromLibrary();
  }
  const result = await ImagePicker.launchCameraAsync({
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    quality: 0.7,
    allowsEditing: true,
    aspect: [4, 3],
  });
  if (result.canceled) return { uri: '', cancelled: true };
  const asset = result.assets[0];
  return { uri: asset.uri, width: asset.width, height: asset.height };
}

export async function pickFromLibrary(): Promise<CapturedImage> {
  const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (status !== 'granted') return { uri: '', cancelled: true };
  const result = await ImagePicker.launchImageLibraryAsync({
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    quality: 0.7,
    allowsEditing: true,
    aspect: [4, 3],
  });
  if (result.canceled) return { uri: '', cancelled: true };
  const asset = result.assets[0];
  return { uri: asset.uri, width: asset.width, height: asset.height };
}
