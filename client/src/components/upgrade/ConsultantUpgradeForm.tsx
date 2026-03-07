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
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  ArrowBack as BackIcon,
  CloudUpload as UploadIcon,
} from '@mui/icons-material';
import { upgradeService } from '@/services/upgradeService';
import { translateError } from '@/utils/errorUtils';
import type { EducationEntryDto, UpgradeRequestLinkDto } from '@/types';
import type { AxiosError } from 'axios';

interface Props {
  onBack: () => void;
}

const EXPERTISE_OPTIONS = [
  'Academic Writing', 'Research', 'STEM', 'Business', 'Law', 'Medicine',
  'Engineering', 'Arts', 'Social Sciences', 'Education', 'Technology',
];

const LANGUAGE_OPTIONS = [
  'English', 'Arabic', 'French', 'Spanish', 'German', 'Chinese', 'Japanese', 'Other',
];

const LINK_LABELS: UpgradeRequestLinkDto['label'][] = ['LinkedIn', 'Portfolio', 'Website', 'Other'];

const emptyEducation: EducationEntryDto = {
  institutionName: '',
  degreeName: '',
  fieldOfStudy: '',
  startYear: new Date().getFullYear(),
  endYear: undefined,
  isCurrentlyStudying: false,
};

export function ConsultantUpgradeForm({ onBack }: Props) {
  const { t } = useTranslation();

  const [education, setEducation] = useState<EducationEntryDto[]>([{ ...emptyEducation }]);
  const [experienceSummary, setExperienceSummary] = useState('');
  const [expertiseTags, setExpertiseTags] = useState<string[]>([]);
  const [languages, setLanguages] = useState<string[]>([]);
  const [links, setLinks] = useState<UpgradeRequestLinkDto[]>([]);
  const [files, setFiles] = useState<File[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const updateEducation = (index: number, field: keyof EducationEntryDto, value: unknown) => {
    setEducation((prev) => prev.map((e, i) => (i === index ? { ...e, [field]: value } : e)));
  };

  const addEducation = () => {
    if (education.length < 5) setEducation([...education, { ...emptyEducation }]);
  };

  const removeEducation = (index: number) => {
    if (education.length > 1) setEducation(education.filter((_, i) => i !== index));
  };

  const addLink = () => {
    if (links.length < 3) setLinks([...links, { url: '', label: 'LinkedIn' }]);
  };

  const updateLink = (index: number, field: keyof UpgradeRequestLinkDto, value: string) => {
    setLinks((prev) => prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)));
  };

  const removeLink = (index: number) => {
    setLinks(links.filter((_, i) => i !== index));
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newFiles = Array.from(e.target.files ?? []);
    setFiles((prev) => [...prev, ...newFiles].slice(0, 5));
  };

  const toggleTag = (tag: string) => {
    setExpertiseTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : prev.length < 10 ? [...prev, tag] : prev
    );
  };

  const toggleLanguage = (lang: string) => {
    setLanguages((prev) =>
      prev.includes(lang) ? prev.filter((l) => l !== lang) : prev.length < 5 ? [...prev, lang] : prev
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;

    setError(null);
    setLoading(true);

    try {
      await upgradeService.submitConsultant({
        education,
        experienceSummary,
        expertiseTags,
        languages,
        links,
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
          <Typography variant="h6">{t('upgrade.becomeConsultant')}</Typography>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate>
          {/* Education */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mt: 2, mb: 1 }}>
            {t('upgrade.education')}
          </Typography>
          {education.map((edu, idx) => (
            <Box key={idx} sx={{ mb: 2, p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="body2" fontWeight={600}>
                  #{idx + 1}
                </Typography>
                {education.length > 1 && (
                  <IconButton size="small" onClick={() => removeEducation(idx)}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                )}
              </Box>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                <TextField
                  label={t('upgrade.university')}
                  value={edu.institutionName}
                  onChange={(e) => updateEducation(idx, 'institutionName', e.target.value)}
                  size="small"
                  required
                  sx={{ flex: 1, minWidth: 200 }}
                />
                <TextField
                  label={t('upgrade.degree')}
                  value={edu.degreeName}
                  onChange={(e) => updateEducation(idx, 'degreeName', e.target.value)}
                  size="small"
                  required
                  sx={{ flex: 1, minWidth: 200 }}
                />
              </Box>
              <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                <TextField
                  label={t('upgrade.fieldOfStudy')}
                  value={edu.fieldOfStudy}
                  onChange={(e) => updateEducation(idx, 'fieldOfStudy', e.target.value)}
                  size="small"
                  required
                  sx={{ flex: 1, minWidth: 150 }}
                />
                <TextField
                  label={t('upgrade.startYear')}
                  type="number"
                  value={edu.startYear}
                  onChange={(e) => updateEducation(idx, 'startYear', parseInt(e.target.value))}
                  size="small"
                  required
                  sx={{ width: 110 }}
                />
                <TextField
                  label={t('upgrade.endYear')}
                  type="number"
                  value={edu.endYear ?? ''}
                  onChange={(e) => updateEducation(idx, 'endYear', e.target.value ? parseInt(e.target.value) : undefined)}
                  size="small"
                  disabled={edu.isCurrentlyStudying}
                  sx={{ width: 110 }}
                />
              </Box>
            </Box>
          ))}
          {education.length < 5 && (
            <Button startIcon={<AddIcon />} onClick={addEducation} size="small">
              {t('upgrade.addEducation')}
            </Button>
          )}

          <Divider sx={{ my: 3 }} />

          {/* Experience */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.experience')}
          </Typography>
          <TextField
            fullWidth
            multiline
            rows={4}
            value={experienceSummary}
            onChange={(e) => setExperienceSummary(e.target.value)}
            placeholder={t('upgrade.experiencePlaceholder')}
            helperText={`${experienceSummary.length}/1500`}
            slotProps={{ htmlInput: { maxLength: 1500 } }}
          />

          <Divider sx={{ my: 3 }} />

          {/* Expertise Tags */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.expertiseTags')} ({expertiseTags.length}/10)
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1 }}>
            {EXPERTISE_OPTIONS.map((tag) => (
              <Chip
                key={tag}
                label={tag}
                clickable
                color={expertiseTags.includes(tag) ? 'primary' : 'default'}
                onClick={() => toggleTag(tag)}
              />
            ))}
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Languages */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.languages')} ({languages.length}/5)
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1 }}>
            {LANGUAGE_OPTIONS.map((lang) => (
              <Chip
                key={lang}
                label={lang}
                clickable
                color={languages.includes(lang) ? 'primary' : 'default'}
                onClick={() => toggleLanguage(lang)}
              />
            ))}
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Links */}
          <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
            {t('upgrade.links')}
          </Typography>
          {links.map((link, idx) => (
            <Box key={idx} sx={{ display: 'flex', gap: 1, mb: 1, alignItems: 'center' }}>
              <TextField
                select
                label={t('upgrade.linkType')}
                value={link.label}
                onChange={(e) => updateLink(idx, 'label', e.target.value)}
                size="small"
                sx={{ width: 140 }}
              >
                {LINK_LABELS.map((l) => (
                  <MenuItem key={l} value={l}>{l}</MenuItem>
                ))}
              </TextField>
              <TextField
                label="URL"
                value={link.url}
                onChange={(e) => updateLink(idx, 'url', e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <IconButton size="small" onClick={() => removeLink(idx)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
          ))}
          {links.length < 3 && (
            <Button startIcon={<AddIcon />} onClick={addLink} size="small">
              {t('upgrade.addLink')}
            </Button>
          )}

          <Divider sx={{ my: 3 }} />

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

          <Divider sx={{ my: 3 }} />

          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={loading || expertiseTags.length === 0 || languages.length === 0 || !experienceSummary.trim()}
            fullWidth
          >
            {loading ? <CircularProgress size={24} /> : t('upgrade.submitRequest')}
          </Button>
        </Box>
      </CardContent>
    </Card>
  );
}
