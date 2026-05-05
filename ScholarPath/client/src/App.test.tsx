import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import './i18n';

describe('App', () => {
  it('renders app routes inside providers', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/']}>
          <App />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      await screen.findByRole('heading', { name: /scholarpath/i, level: 2 })
    ).toBeInTheDocument();
  });
});
