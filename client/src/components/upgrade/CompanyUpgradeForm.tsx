import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  TextField,
  Typography,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  CloudUpload as UploadIcon,
} from '@mui/icons-material';
import { upgradeService } from '@/services/upgradeService';
import { translateError } from '@/utils/errorUtils';
import type { AxiosError } from 'axios';

interface Props {
  onBack: () => void;
}

export function CompanyUpgradeForm({ onBack }: Props) {
  const { t } = useTranslation();

  const [form, setForm] = useState({
    companyName: '',
    country: '',
    website: '',
    contactPersonName: '',
    contactEmail: '',
    contactPhone: '',
    companyRegistrationNumber: '',
  });
  const [files, setFiles] = useState<File[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const updateField = (field: string, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newFiles = Array.from(e.target.files ?? []);
    setFiles((prev) => [...prev, ...newFiles].slice(0, 5));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;

    setError(null);
    setLoading(true);

    try {
      await upgradeService.submitCompany({
        companyName: form.companyName,
        country: form.country,
        website: form.website || undefined,
        contactPersonName: form.contactPersonName,
        contactEmail: form.contactEmail,
        contactPhone: form.contactPhone || undefined,
        companyRegistrationNumber: form.companyRegistrationNumber,
      });

      if (files.length > 0) {
        await upgradeService.uploadFiles(files);
      }

      setSuccess(true);
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string; errors?: string[] }>;
      const data = axiosErr?.response?.data;
      if (data?.error) {
        setError(translateError(data.error));
      } else if (data?.errors && data.errors.length > 0) {
        setError(data.errors.map(translateError).join(' '));
      } else {
        setError(t('error'));
      }
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <Card>
        <CardContent sx={{ textAlign: 'center', py: 4 }}>
          <Alert severity="success" sx={{ mb: 2 }}>
            {t('upgrade.submitSuccess')}
          </Alert>
          <Button variant="outlined" onClick={onBack}>
            {t('upgrade.backToProfile')}
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <IconButton onClick={onBack}>
            <BackIcon />
          </IconButton>
          <Typography variant="h6">{t('upgrade.becomeCompany')}</Typography>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate>
          <TextField
            fullWidth
            label={t('upgrade.companyName')}
            value={form.companyName}
            onChange={(e) => updateField('companyName', e.target.value)}
            margin="normal"
            required
          />

          <TextField
            fullWidth
            label={t('upgrade.country')}
            value={form.country}
            onChange={(e) => updateField('country', e.target.value)}
            margin="normal"
            required
          />

          <TextField
            fullWidth
            label={t('upgrade.website')}
            value={form.website}
            onChange={(e) => updateField('website', e.target.value)}
            margin="normal"
            placeholder="https://"
          />

          <Divider sx={{ my: 2 }} />

          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.contactInfo')}
          </Typography>

          <TextField
            fullWidth
            label={t('upgrade.contactName')}
            value={form.contactPersonName}
            onChange={(e) => updateField('contactPersonName', e.target.value)}
            margin="normal"
            required
          />

          <TextField
            fullWidth
            label={t('upgrade.contactEmail')}
            type="email"
            value={form.contactEmail}
            onChange={(e) => updateField('contactEmail', e.target.value)}
            margin="normal"
            required
          />

          <TextField
            fullWidth
            label={t('upgrade.contactPhone')}
            value={form.contactPhone}
            onChange={(e) => updateField('contactPhone', e.target.value)}
            margin="normal"
          />

          <Divider sx={{ my: 2 }} />

          <TextField
            fullWidth
            label={t('upgrade.crn')}
            value={form.companyRegistrationNumber}
            onChange={(e) => updateField('companyRegistrationNumber', e.target.value)}
            margin="normal"
            required
            helperText={t('upgrade.crnHint')}
          />

          <Divider sx={{ my: 2 }} />

          {/* File Upload */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.proofDocuments')}
          </Typography>
          <Button
            variant="outlined"
            component="label"
            startIcon={<UploadIcon />}
            disabled={files.length >= 5}
          >
            {t('upgrade.uploadFiles')}
            <input
              type="file"
              hidden
              multiple
              accept=".pdf,.jpg,.jpeg,.png"
              onChange={handleFileChange}
            />
          </Button>
          <Typography variant="caption" display="block" color="text.secondary" sx={{ mt: 0.5 }}>
            {t('upgrade.fileHint')}
          </Typography>
          {files.length > 0 && (
            <Box sx={{ mt: 1, display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {files.map((f, i) => (
                <Chip
                  key={i}
                  label={f.name}
                  onDelete={() => setFiles(files.filter((_, j) => j !== i))}
                  size="small"
                />
              ))}
            </Box>
          )}

          <Divider sx={{ my: 2 }} />

          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={
              loading ||
              !form.companyName ||
              !form.country ||
              !form.contactPersonName ||
              !form.contactEmail ||
              !form.companyRegistrationNumber
            }
            fullWidth
          >
            {loading ? <CircularProgress size={24} /> : t('upgrade.submitRequest')}
          </Button>
        </Box>
      </CardContent>
    </Card>
  );
}
