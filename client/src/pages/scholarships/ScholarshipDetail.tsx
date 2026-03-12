import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link as RouterLink } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import DOMPurify from 'dompurify';
import {
  Box,
  Typography,
  Paper,
  Breadcrumbs,
  Link,
  Chip,
  IconButton,
  Button,
  Tabs,
  Tab,
  Skeleton,
  Snackbar,
  Alert,
  Tooltip,
  Divider,
  Container,
  useMediaQuery,
  useTheme,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Bookmark,
  BookmarkBorder,
  Share,
  OpenInNew,
  AccessTime,
  LocationOn,
  School,
  Visibility,
  CheckCircleOutline,
  PlaylistAdd,
} from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuthModal } from '@/hooks/useAuthModal';
import { scholarshipService } from '@/services/scholarshipService';
import { ScholarshipFundingType, DegreeLevel } from '@/types';
import { TrackingModal } from '@/components/scholarships/TrackingModal';

function getFundingTypeKey(type: ScholarshipFundingType): string {
  switch (type) {
    case ScholarshipFundingType.FullyFunded:
      return 'scholarshipPage.fullyFunded';
    case ScholarshipFundingType.PartiallyFunded:
      return 'scholarshipPage.partiallyFunded';
    case ScholarshipFundingType.SelfFunded:
      return 'scholarshipPage.selfFunded';
    default:
      return 'scholarshipPage.other';
  }
}

function getDegreeLevelKey(level: DegreeLevel): string {
  switch (level) {
    case DegreeLevel.Bachelors:
      return 'scholarshipPage.bachelors';
    case DegreeLevel.Masters:
      return 'scholarshipPage.masters';
    case DegreeLevel.PhD:
      return 'scholarshipPage.phd';
    case DegreeLevel.Diploma:
      return 'scholarshipPage.diploma';
    default:
      return 'scholarshipPage.other';
  }
}

function getFundingColor(
  type: ScholarshipFundingType
): 'success' | 'warning' | 'info' | 'default' {
  switch (type) {
    case ScholarshipFundingType.FullyFunded:
      return 'success';
    case ScholarshipFundingType.PartiallyFunded:
      return 'warning';
    case ScholarshipFundingType.SelfFunded:
      return 'info';
    default:
      return 'default';
  }
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel({ children, value, index }: TabPanelProps) {
  return (
    <Box role="tabpanel" hidden={value !== index} sx={{ py: 3 }}>
      {value === index && children}
    </Box>
  );
}

function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'i', 'em', 'strong', 'a', 'ul', 'ol', 'li',
      'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'code',
      'table', 'thead', 'tbody', 'tr', 'th', 'td', 'span', 'div', 'hr',
    ],
    ALLOWED_ATTR: ['href', 'target', 'rel', 'class'],
  });
}

function parseDocumentsChecklist(json: string | null): string[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    if (Array.isArray(parsed)) return parsed;
    return [];
  } catch {
    return [];
  }
}

export default function ScholarshipDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const language = useUiStore((s) => s.language);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useAuthModal((s) => s.open);
  const queryClient = useQueryClient();

  const [tabValue, setTabValue] = useState(0);
  // null = not yet overridden by user; read from server data instead
  const [isSavedOverride, setIsSavedOverride] = useState<boolean | null>(null);
  const [saving, setSaving] = useState(false);
  const [trackingOpen, setTrackingOpen] = useState(false);
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'info' | 'error';
  }>({ open: false, message: '', severity: 'success' });

  const {
    data: scholarship,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['scholarship', id],
    queryFn: () => scholarshipService.getScholarshipById(id!),
    enabled: !!id,
  });

  // Derive isSaved: prefer local override (from user action), fall back to server data
  const isSaved = isSavedOverride ?? scholarship?.isSaved ?? false;

  const title =
    language === 'ar' && scholarship?.titleAr
      ? scholarship.titleAr
      : scholarship?.title;
  const description =
    language === 'ar' && scholarship?.descriptionAr
      ? scholarship.descriptionAr
      : scholarship?.description;
  const providerName =
    language === 'ar' && scholarship?.providerNameAr
      ? scholarship.providerNameAr
      : scholarship?.providerName;

  const handleBookmarkClick = async () => {
    if (!isAuthenticated) {
      openAuthModal('login');
      setSnackbar({
        open: true,
        message: t('scholarshipPage.loginToSave'),
        severity: 'info',
      });
      return;
    }
    if (!scholarship) return;

    const previousState = isSaved;
    setIsSavedOverride(!isSaved);
    setSaving(true);

    try {
      if (previousState) {
        await scholarshipService.unsaveScholarship(scholarship.id);
        setSnackbar({
          open: true,
          message: t('scholarshipPage.removedFromSaved'),
          severity: 'success',
        });
      } else {
        await scholarshipService.saveScholarship(scholarship.id);
        setSnackbar({
          open: true,
          message: t('scholarshipPage.savedToList'),
          severity: 'success',
        });
      }
      // Keep dashboard count and saved list in sync
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dashboard', 'summary'] }),
        queryClient.invalidateQueries({ queryKey: ['scholarships'] }),
      ]);
    } catch {
      setIsSavedOverride(previousState);
    } finally {
      setSaving(false);
    }
  };

  const handleShare = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setSnackbar({
        open: true,
        message: t('scholarshipDetail.linkCopied'),
        severity: 'success',
      });
    } catch {
      // Clipboard API failed silently
    }
  };

  const handleApplyNow = () => {
    if (scholarship?.officialLink) {
      window.open(scholarship.officialLink, '_blank', 'noopener,noreferrer');
    }
  };

  const handleAddToTracking = () => {
    if (!isAuthenticated) {
      openAuthModal('login');
      return;
    }
    setTrackingOpen(true);
  };

  // Error / not found state
  if (error) {
    const is404 = (error as { response?: { status?: number } })?.response?.status === 404;
    if (is404) {
      return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
          <Paper sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="h1" color="primary" sx={{ fontSize: '6rem', fontWeight: 700 }}>
              404
            </Typography>
            <Typography variant="h5" gutterBottom>
              {t('scholarshipDetail.scholarshipNotFound')}
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              {t('scholarshipDetail.scholarshipNotFoundDesc')}
            </Typography>
            <Button
              variant="contained"
              component={RouterLink}
              to="/scholarships"
            >
              {t('scholarshipDetail.backToScholarships')}
            </Button>
          </Paper>
        </Container>
      );
    }
    return (
      <Container maxWidth="sm" sx={{ py: 8 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom>
            {t('error')}
          </Typography>
          <Button
            variant="contained"
            component={RouterLink}
            to="/scholarships"
          >
            {t('scholarshipDetail.backToScholarships')}
          </Button>
        </Paper>
      </Container>
    );
  }

  // Loading state
  if (isLoading || !scholarship) {
    return (
      <Container maxWidth="lg" sx={{ py: 3 }}>
        <Skeleton variant="text" width={300} height={24} sx={{ mb: 2 }} />
        <Skeleton variant="text" width="60%" height={48} sx={{ mb: 1 }} />
        <Skeleton variant="text" width="40%" height={24} sx={{ mb: 3 }} />
        <Paper sx={{ p: 3, mb: 3 }}>
          <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
            {[1, 2, 3, 4].map((i) => (
              <Box key={i} sx={{ flex: '1 1 200px' }}>
                <Skeleton variant="text" width="80%" height={20} />
                <Skeleton variant="text" width="60%" height={28} />
              </Box>
            ))}
          </Box>
        </Paper>
        <Skeleton variant="rounded" width="100%" height={48} sx={{ mb: 2 }} />
        <Skeleton variant="rounded" width="100%" height={200} />
      </Container>
    );
  }

  const documentsChecklist = parseDocumentsChecklist(scholarship.documentsChecklist);

  return (
    <>
      <Container maxWidth="lg" sx={{ py: 3, pb: isMobile ? 10 : 3 }}>
        {/* Breadcrumbs */}
        <Breadcrumbs sx={{ mb: 2 }}>
          <Link component={RouterLink} to="/" underline="hover" color="inherit">
            {t('scholarshipDetail.breadcrumbHome')}
          </Link>
          <Link
            component={RouterLink}
            to="/scholarships"
            underline="hover"
            color="inherit"
          >
            {t('scholarshipDetail.breadcrumbScholarships')}
          </Link>
          <Typography color="text.primary" sx={{ maxWidth: 300 }} noWrap>
            {title}
          </Typography>
        </Breadcrumbs>

        {/* Header Section */}
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            mb: 3,
          }}
        >
          <Box sx={{ flex: 1 }}>
            <Typography variant="h4" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
              {title}
            </Typography>

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
              {providerName && (
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  <School sx={{ fontSize: 18, mr: 0.5, color: 'text.secondary' }} />
                  <Typography variant="body1" color="text.secondary">
                    {providerName}
                  </Typography>
                </Box>
              )}
              {scholarship.country && (
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  <LocationOn sx={{ fontSize: 18, mr: 0.5, color: 'text.secondary' }} />
                  <Typography variant="body1" color="text.secondary">
                    {scholarship.country}
                  </Typography>
                </Box>
              )}
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Visibility sx={{ fontSize: 18, mr: 0.5, color: 'text.secondary' }} />
                <Typography variant="body2" color="text.secondary">
                  {t('scholarshipDetail.viewCount', { count: scholarship.viewCount })}
                </Typography>
              </Box>
            </Box>
          </Box>

          {!isMobile && (
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Tooltip title={isSaved ? t('scholarshipDetail.unsaveScholarship') : t('scholarshipDetail.saveScholarship')}>
                <IconButton
                  onClick={handleBookmarkClick}
                  disabled={saving}
                  color={isSaved ? 'primary' : 'default'}
                >
                  {isSaved ? <Bookmark /> : <BookmarkBorder />}
                </IconButton>
              </Tooltip>
              <Tooltip title={t('scholarshipDetail.share')}>
                <IconButton onClick={handleShare}>
                  <Share />
                </IconButton>
              </Tooltip>
            </Box>
          )}
        </Box>

        {/* Key Info Card */}
        <Paper sx={{ p: 3, mb: 3 }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'repeat(2, 1fr)',
                md: 'repeat(4, 1fr)',
              },
              gap: 3,
            }}
          >
            {/* Deadline */}
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('scholarshipDetail.deadline')}
              </Typography>
              {scholarship.deadline ? (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <AccessTime sx={{ fontSize: 18 }} />
                  <Typography variant="body1" sx={{ fontWeight: 600 }}>
                    {new Date(scholarship.deadline).toLocaleDateString()}
                  </Typography>
                  {scholarship.deadlineCountdownDays != null && (
                    <Chip
                      label={
                        scholarship.deadlineCountdownDays > 0
                          ? t('scholarshipDetail.daysLeft', {
                              count: scholarship.deadlineCountdownDays,
                            })
                          : t('scholarshipDetail.expired')
                      }
                      size="small"
                      color={
                        scholarship.deadlineCountdownDays <= 0
                          ? 'error'
                          : scholarship.deadlineCountdownDays <= 14
                            ? 'warning'
                            : 'success'
                      }
                    />
                  )}
                </Box>
              ) : (
                <Typography variant="body1">
                  {t('scholarshipDetail.notSpecified')}
                </Typography>
              )}
            </Box>

            {/* Award Amount */}
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('scholarshipDetail.awardAmount')}
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }} color="primary">
                {scholarship.awardAmount != null
                  ? `${scholarship.currency || 'USD'} ${scholarship.awardAmount.toLocaleString()}`
                  : t('scholarshipDetail.notSpecified')}
              </Typography>
            </Box>

            {/* Degree Level */}
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('scholarshipDetail.degreeLevel')}
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {t(getDegreeLevelKey(scholarship.degreeLevel))}
              </Typography>
            </Box>

            {/* Funding Type */}
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('scholarshipDetail.fundingType')}
              </Typography>
              <Chip
                label={t(getFundingTypeKey(scholarship.fundingType))}
                color={getFundingColor(scholarship.fundingType)}
                size="small"
              />
            </Box>
          </Box>
        </Paper>

        {/* Action Buttons (desktop) */}
        {!isMobile && (
          <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
            {scholarship.officialLink && (
              <Button
                variant="contained"
                startIcon={<OpenInNew />}
                onClick={handleApplyNow}
              >
                {t('scholarshipDetail.applyNow')}
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<PlaylistAdd />}
              onClick={handleAddToTracking}
            >
              {t('scholarshipDetail.addToTracking')}
            </Button>
          </Box>
        )}

        {/* Tabbed Content */}
        <Paper sx={{ mb: 3 }}>
          <Tabs
            value={tabValue}
            onChange={(_, v) => setTabValue(v)}
            variant={isMobile ? 'scrollable' : 'standard'}
            scrollButtons={isMobile ? 'auto' : false}
            sx={{ borderBottom: 1, borderColor: 'divider' }}
          >
            <Tab label={t('scholarshipDetail.overview')} />
            <Tab label={t('scholarshipDetail.eligibility')} />
            <Tab label={t('scholarshipDetail.requirements')} />
            <Tab label={t('scholarshipDetail.howToApply')} />
            <Tab label={t('scholarshipDetail.documents')} />
          </Tabs>

          <Box sx={{ px: 3 }}>
            {/* Overview Tab */}
            <TabPanel value={tabValue} index={0}>
              {scholarship.overviewHtml ? (
                <Box
                  dangerouslySetInnerHTML={{
                    __html: sanitizeHtml(scholarship.overviewHtml),
                  }}
                  sx={{
                    '& a': { color: 'primary.main' },
                    '& p': { mb: 1.5 },
                    '& ul, & ol': { pl: 3 },
                  }}
                />
              ) : description ? (
                <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                  {description}
                </Typography>
              ) : (
                <Typography color="text.secondary">
                  {t('scholarshipDetail.notProvided')}
                </Typography>
              )}
            </TabPanel>

            {/* Eligibility Tab */}
            <TabPanel value={tabValue} index={1}>
              {scholarship.eligibilityDescription ? (
                <Typography variant="body1" sx={{ mb: 2, whiteSpace: 'pre-line' }}>
                  {scholarship.eligibilityDescription}
                </Typography>
              ) : (
                <Typography color="text.secondary" sx={{ mb: 2 }}>
                  {t('scholarshipDetail.notProvided')}
                </Typography>
              )}

              <Divider sx={{ my: 2 }} />

              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                  gap: 2,
                }}
              >
                {scholarship.minGPA != null && (
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">
                      {t('scholarshipDetail.minGPA')}
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                      {scholarship.minGPA}
                    </Typography>
                  </Box>
                )}
                {scholarship.maxAge != null && (
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">
                      {t('scholarshipDetail.maxAge')}
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                      {scholarship.maxAge}
                    </Typography>
                  </Box>
                )}
                {scholarship.eligibleCountries && (
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">
                      {t('scholarshipDetail.eligibleCountries')}
                    </Typography>
                    <Typography variant="body1">
                      {scholarship.eligibleCountries}
                    </Typography>
                  </Box>
                )}
                {scholarship.eligibleMajors && (
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">
                      {t('scholarshipDetail.eligibleMajors')}
                    </Typography>
                    <Typography variant="body1">
                      {scholarship.eligibleMajors}
                    </Typography>
                  </Box>
                )}
              </Box>
            </TabPanel>

            {/* Requirements Tab */}
            <TabPanel value={tabValue} index={2}>
              {scholarship.requiredDocuments ? (
                <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                  {scholarship.requiredDocuments}
                </Typography>
              ) : (
                <Typography color="text.secondary">
                  {t('scholarshipDetail.notProvided')}
                </Typography>
              )}
            </TabPanel>

            {/* How to Apply Tab */}
            <TabPanel value={tabValue} index={3}>
              {scholarship.howToApplyHtml ? (
                <Box
                  dangerouslySetInnerHTML={{
                    __html: sanitizeHtml(scholarship.howToApplyHtml),
                  }}
                  sx={{
                    '& a': { color: 'primary.main' },
                    '& p': { mb: 1.5 },
                    '& ul, & ol': { pl: 3 },
                  }}
                />
              ) : (
                <Typography color="text.secondary">
                  {t('scholarshipDetail.notProvided')}
                </Typography>
              )}
            </TabPanel>

            {/* Documents Tab */}
            <TabPanel value={tabValue} index={4}>
              {documentsChecklist.length > 0 ? (
                <List>
                  {documentsChecklist.map((doc, index) => (
                    <ListItem key={index} disablePadding sx={{ py: 0.5 }}>
                      <ListItemIcon sx={{ minWidth: 36 }}>
                        <CheckCircleOutline color="primary" fontSize="small" />
                      </ListItemIcon>
                      <ListItemText primary={doc} />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="text.secondary">
                  {t('scholarshipDetail.notProvided')}
                </Typography>
              )}
            </TabPanel>
          </Box>
        </Paper>
      </Container>

      {/* Mobile Sticky Bottom Bar */}
      {isMobile && (
        <Paper
          sx={{
            position: 'fixed',
            bottom: 0,
            left: 0,
            right: 0,
            zIndex: theme.zIndex.appBar,
            px: 2,
            py: 1.5,
            display: 'flex',
            gap: 1,
            borderTop: 1,
            borderColor: 'divider',
          }}
          elevation={8}
        >
          <IconButton
            onClick={handleBookmarkClick}
            disabled={saving}
            color={isSaved ? 'primary' : 'default'}
            size="small"
          >
            {isSaved ? <Bookmark /> : <BookmarkBorder />}
          </IconButton>
          <IconButton onClick={handleShare} size="small">
            <Share />
          </IconButton>
          {scholarship.officialLink && (
            <Button
              variant="contained"
              size="small"
              startIcon={<OpenInNew />}
              onClick={handleApplyNow}
              sx={{ flex: 1 }}
            >
              {t('scholarshipDetail.applyNow')}
            </Button>
          )}
          <Button
            variant="outlined"
            size="small"
            startIcon={<PlaylistAdd />}
            onClick={handleAddToTracking}
            sx={{ flex: 1 }}
          >
            {t('scholarshipDetail.addToTracking')}
          </Button>
        </Paper>
      )}

      {/* Tracking Modal */}
      {scholarship && (
        <TrackingModal
          open={trackingOpen}
          onClose={() => setTrackingOpen(false)}
          scholarshipId={scholarship.id}
          scholarshipTitle={title || scholarship.title}
        />
      )}

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={3000}
        onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={snackbar.severity}
          onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
          variant="filled"
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </>
  );
}
