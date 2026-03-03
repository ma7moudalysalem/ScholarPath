import { Box } from '@mui/material';
import { HomeSlider } from '../components/home/HomeSlider';
import { ServicesSection } from '../components/home/ServicesSection';
import { TrustSection } from '../components/home/TrustSection';

export default function Home() {
  return (
    <Box>
      <HomeSlider />
      <ServicesSection />
      <TrustSection />
    </Box>
  );
}
