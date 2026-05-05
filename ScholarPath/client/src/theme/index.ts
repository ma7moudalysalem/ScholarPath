import { createTheme, type PaletteMode } from '@mui/material/styles';
import { DESIGNS, ARABIC_FONT, type DesignTokens } from './designs';

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Build the contained button background/shadow based on design style */
function containedButtonStyles(
  design: DesignTokens,
  primary: string,
  primaryLight: string,
  primaryDark: string,
  isDark: boolean
) {
  if (design.buttonStyle === 'gradient') {
    const from = isDark ? primary : primaryDark;
    const to = isDark ? primaryLight : primary;
    const fromHover = isDark ? primaryLight : primary;
    const toHover = isDark ? `${primaryLight}CC` : primaryLight;
    return {
      background: `linear-gradient(135deg, ${from} 0%, ${to} 100%)`,
      boxShadow: `0 4px 20px ${primary}40`,
      '&:hover': {
        background: `linear-gradient(135deg, ${fromHover} 0%, ${toHover} 100%)`,
        boxShadow: `0 6px 24px ${primary}66`,
        transform: 'translateY(-1px)',
      },
      '&:active': { transform: 'translateY(0)' },
    };
  }
  // flat
  return {
    background: primary,
    boxShadow: 'none',
    '&:hover': {
      background: isDark ? primaryLight : primaryDark,
      boxShadow: 'none',
      transform: 'translateY(-1px)',
    },
    '&:active': { transform: 'translateY(0)' },
  };
}

/** Card hover styles — with optional glow */
function cardHoverStyles(
  design: DesignTokens,
  primary: string,
  cardBorderHover: string,
  isDark: boolean
) {
  const baseHover = {
    borderColor: cardBorderHover,
    transform: 'translateY(-2px)',
  };

  if (design.id === 3) {
    // Neon Brutalist: sharp offset shadow, no blur
    return {
      ...baseHover,
      boxShadow: isDark ? `4px 4px 0 ${primary}` : `4px 4px 0 ${primary}`,
      transform: 'translate(-2px, -2px)',
    };
  }

  if (design.cardGlow) {
    return {
      ...baseHover,
      boxShadow: isDark
        ? `0 12px 40px rgba(0,0,0,0.5), 0 0 20px ${primary}30`
        : `0 8px 28px rgba(0,0,0,0.1), 0 0 12px ${primary}20`,
    };
  }

  return {
    ...baseHover,
    boxShadow: isDark
      ? `0 12px 40px rgba(0,0,0,0.5), 0 0 0 1px ${cardBorderHover}`
      : '0 8px 28px rgba(0,0,0,0.1)',
  };
}

/** Active indicator pseudo-element for sidebar ListItemButton */
function activeIndicatorStyles(design: DesignTokens, primary: string) {
  const { activeIndicator } = design;

  if (activeIndicator === 'bar') {
    return {
      content: '""',
      position: 'absolute' as const,
      insetInlineStart: 0,
      top: '22%',
      height: '56%',
      width: design.id === 3 ? 4 : 3,
      borderRadius: '0 2px 2px 0',
      backgroundColor: primary,
    };
  }

  if (activeIndicator === 'pill') {
    return {
      content: '""',
      position: 'absolute' as const,
      insetInlineStart: 6,
      top: '50%',
      transform: 'translateY(-50%)',
      height: '60%',
      width: 4,
      borderRadius: 4,
      backgroundColor: primary,
    };
  }

  // dot
  return {
    content: '""',
    position: 'absolute' as const,
    insetInlineStart: 8,
    top: '50%',
    transform: 'translateY(-50%)',
    height: 6,
    width: 6,
    borderRadius: '50%',
    backgroundColor: primary,
  };
}

// ─── Main factory ─────────────────────────────────────────────────────────────

export function createScholarPathTheme(
  direction: 'ltr' | 'rtl',
  mode: PaletteMode = 'light',
  designId: number = 0
) {
  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  const design: DesignTokens = (DESIGNS[designId] ?? DESIGNS[0])!;
  const pal = mode === 'dark' ? design.dark : design.light;
  const isDark = mode === 'dark';
  const isRTL = direction === 'rtl';

  // ── Typography fonts ────────────────────────────────────────────────────
  const bodyFont = isRTL ? ARABIC_FONT : design.bodyFontLTR;
  const displayFont = isRTL ? ARABIC_FONT : design.displayFontLTR;
  const displayWeight = isRTL ? 700 : design.displayWeightLTR;
  const hls = isRTL ? '0' : design.headingLetterSpacing;

  // ── Status colours (standard MUI defaults, mode-adjusted) ───────────────
  const error = isDark
    ? { main: '#EF4444', light: '#F87171', dark: '#B91C1C', contrastText: '#fff' }
    : { main: '#DC2626', light: '#EF4444', dark: '#991B1B', contrastText: '#fff' };
  const warning = isDark
    ? { main: '#F59E0B', light: '#FCD34D', dark: '#B45309', contrastText: '#fff' }
    : { main: '#D97706', light: '#F59E0B', dark: '#92400E', contrastText: '#fff' };
  const success = isDark
    ? { main: '#10B981', light: '#34D399', dark: '#047857', contrastText: '#fff' }
    : { main: '#059669', light: '#10B981', dark: '#065F46', contrastText: '#fff' };
  const info = isDark
    ? { main: '#38BDF8', light: '#7DD3FC', dark: '#0284C7', contrastText: '#fff' }
    : { main: '#0284C7', light: '#38BDF8', dark: '#075985', contrastText: '#fff' };

  // ── Brutalist border width ───────────────────────────────────────────────
  const isBrutalist = design.id === 3;
  const borderWidth = isBrutalist ? '2px' : '1px';

  // ── Contained button styles ──────────────────────────────────────────────
  const containedBtn = containedButtonStyles(
    design,
    pal.primary,
    pal.primaryLight,
    pal.primaryDark,
    isDark
  );

  return createTheme({
    direction,
    palette: {
      mode,
      primary: {
        main: pal.primary,
        light: pal.primaryLight,
        dark: pal.primaryDark,
        contrastText: pal.primaryContrast,
      },
      secondary: {
        main: pal.secondary,
        contrastText: pal.secondaryContrast,
      },
      error,
      warning,
      success,
      info,
      background: {
        default: pal.bg,
        paper: pal.paper,
      },
      text: {
        primary: pal.textPrimary,
        secondary: pal.textSecondary,
        disabled: pal.textDisabled,
      },
      divider: pal.divider,
      action: {
        hover: pal.hoverBg,
        selected: pal.selectedBg,
        disabledBackground: `${pal.primary}20`,
        focus: `${pal.primary}1F`,
      },
    },

    shape: { borderRadius: design.borderRadius },

    typography: {
      fontFamily: bodyFont,
      h1: {
        fontFamily: displayFont,
        fontWeight: displayWeight,
        fontSize: '3.5rem',
        lineHeight: 1.08,
        letterSpacing: hls,
      },
      h2: {
        fontFamily: displayFont,
        fontWeight: displayWeight,
        fontSize: '2.75rem',
        lineHeight: 1.15,
        letterSpacing: hls,
      },
      h3: {
        fontFamily: displayFont,
        fontWeight: isRTL
          ? Math.max(displayWeight - 100, 400)
          : Math.max(design.displayWeightLTR - 100, 400),
        fontSize: '2.125rem',
        lineHeight: 1.2,
        letterSpacing: hls,
      },
      h4: {
        fontFamily: bodyFont,
        fontWeight: 600,
        fontSize: '1.5rem',
        lineHeight: 1.3,
      },
      h5: {
        fontFamily: bodyFont,
        fontWeight: 600,
        fontSize: '1.25rem',
        lineHeight: 1.4,
      },
      h6: {
        fontFamily: bodyFont,
        fontWeight: 600,
        fontSize: '1.0625rem',
        lineHeight: 1.5,
      },
      subtitle1: { fontWeight: 500, fontSize: '1rem' },
      subtitle2: { fontWeight: 500, fontSize: '0.875rem' },
      body1: { fontSize: '1rem', lineHeight: 1.65 },
      body2: { fontSize: '0.875rem', lineHeight: 1.65 },
      button: { fontWeight: 600, letterSpacing: '0.01em' },
      caption: { fontSize: '0.75rem', letterSpacing: '0.02em' },
      overline: { fontWeight: 600, letterSpacing: '0.12em', fontSize: '0.6875rem' },
    },

    components: {
      // ── CSS Baseline / Scrollbar ───────────────────────────────────────
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            scrollbarWidth: 'thin',
            scrollbarColor: `${pal.scrollbarThumb} ${pal.scrollbarTrack}`,
            '&::-webkit-scrollbar': { width: '7px', height: '7px' },
            '&::-webkit-scrollbar-track': { background: pal.scrollbarTrack },
            '&::-webkit-scrollbar-thumb': {
              background: pal.scrollbarThumb,
              borderRadius: isBrutalist ? '0' : '4px',
              '&:hover': { filter: 'brightness(1.3)' },
            },
          },
        },
      },

      // ── Button ────────────────────────────────────────────────────────
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: {
            textTransform: 'none',
            fontWeight: 600,
            borderRadius: design.buttonBorderRadius,
            letterSpacing: '0.01em',
            transition: 'all 0.2s ease',
          },
          contained: {
            ...containedBtn,
            color: pal.primaryContrast,
          },
          outlined: {
            borderColor: pal.primary,
            borderWidth,
            color: pal.primary,
            '&:hover': {
              borderColor: pal.primaryLight,
              borderWidth,
              background: pal.hoverBg,
              transform: 'translateY(-1px)',
            },
          },
          text: {
            color: pal.primary,
            '&:hover': { background: pal.hoverBg },
          },
          sizeLarge: { padding: '12px 28px', fontSize: '1rem' },
          sizeMedium: { padding: '9px 20px' },
          sizeSmall: { padding: '6px 14px', fontSize: '0.8125rem' },
        },
      },

      // ── Card ──────────────────────────────────────────────────────────
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: design.cardBorderRadius,
            border: `${borderWidth} solid ${pal.cardBorder}`,
            backgroundImage: 'none',
            boxShadow: isBrutalist
              ? 'none'
              : isDark
                ? '0 4px 24px rgba(0,0,0,0.4)'
                : '0 2px 16px rgba(0,0,0,0.06)',
            transition: isBrutalist
              ? 'border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease'
              : 'all 0.25s ease',
            '&:hover': cardHoverStyles(design, pal.primary, pal.cardBorderHover, isDark),
          },
        },
      },

      // ── Paper ─────────────────────────────────────────────────────────
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            border: `${borderWidth} solid ${isDark ? `${pal.primary}14` : `${pal.primary}0D`}`,
          },
          elevation0: { border: 'none' },
          elevation1: {
            boxShadow: isBrutalist
              ? `2px 2px 0 ${pal.primary}40`
              : isDark
                ? '0 2px 12px rgba(0,0,0,0.35)'
                : '0 1px 8px rgba(0,0,0,0.06)',
          },
          elevation2: {
            boxShadow: isBrutalist
              ? `3px 3px 0 ${pal.primary}60`
              : isDark
                ? '0 4px 20px rgba(0,0,0,0.45)'
                : '0 2px 14px rgba(0,0,0,0.08)',
          },
          elevation3: {
            boxShadow: isBrutalist
              ? `4px 4px 0 ${pal.primary}`
              : isDark
                ? '0 8px 32px rgba(0,0,0,0.5)'
                : '0 4px 20px rgba(0,0,0,0.1)',
          },
        },
      },

      // ── AppBar (always glassmorphism) ─────────────────────────────────
      MuiAppBar: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            backgroundColor: pal.appBarBg,
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
            borderBottom: `${borderWidth} solid ${pal.divider}`,
            boxShadow: 'none',
          },
        },
      },

      // ── Drawer (always uses sidebarBg) ────────────────────────────────
      MuiDrawer: {
        styleOverrides: {
          paper: {
            backgroundImage: 'none',
            backgroundColor: pal.sidebarBg,
            borderRight: 'none',
            boxShadow: isBrutalist ? `4px 0 0 ${pal.primary}` : '4px 0 32px rgba(0,0,0,0.6)',
          },
        },
      },

      // ── ListItemButton (sidebar nav items) ───────────────────────────
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: design.borderRadius,
            marginBottom: 2,
            transition: 'all 0.2s ease',
            position: 'relative',
            '&:hover': { backgroundColor: pal.hoverBg },
            '&.Mui-selected': {
              backgroundColor: pal.selectedBg,
              '&::before': activeIndicatorStyles(design, pal.primary),
              '&:hover': { backgroundColor: `${pal.primary}20` },
            },
          },
        },
      },

      // ── ListItemIcon ──────────────────────────────────────────────────
      MuiListItemIcon: {
        styleOverrides: {
          root: { minWidth: 40 },
        },
      },

      // ── TextField ────────────────────────────────────────────────────
      MuiTextField: {
        defaultProps: { variant: 'outlined' },
        styleOverrides: {
          root: {
            '& .MuiOutlinedInput-root': {
              borderRadius: design.inputBorderRadius,
              backgroundColor: isDark ? 'rgba(255,255,255,0.03)' : 'rgba(0,0,0,0.02)',
              '& fieldset': {
                borderColor: `${pal.primary}33`,
                borderWidth,
                transition: 'border-color 0.2s',
              },
              '&:hover fieldset': {
                borderColor: `${pal.primary}80`,
                borderWidth,
              },
              '&.Mui-focused fieldset': {
                borderColor: pal.primary,
                borderWidth: isBrutalist ? '2px' : '1.5px',
              },
            },
            '& .MuiInputLabel-root.Mui-focused': { color: pal.primary },
          },
        },
      },

      // ── Chip ─────────────────────────────────────────────────────────
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: isBrutalist ? 4 : design.borderRadius,
            fontWeight: 500,
          },
          outlined: { borderColor: `${pal.primary}4D`, borderWidth },
        },
      },

      // ── Avatar ───────────────────────────────────────────────────────
      MuiAvatar: {
        styleOverrides: {
          root: {
            background:
              design.buttonStyle === 'gradient'
                ? `linear-gradient(135deg, ${pal.primary}, ${pal.primaryDark})`
                : pal.primary,
            color: pal.primaryContrast,
            fontWeight: 700,
          },
        },
      },

      // ── Divider ──────────────────────────────────────────────────────
      MuiDivider: {
        styleOverrides: {
          root: { borderColor: pal.divider },
        },
      },

      // ── LinearProgress ───────────────────────────────────────────────
      MuiLinearProgress: {
        styleOverrides: {
          root: { borderRadius: isBrutalist ? 0 : 4, height: isBrutalist ? 8 : 6 },
          bar: { borderRadius: isBrutalist ? 0 : 4 },
        },
      },

      // ── Alert ────────────────────────────────────────────────────────
      MuiAlert: {
        styleOverrides: {
          root: {
            borderRadius: design.borderRadius,
            border: isBrutalist ? `${borderWidth} solid currentColor` : undefined,
          },
        },
      },

      // ── Dialog ───────────────────────────────────────────────────────
      MuiDialog: {
        styleOverrides: {
          paper: {
            borderRadius: design.cardBorderRadius,
            border: `${borderWidth} solid ${pal.cardBorder}`,
            backgroundImage: 'none',
            backgroundColor: pal.paper,
          },
        },
      },

      // ── Tooltip ──────────────────────────────────────────────────────
      MuiTooltip: {
        styleOverrides: {
          tooltip: {
            borderRadius: isBrutalist ? 0 : design.borderRadius,
            backgroundColor: isDark ? pal.elevated : pal.textPrimary,
            color: isDark ? pal.textPrimary : pal.bg,
            fontSize: '0.75rem',
            boxShadow: isBrutalist ? `2px 2px 0 ${pal.primary}` : '0 4px 12px rgba(0,0,0,0.25)',
            border: isBrutalist ? `1px solid ${pal.primary}` : undefined,
          },
          arrow: {
            color: isDark ? pal.elevated : pal.textPrimary,
          },
        },
      },

      // ── Badge ────────────────────────────────────────────────────────
      MuiBadge: {
        styleOverrides: {
          badge: {
            fontWeight: 600,
            fontSize: '0.6875rem',
            borderRadius: isBrutalist ? 2 : undefined,
          },
        },
      },

      // ── IconButton ───────────────────────────────────────────────────
      MuiIconButton: {
        styleOverrides: {
          root: {
            borderRadius: isBrutalist ? design.borderRadius : design.borderRadius,
            transition: 'all 0.2s ease',
            '&:hover': {
              backgroundColor: pal.hoverBg,
              ...(isBrutalist && { outline: `2px solid ${pal.primary}` }),
            },
          },
        },
      },

      // ── Menu ─────────────────────────────────────────────────────────
      MuiMenu: {
        styleOverrides: {
          paper: {
            borderRadius: design.borderRadius,
            border: `${borderWidth} solid ${pal.cardBorder}`,
            backgroundImage: 'none',
            backgroundColor: pal.paper,
            boxShadow: isBrutalist
              ? `4px 4px 0 ${pal.primary}`
              : isDark
                ? '0 8px 32px rgba(0,0,0,0.5)'
                : '0 8px 24px rgba(0,0,0,0.12)',
          },
        },
      },

      // ── MenuItem ─────────────────────────────────────────────────────
      MuiMenuItem: {
        styleOverrides: {
          root: {
            borderRadius: isBrutalist ? 0 : Math.max(design.borderRadius - 4, 4),
            margin: isBrutalist ? '0' : '2px 8px',
            padding: '8px 12px',
            '&:hover': { backgroundColor: pal.hoverBg },
            '&.Mui-selected': {
              backgroundColor: pal.selectedBg,
              '&:hover': { backgroundColor: `${pal.primary}20` },
            },
          },
        },
      },
    },
  });
}
