/**
 * Expense Creation Screen
 * Create new expense with camera capture and OCR
 * Part of Phase 7: Mobile Applications - Task 24 (Expense Form Screens)
 */

import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  TextInput,
  Switch,
  Alert,
  ActivityIndicator,
  Image,
  TouchableOpacity,
  Modal,
} from 'react-native';
import {
  Text as PaperText,
  Card,
  Button,
  ActivityIndicator as PaperActivityIndicator,
  Divider,
  Chip,
  FAB,
  Modal,
} from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import * as ImagePicker from 'expo-image-picker';

import { AppDispatch, RootState } from '@store/store';
import {
  createExpense,
  updateExpense,
  getExpenseDetail,
} from '@store/slices/expensesSlice';
import { addToQueue } from '@store/slices/syncSlice';
import { Expense, ReceiptOcrData } from '@types';
import { validateExpenseForm, ExpenseFormErrors } from '@utils/validation';
import { enqueueOffline } from '@services/offlineQueue';
import { captureReceipt } from '@services/camera';

type CreateRoute = RouteProp<
  { CreateExpense: { draftId?: number } },
  'CreateExpense'
>;

const CATEGORIES = [
  'Meals',
  'Accommodation',
  'Transport',
  'Fuel',
  'Office',
  'Travel',
  'Entertainment',
  'Other',
];

const CreateExpenseScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<CreateRoute>();
  const draftId = route.params?.draftId;

  const dispatch = useDispatch<AppDispatch>();
  const { items, currentExpense, isLoading } = useSelector(
    (state: RootState) => state.expenses
  );
  const offlineMode = useSelector((state: RootState) => state.sync.offlineMode);

  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('AUD');
  const [category, setCategory] = useState(CATEGORIES[0]);
  const [description, setDescription] = useState('');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [costCenter, setCostCenter] = useState('');
  const [receiptUri, setReceiptUri] = useState<string | undefined>();
  const [ocr, setOcr] = useState<ReceiptOcrData | undefined>();
  const [ocrLoading, setOcrLoading] = useState(false);
  const [errors, setErrors] = useState<ExpenseFormErrors>({});
  const [savingDraft, setSavingDraft] = useState(false);
  const [cameraPermissionResponse, setCameraPermissionResponse] = useState(false);
  const [isCameraReady, setIsCameraReady] = useState(false);
  const [capturedImage, setCapturedImage] = useState<string | null>(null);
  const [showImagePreview, setShowImagePreview] = useState(false);
  const [showConfirmationModal, setShowConfirmationModal] = useState(false);
  const [submitLoading, setSubmitLoading] = useState(false);

  const isEdit = draftId != null;
  const loadedRef = useRef(false);

  // Request camera permission on mount
  useEffect(() => {
    (async () => {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      setCameraPermissionResponse(status === 'granted');
    })();
  }, []);

  useEffect(() => {
    if (isEdit && draftId && !loadedRef.current) {
      const existing = items.find((e) => e.expenseId === draftId) || currentExpense;
      if (existing) {
        hydrate(existing);
        loadedRef.current = true;
      } else {
        dispatch(getExpenseDetail(draftId));
      }
    }
  }, [isEdit, draftId, items, currentExpense, dispatch]);

  useEffect(() => {
    if (isEdit && currentExpense && currentExpense.expenseId === draftId && !loadedRef.current) {
      hydrate(currentExpense);
      loadedRef.current = true;
    }
  }, [currentExpense, draftId, isEdit]);

  const hydrate = (e: Expense) => {
    setAmount(String(e.amount));
    setCurrency(e.currency);
    setCategory(e.category);
    setDescription(e.description);
    setDate((e.submittedAt || e.createdAt).slice(0, 10));
    setCostCenter(e.costCenter || '');
    setReceiptUri(e.receiptUrl);
    setOcr(e.receiptOcrData);
  };

  const captureImage = async () => {
    try {
      const result = await captureReceipt();
      if (!result.cancelled && result.uri) {
        setCapturedImage(result.uri);
        setShowImagePreview(true);
        await runOcr(result.uri);
      }
    } catch (error) {
      console.error('Camera capture failed:', error);
      Alert.alert('Camera Error', 'Failed to access camera. Please try again or use gallery.');
    }
  };

  const pickFromLibrary = async () => {
    try {
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        quality: 0.7,
        base64: false,
      });
      if (!result.cancelled && result.assets[0]) {
        const uri = result.assets[0].uri;
        setCapturedImage(uri);
        setShowImagePreview(true);
        await runOcr(uri);
      }
    } catch (error) {
      console.error('Image picker error:', error);
      Alert.alert('Gallery Error', 'Failed to access gallery. Please try again.');
    }
  };

  const runOcr = async (uri: string) => {
    setOcrLoading(true);
    try {
      const form = new FormData();
      form.append('file', { uri, name: 'receipt.jpg', type: 'image/jpeg' } as any);
      const res = await fetch(`${process.env.EXPO_PUBLIC_API_BASE_URL}/api/receipts/ocr`, {
        method: 'POST',
        headers: { 'Content-Type': 'multipart/form-data' },
        body: form,
      });
      if (res.ok) {
        const json = await res.json();
        const data: ReceiptOcrData = json.data || json;
        setOcr(data);
        if (data.grossAmount != null) setAmount(String(data.grossAmount));
        if (data.supplierName) setDescription(data.supplierName);
        if (data.currency) setCurrency(data.currency);
      }
    } catch {
      // OCR is best-effort; user can edit fields manually
    } finally {
      setOcrLoading(false);
    }
  };

  const buildPayload = (): Partial<Expense> => ({
    amount: parseFloat(amount),
    currency,
    category,
    description: description.trim(),
    costCenter: costCenter.trim() || undefined,
    receiptUrl: receiptUri,
    receiptOcrData: ocr,
    status: 'Draft',
  });

  const saveDraft = () => {
    setSavingDraft(true);
    const payload = buildPayload();
    if (isEdit && draftId) {
      dispatch(updateExpense({ id: draftId, data: payload }));
    } else {
      enqueueOffline(dispatch, {
        action: 'CREATE',
        entityType: 'Expense',
        payload: { ...payload, draftSyncAt: new Date().toISOString() },
      });
    }
    setTimeout(() => {
      setSavingDraft(false);
      navigation.goBack();
    }, 300);
  };

  const submit = () => {
    const validation = validateExpenseForm({ amount, currency, category, description });
    setErrors(validation);
    if (Object.keys(validation).length > 0) return;

    const payload: Partial<Expense> = { ...buildPayload(), status: 'Submitted', submittedAt: new Date().toISOString() };

    if (offlineMode) {
      enqueueOffline(dispatch, {
        action: isEdit ? 'UPDATE' : 'CREATE',
        entityType: 'Expense',
        entityId: draftId ?? undefined,
        payload: { ...payload, draftSyncAt: new Date().toISOString() },
      });
      // Show confirmation then go back
      setShowConfirmationModal(true);
      return;
    }

    setSubmitLoading(true);
    if (isEdit && draftId) {
      dispatch(updateExpense({ id: draftId, data: payload }));
    } else {
      dispatch(createExpense(payload));
    }
    // Show confirmation then go back after short delay
    setShowConfirmationModal(true);
    setTimeout(() => {
      setSubmitLoading(false);
      navigation.goBack();
    }, 1500);
  };

  const handleBackPress = () => {
    if (savingDraft || submitLoading) {
      return true; // Prevent back during save/submit
    }
    // Ask to save changes if form is dirty
    const hasChanges =
      amount !== '' ||
      currency !== 'AUD' ||
      category !== CATEGORIES[0] ||
      description !== '' ||
      date !== new Date().toISOString().slice(0, 10) ||
      costCenter !== '' ||
      receiptUri !== undefined ||
      ocr !== undefined;

    if (hasChanges) {
      Alert.alert(
        'Save changes?',
        'You have unsaved changes. Save as draft before leaving?',
        [
          {
            text: 'Discard',
            onPress: () => navigation.goBack(),
            style: 'destructive',
          },
          { text: 'Save Draft', onPress: saveDraft },
          { text: 'Cancel', style: 'cancel' },
        ]
      );
      return true; // Stay on screen
    }
    navigation.goBack();
    return true;
  };

  return (
    <View style={{ flex: 1 }}>
      {/* Image Preview Modal */}
      <Modal
        visible={showImagePreview && !!capturedImage}
        animationType="slide"
        transparent={false}
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalContent}>
            <Image source={{ uri: capturedImage }} style={styles.previewImage} />
            <View style={styles.modalButtons}>
              <Button mode="outlined" onPress={() => setShowImagePreview(false)}>
                Cancel
              </Button>
              <Button
                mode="contained"
                onPress={() => {
                  setShowImagePreview(false);
                  setReceiptUri(capturedImage);
                }}
              >
                Use This Photo
              </Button>
            </View>
          </View>
        </View>
      </Modal>

      {/* Submission Confirmation Modal */}
      <Modal
        visible={showConfirmationModal}
        animationType="fade"
        transparent={true}
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>
              {isEdit ? 'Expense Updated!' : 'Expense Submitted!'}
            </Text>
            <Text style={styles.modalSubtitle}>
              Your expense has been {isEdit ? 'updated' : 'submitted'} successfully.
            </Text>
            {offlineMode && (
              <Text style={[styles.modalNote, styles.offlineNote]}>
                Saved locally - will sync when connection returns
              </Text>
            )}
            <Button
              mode="contained"
              style={styles.modalButton}
              onPress={() => {
                setShowConfirmationModal(false);
                navigation.goBack();
              }}
            >
              OK
            </Button>
          </View>
        </View>
      </Modal>

      <ScrollView
        style={styles.container}
        contentContainerStyle={styles.contentContainer}
        keyboardShouldPersistTaps="handled"
      >
        <Card style={styles.card}>
          <Card.Content>
            <PaperText variant="titleMedium" style={styles.sectionTitle}>
              {isEdit ? 'Edit Expense' : 'New Expense'}
            </PaperText>

            {/* Amount */}
            <PaperText variant="labelSmall">Amount</PaperText>
            <View style={styles.inputContainer}>
              <TextInput
                style={styles.input}
                keyboardType="decimal-pad"
                value={amount}
                onChangeText={setAmount}
                placeholder="0.00"
              />
              <TextInput
                style={styles.currencyInput}
                value={currency}
                onChangeText={setCurrency}
                autoCapitalize="characters"
                maxLength={3}
                placeholder="AUD"
              />
            </View>
            {errors.amount && <Text style={styles.errorText}>{errors.amount}</Text>}

            {/* Description */}
            <PaperText variant="labelSmall" style={styles.labelTop}>Description</PaperText>
            <TextInput
              style={styles.input}
              value={description}
              onChangeText={setDescription}
              placeholder="What was this for?"
            />
            {errors.description && <Text style={styles.errorText}>{errors.description}</Text>}

            {/* Category */}
            <PaperText variant="labelSmall" style={styles.labelTop}>Category</PaperText>
            <View style={styles.chipRow}>
              {CATEGORIES.map((c) => (
                <TouchableOpacity
                  key={c}
                  style={[
                    styles.chip,
                    category === c ? styles.chipActive : styles.chipInactive,
                  ]}
                  onPress={() => setCategory(c)}
                >
                  <Text style={styles.chipText}>{c}</Text>
                </TouchableOpacity>
              ))}
            </View>

            {/* Date */}
            <PaperText variant="labelSmall" style={styles.labelTop}>Date</PaperText>
            <TextInput
              style={styles.input}
              value={date}
              onChangeText={setDate}
              placeholder="YYYY-MM-DD"
            />

            {/* Cost Center */}
            <PaperText variant="labelSmall" style={styles.labelTop}>Cost Center (optional)</PaperText>
            <TextInput
              style={styles.input}
              value={costCenter}
              onChangeText={setCostCenter}
            />

            {/* Receipt Section */}
            <PaperText variant="labelSmall" style={styles.labelTop}>Receipt</PaperText>
            <View style={styles.receiptContainer}>
              {receiptUri ? (
                <View style={styles.receiptImageContainer}>
                  <Image source={{ uri: receiptUri }} style={styles.receiptImage} />
                  <View style={styles.receiptOverlay}>
                    <Text style={styles.overlayText}>Tap to change</Text>
                  </View>
                  <TouchableOpacity
                    onPress={() => {
                      setReceiptUri(undefined);
                      setOcr(undefined);
                    }}
                    style={styles.removeButton}
                  >
                    <Text style={styles.removeText}>Remove</Text>
                  </TouchableOpacity>
                </View>
              ) : (
                <View style={styles.receiptPlaceholder}>
                  {!cameraPermissionResponse ? (
                    <View style={styles.permissionDenied}>
                      <Text style={styles.permissionText}>Camera permission denied</Text>
                      <Button mode="outlined" onPress={() => Linking.openSettings()}>
                        Grant Permission
                      </Button>
                    </View>
                  ) : (
                    <View style={!ocrLoading ? buttonsContainer : loadingContainer}>
                      {ocrLoading ? (
                        <View style={styles.loadingContainer}>
                          <Text style={styles.loadingText}>Processing receipt...</Text>
                          <ActivityIndicator size="small" color="#fff" />
                        </View>
                      ) : (
                        <>
                          <TouchableOpacity
                            style={[styles.button, styles.cameraButton]}
                            onPress={captureImage}
                            activeOpacity={0.7}
                          >
                            <Text style={styles.buttonText}>📷 Take Photo</Text>
                          </TouchableOpacity>
                          <TouchableOpacity
                            style={[styles.button, styles.libraryButton]}
                            onPress={pickFromLibrary}
                            activeOpacity={0.7}
                          >
                            <Text style={styles.buttonText}>🖼️ From Gallery</Text>
                          </TouchableOpacity>
                        </>
                      )}
                    </View>
                  )}
                </View>
              )}
            </View>

            {/* OCR Results Section */}
            {ocr && (
              <View style={styles.ocrSection}>
                <PaperText variant="labelSmall" style={styles.sectionTitle}>
                  Extracted Details
                </PaperText>
                <View style={styles.ocrFields}>
                  <View style={styles.fieldRow}>
                    <Text style={styles.fieldLabel}>Supplier:</Text>
                    <TextInput
                      style={styles.fieldInput}
                      value={ocr.supplierName || ''}
                      onChangeText={(text) => {
                        const updated = { ...ocr, supplierName: text };
                        setOcr(updated);
                      }}
                    />
                  </View>
                  <View style={styles.fieldRow}>
                    <Text style={styles.fieldLabel}>Date:</Text>
                    <TextInput
                      style={styles.fieldInput}
                      value={ocr.transactionDate || ''}
                      onChangeText={(text) => {
                        const updated = { ...ocr, transactionDate: text };
                        setOcr(updated);
                      }}
                    />
                  </View>
                  <View style={styles.fieldRow}>
                    <Text style={styles.fieldLabel}>Amount ({currency}):</Text>
                    <TextInput
                      style={styles.fieldInput}
                      value={ocr.grossAmount?.toString() || ''}
                      keyboardType="decimal-pad"
                      onChangeText={(text) => {
                        const num = parseFloat(text);
                        const updated = { ...ocr, grossAmount: isNaN(num) ? null : num };
                        setOcr(updated);
                        if (!isNaN(num)) setAmount(num.toString());
                      }}
                    />
                  </View>
                  <View style={styles.fieldRow}>
                    <Text style={styles.fieldLabel}>GST ({currency}):</Text>
                    <TextInput
                      style={styles.fieldInput}
                      value={ocr.gstAmount?.toString() || ''}
                      keyboardType="decimal-pad"
                      onChangeText={(text) => {
                        const num = parseFloat(text);
                        const updated = { ...ocr, gstAmount: isNaN(num) ? null : num };
                        setOcr(updated);
                      }}
                    />
                  </View>
                </View>
                {ocr.extractionConfidence !== undefined && (
                  <Text style={styles.confidenceText}>
                    Confidence: {Math.round(ocr.extractionConfidence * 100)}%
                  </Text>
                )}
              </View>
            )}
          </Card.Content>
        </Card>

        {/* Action Buttons */}
        <View style={styles.actionsContainer}>
          <View style={styles.buttonRow}>
            <Button
              mode="outlined"
              onPress={saveDraft}
              disabled={savingDraft}
              style={styles.actionButton}
            >
              {savingDraft ? 'Saving...' : 'Save Draft'}
            </Button>
            <Button
              mode="contained"
              onPress={submit}
              disabled={submitLoading || !amount || !description}
              style={styles.primaryButton}
              loading={submitLoading}
            >
              {submitLoading ? 'Submitting...' : isEdit ? 'Update' : 'Submit'}
            </Button>
          </View>
          <View style={styles.buttonHint}>
            <Text style={styles.hintText}>
              Swipe left on expense in list to edit or delete
            </Text>
          </View>
        </View>

        {/* Offline Indicator */}
        {offlineMode && (
          <View style={styles.offlineBanner}>
            <Text style={styles.offlineText}>📶 Offline - Changes will sync when online</Text>
          </View>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  contentContainer: {
    paddingBottom: 80,
  },
  card: {
    marginBottom: 16,
    backgroundColor: '#fff',
    borderRadius: 12,
    overflow: 'hidden',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 16,
    color: '#333',
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  input: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    backgroundColor: '#fff',
  },
  currencyInput: {
    width: 50,
    textAlign: 'center',
    marginLeft: 8,
  },
  errorText: {
    color: '#d32f2f',
    fontSize: 12,
    marginTop: 4,
    marginHorizontal: 12,
  },
  labelTop: {
    marginTop: 16,
    marginBottom: 4,
    fontSize: 14,
    fontWeight: '500',
    color: '#555',
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 4,
  },
  chip: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    marginRight: 8,
    marginBottom: 8,
    borderRadius: 20,
  },
  chipActive: {
    backgroundColor: '#1976d2',
  },
  chipInactive: {
    backgroundColor: '#f5f5f5',
    borderWidth: 1,
    borderColor: '#ddd',
  },
  chipText: {
    color: '#fff',
    fontSize: 14,
  },
  labelTop: {
    marginTop: 16,
    marginBottom: 4,
    fontSize: 14,
    fontWeight: '500',
    color: '#555',
  },
  receiptContainer: {
    marginTop: 16,
  },
  receiptPlaceholder: {
    backgroundColor: '#fafafa',
    borderWidth: 2,
    borderStyle: 'dashed',
    borderColor: '#bbb',
    borderRadius: 12,
    height: 180,
    justifyContent: 'center',
    alignItems: 'center',
  },
  receiptImageContainer: {
    position: 'relative',
    borderRadius: 12,
    overflow: 'hidden',
    height: 180,
  },
  receiptImage: {
    width: '100%',
    height: '100%',
  },
  receiptOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  overlayText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '500',
  },
  removeButton: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: 'rgba(255,255,255,0.7)',
    borderRadius: 16,
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  removeText: {
    color: '#d32f2f',
    fontSize: 12,
    fontWeight: '600',
  },
  buttonsContainer: {
    marginTop: 16,
    flexDirection: 'row',
    justifyContent: 'space-around',
  },
  button: {
    backgroundColor: '#1976d2',
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 8,
    minWidth: 120,
    alignItems: 'center',
  },
  cameraButton: {
    backgroundColor: '#4caf50',
  },
  libraryButton: {
    backgroundColor: '#ff9800',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  loadingContainer: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    color: '#fff',
    fontSize: 14,
    marginBottom: 8,
  },
  ocrSection: {
    marginTop: 20,
    backgroundColor: '#f8f9fa',
    borderRadius: 12,
    padding: 16,
  },
  ocrFields: {
    marginTop: 12,
  },
  fieldRow: {
    flexDirection: 'row',
    marginBottom: 12,
  },
  fieldLabel: {
    width: 80,
    fontWeight: '600',
    color: '#555',
  },
  fieldInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 6,
    paddingHorizontal: 10,
    paddingVertical: 8,
  },
  confidenceText: {
    marginTop: 8,
    fontSize: 12,
    color: '#666',
    textAlign: 'center',
  },
  actionsContainer: {
    padding: 16,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#eee',
  },
  buttonRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  actionButton: {
    flex: 1,
    marginHorizontal: 8,
  },
  primaryButton: {
    backgroundColor: '#4caf50',
  },
  hintText: {
    textAlign: 'center',
    color: '#999',
    fontSize: 12,
    marginTop: 8,
  },
  offlineBanner: {
    backgroundColor: '#ff9800',
    padding: 12,
    alignItems: 'center',
  },
  offlineText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '500',
  },
  modalContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(0,0,0,0.5)',
  },
  modalContent: {
    backgroundColor: '#fff',
    padding: 24,
    borderRadius: 12,
    width: '80%',
    maxWidth: 400,
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: '600',
    textAlign: 'center',
    marginBottom: 16,
    color: '#333',
  },
  modalSubtitle: {
    fontSize: 16,
    textAlign: 'center',
    color: '#666',
    marginBottom: 24,
  },
  modalNote: {
    fontSize: 14,
    textAlign: 'center',
    color: '#888',
  },
  offlineNote: {
    color: '#ff9800',
    fontWeight: '600',
  },
  modalButton: {
    backgroundColor: '#1976d2',
    paddingVertical: 12,
    borderRadius: 8,
    width: '100%',
  },
});

export default CreateExpenseScreen;