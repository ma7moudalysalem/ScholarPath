export interface DesignTokens {
  id: number;
  name: string;
  tagline: string;
  swatches: string[]; // 5 colors for the mini swatch row in picker
  previewAccent: string; // main accent for preview card border/highlights

  // Fonts (LTR)
  displayFontLTR: string; // CSS font-family for h1-h3
  bodyFontLTR: string; // CSS font-family for UI/body
  // RTL always uses Cairo
  displayWeightLTR: number; // h1-h3 weight
  headingLetterSpacing: string; // e.g. '-0.02em' or '0'

  // Dark palette
  dark: {
    bg: string;
    paper: string;
    elevated: string;
    primary: string;
    primaryLight: string;
    primaryDark: string;
    primaryContrast: string;
    secondary: string;
    secondaryContrast: string;
    textPrimary: string;
    textSecondary: string;
    textDisabled: string;
    divider: string;
    hoverBg: string;
    selectedBg: string;
    // For glassmorphism appbar
    appBarBg: string;
    sidebarBg: string;
    // Card border
    cardBorder: string;
    cardBorderHover: string;
    // Scrollbar
    scrollbarThumb: string;
    scrollbarTrack: string;
  };

  // Light palette
  light: {
    bg: string;
    paper: string;
    elevated: string;
    primary: string;
    primaryLight: string;
    primaryDark: string;
    primaryContrast: string;
    secondary: string;
    secondaryContrast: string;
    textPrimary: string;
    textSecondary: string;
    textDisabled: string;
    divider: string;
    hoverBg: string;
    selectedBg: string;
    appBarBg: string;
    sidebarBg: string;
    cardBorder: string;
    cardBorderHover: string;
    scrollbarThumb: string;
    scrollbarTrack: string;
  };

  // Shape
  borderRadius: number; // base
  buttonBorderRadius: number; // buttons
  cardBorderRadius: number; // cards
  inputBorderRadius: number; // inputs

  // Button style: 'gradient' | 'flat' | 'glass'
  buttonStyle: 'gradient' | 'flat' | 'glass';
  // Card style: affects hover effects
  cardGlow: boolean; // glow effect on hover
  // Sidebar: show indicator as 'bar' | 'dot' | 'pill'
  activeIndicator: 'bar' | 'dot' | 'pill';
}

export const ARABIC_FONT = '"Cairo", "Roboto", "Helvetica", "Arial", sans-serif';

export const DESIGNS: DesignTokens[] = [
  // ─── 0: OBSIDIAN GOLD ─────────────────────────────────────────────────
  {
    id: 0,
    name: 'Obsidian Gold',
    tagline: 'Scholarly Luxury',
    swatches: ['#070C18', '#0E1524', '#C9A534', '#4F7EF5', '#EDE8DF'],
    previewAccent: '#C9A534',
    displayFontLTR: '"Cormorant Garamond", "Georgia", serif',
    bodyFontLTR: '"DM Sans", "Helvetica Neue", "Arial", sans-serif',
    displayWeightLTR: 600,
    headingLetterSpacing: '-0.02em',
    dark: {
      bg: '#070C18',
      paper: '#0E1524',
      elevated: '#131E35',
      primary: '#C9A534',
      primaryLight: '#E0BE68',
      primaryDark: '#9B7A1A',
      primaryContrast: '#07090F',
      secondary: '#4F7EF5',
      secondaryContrast: '#ffffff',
      textPrimary: '#EDE8DF',
      textSecondary: '#7D8FA8',
      textDisabled: '#3D4F68',
      divider: 'rgba(201,165,52,0.12)',
      hoverBg: 'rgba(201,165,52,0.07)',
      selectedBg: 'rgba(201,165,52,0.13)',
      appBarBg: 'rgba(7,12,24,0.88)',
      sidebarBg: '#05090F',
      cardBorder: 'rgba(201,165,52,0.1)',
      cardBorderHover: 'rgba(201,165,52,0.28)',
      scrollbarThumb: '#1E2D46',
      scrollbarTrack: '#070C18',
    },
    light: {
      bg: '#F8F5EF',
      paper: '#FFFFFF',
      elevated: '#F0ECE3',
      primary: '#8B6914',
      primaryLight: '#C9A534',
      primaryDark: '#5C440D',
      primaryContrast: '#ffffff',
      secondary: '#2D5BE3',
      secondaryContrast: '#ffffff',
      textPrimary: '#0F1628',
      textSecondary: '#5A6878',
      textDisabled: '#9CA3AF',
      divider: 'rgba(139,105,20,0.15)',
      hoverBg: 'rgba(139,105,20,0.06)',
      selectedBg: 'rgba(139,105,20,0.10)',
      appBarBg: 'rgba(248,245,239,0.93)',
      sidebarBg: '#0B1428',
      cardBorder: 'rgba(0,0,0,0.06)',
      cardBorderHover: 'rgba(139,105,20,0.2)',
      scrollbarThumb: '#C2B48A',
      scrollbarTrack: '#F0EDE6',
    },
    borderRadius: 10,
    buttonBorderRadius: 8,
    cardBorderRadius: 14,
    inputBorderRadius: 10,
    buttonStyle: 'gradient',
    cardGlow: false,
    activeIndicator: 'bar',
  },

  // ─── 1: AURORA MIDNIGHT ───────────────────────────────────────────────
  {
    id: 1,
    name: 'Aurora Midnight',
    tagline: 'Futuristic & Tech',
    swatches: ['#050814', '#090E20', '#00C8E0', '#7B5EF8', '#E8F4FF'],
    previewAccent: '#00C8E0',
    displayFontLTR: '"Sora", "Helvetica Neue", sans-serif',
    bodyFontLTR: '"Sora", "Helvetica Neue", sans-serif',
    displayWeightLTR: 700,
    headingLetterSpacing: '-0.03em',
    dark: {
      bg: '#050814',
      paper: '#090E20',
      elevated: '#0E1530',
      primary: '#00C8E0',
      primaryLight: '#40D9EC',
      primaryDark: '#009CB0',
      primaryContrast: '#030810',
      secondary: '#7B5EF8',
      secondaryContrast: '#ffffff',
      textPrimary: '#E8F4FF',
      textSecondary: '#8BA8C8',
      textDisabled: '#2D4060',
      divider: 'rgba(0,200,224,0.12)',
      hoverBg: 'rgba(0,200,224,0.07)',
      selectedBg: 'rgba(0,200,224,0.13)',
      appBarBg: 'rgba(5,8,20,0.9)',
      sidebarBg: '#03050F',
      cardBorder: 'rgba(0,200,224,0.12)',
      cardBorderHover: 'rgba(0,200,224,0.35)',
      scrollbarThumb: '#1A2A4A',
      scrollbarTrack: '#050814',
    },
    light: {
      bg: '#F0F7FF',
      paper: '#FFFFFF',
      elevated: '#E4F0FC',
      primary: '#0284C7',
      primaryLight: '#38BDF8',
      primaryDark: '#0369A1',
      primaryContrast: '#ffffff',
      secondary: '#6D4AE0',
      secondaryContrast: '#ffffff',
      textPrimary: '#0A1628',
      textSecondary: '#4A6080',
      textDisabled: '#9BB0C8',
      divider: 'rgba(2,132,199,0.15)',
      hoverBg: 'rgba(2,132,199,0.06)',
      selectedBg: 'rgba(2,132,199,0.10)',
      appBarBg: 'rgba(240,247,255,0.93)',
      sidebarBg: '#03050F',
      cardBorder: 'rgba(2,132,199,0.1)',
      cardBorderHover: 'rgba(2,132,199,0.3)',
      scrollbarThumb: '#38BDF8',
      scrollbarTrack: '#E4F0FC',
    },
    borderRadius: 14,
    buttonBorderRadius: 50,
    cardBorderRadius: 18,
    inputBorderRadius: 14,
    buttonStyle: 'gradient',
    cardGlow: true,
    activeIndicator: 'pill',
  },

  // ─── 2: IVORY ACADEMIA ────────────────────────────────────────────────
  {
    id: 2,
    name: 'Ivory Academia',
    tagline: 'Classic Editorial',
    swatches: ['#FAF7F0', '#1A2D5B', '#C41E3A', '#8B9DC3', '#2D3748'],
    previewAccent: '#C41E3A',
    displayFontLTR: '"Playfair Display", "Georgia", serif',
    bodyFontLTR: '"DM Sans", "Helvetica Neue", "Arial", sans-serif',
    displayWeightLTR: 700,
    headingLetterSpacing: '-0.01em',
    dark: {
      bg: '#12100A',
      paper: '#1C1912',
      elevated: '#252218',
      primary: '#E8A83A',
      primaryLight: '#F0C060',
      primaryDark: '#C07820',
      primaryContrast: '#12100A',
      secondary: '#C41E3A',
      secondaryContrast: '#ffffff',
      textPrimary: '#F5E8D8',
      textSecondary: '#9E8E74',
      textDisabled: '#4A3E2E',
      divider: 'rgba(232,168,58,0.12)',
      hoverBg: 'rgba(232,168,58,0.07)',
      selectedBg: 'rgba(232,168,58,0.13)',
      appBarBg: 'rgba(18,16,10,0.92)',
      sidebarBg: '#0E0C08',
      cardBorder: 'rgba(232,168,58,0.1)',
      cardBorderHover: 'rgba(232,168,58,0.3)',
      scrollbarThumb: '#3A2E1A',
      scrollbarTrack: '#12100A',
    },
    light: {
      bg: '#FAF7F0',
      paper: '#FFFFFF',
      elevated: '#F4F0E8',
      primary: '#1A2D5B',
      primaryLight: '#2D4A8A',
      primaryDark: '#0F1A35',
      primaryContrast: '#ffffff',
      secondary: '#C41E3A',
      secondaryContrast: '#ffffff',
      textPrimary: '#1A1209',
      textSecondary: '#5A4E3A',
      textDisabled: '#9E9280',
      divider: 'rgba(26,45,91,0.12)',
      hoverBg: 'rgba(26,45,91,0.05)',
      selectedBg: 'rgba(26,45,91,0.09)',
      appBarBg: 'rgba(250,247,240,0.95)',
      sidebarBg: '#0D1A3A',
      cardBorder: 'rgba(26,45,91,0.08)',
      cardBorderHover: 'rgba(26,45,91,0.2)',
      scrollbarThumb: '#8B9DC3',
      scrollbarTrack: '#F4F0E8',
    },
    borderRadius: 6,
    buttonBorderRadius: 4,
    cardBorderRadius: 8,
    inputBorderRadius: 6,
    buttonStyle: 'flat',
    cardGlow: false,
    activeIndicator: 'bar',
  },

  // ─── 3: NEON BRUTALIST ────────────────────────────────────────────────
  {
    id: 3,
    name: 'Neon Brutalist',
    tagline: 'Bold & Raw Energy',
    swatches: ['#0A0A0A', '#111111', '#00FF88', '#FF0080', '#F0F0F0'],
    previewAccent: '#00FF88',
    displayFontLTR: '"Syne", "Impact", "Arial Black", sans-serif',
    bodyFontLTR: '"Outfit", "Helvetica Neue", sans-serif',
    displayWeightLTR: 800,
    headingLetterSpacing: '-0.04em',
    dark: {
      bg: '#0A0A0A',
      paper: '#111111',
      elevated: '#1A1A1A',
      primary: '#00FF88',
      primaryLight: '#40FFB0',
      primaryDark: '#00CC6A',
      primaryContrast: '#0A0A0A',
      secondary: '#FF0080',
      secondaryContrast: '#ffffff',
      textPrimary: '#F0F0F0',
      textSecondary: '#888888',
      textDisabled: '#444444',
      divider: 'rgba(0,255,136,0.15)',
      hoverBg: 'rgba(0,255,136,0.08)',
      selectedBg: 'rgba(0,255,136,0.14)',
      appBarBg: 'rgba(10,10,10,0.95)',
      sidebarBg: '#060606',
      cardBorder: 'rgba(0,255,136,0.15)',
      cardBorderHover: 'rgba(0,255,136,0.5)',
      scrollbarThumb: '#333333',
      scrollbarTrack: '#0A0A0A',
    },
    light: {
      bg: '#F5F5F5',
      paper: '#FFFFFF',
      elevated: '#EBEBEB',
      primary: '#0A0A0A',
      primaryLight: '#333333',
      primaryDark: '#000000',
      primaryContrast: '#00FF88',
      secondary: '#FF0080',
      secondaryContrast: '#ffffff',
      textPrimary: '#0A0A0A',
      textSecondary: '#555555',
      textDisabled: '#AAAAAA',
      divider: 'rgba(0,0,0,0.12)',
      hoverBg: 'rgba(0,0,0,0.05)',
      selectedBg: 'rgba(0,0,0,0.09)',
      appBarBg: 'rgba(245,245,245,0.95)',
      sidebarBg: '#060606',
      cardBorder: 'rgba(0,0,0,0.1)',
      cardBorderHover: 'rgba(0,255,136,0.5)',
      scrollbarThumb: '#AAAAAA',
      scrollbarTrack: '#EBEBEB',
    },
    borderRadius: 4,
    buttonBorderRadius: 0,
    cardBorderRadius: 4,
    inputBorderRadius: 4,
    buttonStyle: 'flat',
    cardGlow: true,
    activeIndicator: 'pill',
  },

  // ─── 4: COSMIC AMETHYST ───────────────────────────────────────────────
  {
    id: 4,
    name: 'Cosmic Amethyst',
    tagline: 'Deep Space & Creative',
    swatches: ['#0C0819', '#120D26', '#B568F0', '#FF6B35', '#EDE8FF'],
    previewAccent: '#B568F0',
    displayFontLTR: '"Fraunces", "Georgia", serif',
    bodyFontLTR: '"Plus Jakarta Sans", "Helvetica Neue", sans-serif',
    displayWeightLTR: 700,
    headingLetterSpacing: '-0.02em',
    dark: {
      bg: '#0C0819',
      paper: '#120D26',
      elevated: '#1A1235',
      primary: '#B568F0',
      primaryLight: '#CF8FF8',
      primaryDark: '#8A3EC8',
      primaryContrast: '#0C0819',
      secondary: '#FF6B35',
      secondaryContrast: '#ffffff',
      textPrimary: '#EDE8FF',
      textSecondary: '#9E8FC0',
      textDisabled: '#3D2E60',
      divider: 'rgba(181,104,240,0.12)',
      hoverBg: 'rgba(181,104,240,0.08)',
      selectedBg: 'rgba(181,104,240,0.14)',
      appBarBg: 'rgba(12,8,25,0.9)',
      sidebarBg: '#08051A',
      cardBorder: 'rgba(181,104,240,0.12)',
      cardBorderHover: 'rgba(181,104,240,0.35)',
      scrollbarThumb: '#2A1A50',
      scrollbarTrack: '#0C0819',
    },
    light: {
      bg: '#FAF8FF',
      paper: '#FFFFFF',
      elevated: '#F2EEF9',
      primary: '#7C3AED',
      primaryLight: '#9D6FF5',
      primaryDark: '#5B21B6',
      primaryContrast: '#ffffff',
      secondary: '#EA580C',
      secondaryContrast: '#ffffff',
      textPrimary: '#1A0A30',
      textSecondary: '#6B5E88',
      textDisabled: '#B8ACC8',
      divider: 'rgba(124,58,237,0.12)',
      hoverBg: 'rgba(124,58,237,0.05)',
      selectedBg: 'rgba(124,58,237,0.09)',
      appBarBg: 'rgba(250,248,255,0.93)',
      sidebarBg: '#08051A',
      cardBorder: 'rgba(124,58,237,0.08)',
      cardBorderHover: 'rgba(124,58,237,0.25)',
      scrollbarThumb: '#9D6FF5',
      scrollbarTrack: '#F2EEF9',
    },
    borderRadius: 12,
    buttonBorderRadius: 50,
    cardBorderRadius: 16,
    inputBorderRadius: 12,
    buttonStyle: 'gradient',
    cardGlow: true,
    activeIndicator: 'dot',
  },
];
