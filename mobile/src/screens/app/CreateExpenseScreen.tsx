import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  TextInput,
  Switch,
  Alert,
} from 'react-native';
import { Text as PaperText, Card, Button, ActivityIndicator, Divider } from 'react-native-paper';
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

  const isEdit = draftId != null;
  const loadedRef = useRef(false);

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

  const pickReceipt = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      quality: 0.7,
      base64: false,
    });
    if (!result.canceled && result.assets[0]) {
      const uri = result.assets[0].uri;
      setReceiptUri(uri);
      await runOcr(uri);
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
      navigation.goBack();
      return;
    }

    if (isEdit && draftId) {
      dispatch(updateExpense({ id: draftId, data: payload }));
    } else {
      dispatch(createExpense(payload));
    }
    navigation.goBack();
  };

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>
            {isEdit ? 'Edit Expense' : 'New Expense'}
          </PaperText>

          <PaperText variant="labelSmall">Amount</PaperText>
          <TextInput
            style={styles.input}
            keyboardType="decimal-pad"
            value={amount}
            onChangeText={setAmount}
            placeholder="0.00"
          />
          {errors.amount && <PaperText style={styles.err}>{errors.amount}</PaperText>}

          <PaperText variant="labelSmall" style={styles.labelTop}>Currency</PaperText>
          <TextInput style={styles.input} value={currency} onChangeText={setCurrency} autoCapitalize="characters" />

          <PaperText variant="labelSmall" style={styles.labelTop}>Category</PaperText>
          <View style={styles.chipRow}>
            {CATEGORIES.map((c) => (
              <Button
                key={c}
                mode={category === c ? 'contained' : 'outlined'}
                compact
                onPress={() => setCategory(c)}
                style={styles.chip}
              >
                {c}
              </Button>
            ))}
          </View>

          <PaperText variant="labelSmall" style={styles.labelTop}>Description</PaperText>
          <TextInput
            style={styles.input}
            value={description}
            onChangeText={setDescription}
            placeholder="What was this for?"
          />
          {errors.description && <PaperText style={styles.err}>{errors.description}</PaperText>}

          <PaperText variant="labelSmall" style={styles.labelTop}>Date</PaperText>
          <TextInput style={styles.input} value={date} onChangeText={setDate} placeholder="YYYY-MM-DD" />

          <PaperText variant="labelSmall" style={styles.labelTop}>Cost Center (optional)</PaperText>
          <TextInput style={styles.input} value={costCenter} onChangeText={setCostCenter} />
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <PaperText variant="titleMedium" style={styles.sectionTitle}>Receipt</PaperText>
          <Button mode="outlined" icon="camera" onPress={pickReceipt} style={styles.receiptBtn}>
            {receiptUri ? 'Change Receipt' : 'Add Receipt'}
          </Button>
          {ocrLoading && <ActivityIndicator style={styles.ocrLoading} />}
          {ocr && (
            <View style={styles.ocrBox}>
              <Divider style={styles.divider} />
              <PaperText variant="labelSmall" style={styles.muted}>
                Extracted (conf: {Math.round((ocr.extractionConfidence ?? 0) * 100)}%)
              </PaperText>
              {ocr.supplierName && <PaperText variant="bodySmall">Supplier: {ocr.supplierName}</PaperText>}
              {ocr.transactionDate && <PaperText variant="bodySmall">Date: {ocr.transactionDate}</PaperText>}
              {ocr.gstAmount != null && (
                <PaperText variant="bodySmall">GST: {currency} {ocr.gstAmount.toFixed(2)}</PaperText>
              )}
            </View>
          )}
        </Card.Content>
      </Card>

      <View style={styles.actions}>
        <Button mode="contained" onPress={submit} loading={isLoading} style={styles.actionButton}>
          Submit
        </Button>
        <Button mode="outlined" onPress={saveDraft} loading={savingDraft} style={styles.actionButton}>
          Save Draft
        </Button>
        <Button mode="text" onPress={() => navigation.goBack()}>Cancel</Button>
      </View>

      {offlineMode && (
        <PaperText variant="labelSmall" style={styles.offlineNote}>
          Offline — will sync when connection returns
        </PaperText>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f5f5f5', padding: 16 },
  card: { marginBottom: 12, backgroundColor: '#fff' },
  sectionTitle: { marginBottom: 12 },
  labelTop: { marginTop: 14, marginBottom: 4 },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 6,
    paddingHorizontal: 10,
    paddingVertical: 8,
    fontSize: 16,
    backgroundColor: '#fff',
  },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap' },
  chip: { marginRight: 6, marginBottom: 6 },
  err: { color: '#f44336', marginTop: 4 },
  receiptBtn: { marginTop: 4 },
  ocrLoading: { marginVertical: 10 },
  ocrBox: { marginTop: 8 },
  divider: { marginVertical: 8 },
  muted: { color: '#999', marginBottom: 4 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', marginTop: 4 },
  actionButton: { marginRight: 8, marginBottom: 8 },
  offlineNote: { color: '#ff9800', textAlign: 'center', marginTop: 8 },
});

export default CreateExpenseScreen;
