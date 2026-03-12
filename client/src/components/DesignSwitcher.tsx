import { useState } from 'react';
import {
  Box,
  Drawer,
  IconButton,
  Typography,
  Tooltip,
  useTheme,
} from '@mui/material';
import { Palette as PaletteIcon, Close as CloseIcon, Check as CheckIcon } from '@mui/icons-material';
import { useUiStore } from '@/stores/uiStore';
import { DESIGNS } from '@/theme/designs';

export default function DesignSwitcher() {
  const [open, setOpen] = useState(false);
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const designTheme = useUiStore((s) => s.designTheme);
  const setDesignTheme = useUiStore((s) => s.setDesignTheme);

  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  const active = (DESIGNS[designTheme] ?? DESIGNS[0])!;

  return (
    <>
      {/* Floating trigger */}
      <Tooltip title="Change Design Theme" placement="left">
        <Box
          onClick={() => setOpen(true)}
          sx={{
            position: 'fixed',
            bottom: 28,
            right: 28,
            zIndex: 1400,
            width: 48,
            height: 48,
            borderRadius: '50%',
            background: active.previewAccent,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            boxShadow: `0 4px 20px ${active.previewAccent}66`,
            transition: 'transform 0.2s, box-shadow 0.2s',
            '&:hover': {
              transform: 'scale(1.1)',
              boxShadow: `0 6px 28px ${active.previewAccent}99`,
            },
          }}
        >
          <PaletteIcon sx={{ fontSize: 22, color: isDark ? '#000' : '#fff', opacity: 0.9 }} />
        </Box>
      </Tooltip>

      {/* Side drawer panel */}
      <Drawer
        anchor="right"
        open={open}
        onClose={() => setOpen(false)}
        PaperProps={{
          sx: {
            width: 320,
            bgcolor: isDark ? '#0D0D0D' : '#FAFAFA',
            borderLeft: `1px solid ${isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.08)'}`,
            p: 3,
          },
        }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
          <Box>
            <Typography sx={{
              fontSize: '0.65rem',
              fontWeight: 700,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
              color: isDark ? 'rgba(255,255,255,0.35)' : 'rgba(0,0,0,0.35)',
              mb: 0.5,
            }}>
              Design System
            </Typography>
            <Typography sx={{
              fontSize: '1.25rem',
              fontWeight: 700,
              color: isDark ? '#fff' : '#000',
              lineHeight: 1,
            }}>
              Choose Theme
            </Typography>
          </Box>
          <IconButton
            onClick={() => setOpen(false)}
            size="small"
            sx={{ opacity: 0.5, '&:hover': { opacity: 1 } }}
          >
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>

        {/* Design cards */}
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          {DESIGNS.map((design) => {
            const isActive = designTheme === design.id;
            return (
              <Box
                key={design.id}
                onClick={() => { setDesignTheme(design.id); }}
                sx={{
                  p: 2,
                  borderRadius: 2,
                  cursor: 'pointer',
                  border: isActive
                    ? `2px solid ${design.previewAccent}`
                    : `1px solid ${isDark ? 'rgba(255,255,255,0.07)' : 'rgba(0,0,0,0.07)'}`,
                  bgcolor: isActive
                    ? `${design.previewAccent}10`
                    : isDark ? 'rgba(255,255,255,0.02)' : 'rgba(0,0,0,0.02)',
                  transition: 'all 0.18s ease',
                  position: 'relative',
                  overflow: 'hidden',
                  '&:hover': {
                    borderColor: design.previewAccent,
                    bgcolor: `${design.previewAccent}08`,
                    transform: 'translateX(-2px)',
                  },
                }}
              >
                {/* Accent bar */}
                <Box sx={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  bottom: 0,
                  width: 3,
                  bgcolor: design.previewAccent,
                  borderRadius: '2px 0 0 2px',
                }} />

                <Box sx={{ pl: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <Box>
                    {/* Design name + tagline */}
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                      <Typography sx={{
                        fontWeight: 700,
                        fontSize: '0.9rem',
                        color: isActive ? design.previewAccent : isDark ? '#fff' : '#000',
                        lineHeight: 1,
                      }}>
                        {design.name}
                      </Typography>
                      {isActive && (
                        <CheckIcon sx={{ fontSize: 14, color: design.previewAccent }} />
                      )}
                    </Box>
                    <Typography sx={{
                      fontSize: '0.7rem',
                      color: isDark ? 'rgba(255,255,255,0.35)' : 'rgba(0,0,0,0.4)',
                      mb: 1.5,
                    }}>
                      {design.tagline}
                    </Typography>

                    {/* Swatches */}
                    <Box sx={{ display: 'flex', gap: 0.5 }}>
                      {design.swatches.map((swatch, i) => (
                        <Box
                          key={i}
                          sx={{
                            width: 16,
                            height: 16,
                            borderRadius: '50%',
                            bgcolor: swatch,
                            border: `1.5px solid ${isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)'}`,
                          }}
                        />
                      ))}
                    </Box>
                  </Box>

                  {/* Font badge */}
                  <Box sx={{
                    px: 1,
                    py: 0.5,
                    borderRadius: 1,
                    bgcolor: `${design.previewAccent}18`,
                    border: `1px solid ${design.previewAccent}30`,
                  }}>
                    <Typography sx={{
                      fontSize: '0.6rem',
                      fontWeight: 600,
                      color: design.previewAccent,
                      letterSpacing: '0.05em',
                      lineHeight: 1,
                    }}>
                      {(design.displayFontLTR.split(',')[0] ?? '').replace(/"/g, '').trim().split(' ')[0]}
                    </Typography>
                  </Box>
                </Box>
              </Box>
            );
          })}
        </Box>

        {/* Footer hint */}
        <Typography sx={{
          mt: 3,
          fontSize: '0.68rem',
          color: isDark ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.25)',
          textAlign: 'center',
          lineHeight: 1.6,
        }}>
          Design preference is saved locally and persists across sessions.
        </Typography>
      </Drawer>
    </>
  );
}
