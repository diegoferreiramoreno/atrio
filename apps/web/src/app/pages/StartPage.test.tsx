import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { StartPage } from './StartPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  return {
    ...render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>),
    queryClient,
  };
}

describe('StartPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders Átrio and dynamic version on api success', async () => {
    const mockVersion = '0.1.0-alpha.42';
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(JSON.stringify({ version: mockVersion, commit: 'abcdef12' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    );

    renderWithClient(<StartPage />);

    expect(await screen.findByRole('heading', { level: 1, name: /Átrio/i })).toBeInTheDocument();
    expect(await screen.findByText(`v${mockVersion}`)).toBeInTheDocument();
  });

  it('renders recoverable error state with Tentar novamente action on api failure without blank screen', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          status: 500,
          code: 'internal_error',
          traceId: 'trace-xyz-123',
          title: 'Erro interno',
        }),
        {
          status: 500,
          headers: { 'Content-Type': 'application/problem+json' },
        }
      )
    );

    const user = userEvent.setup();
    renderWithClient(<StartPage />);

    // Assert que não é tela branca e exibe mensagem recuperável
    const errorMessage = await screen.findByText(/Não foi possível carregar as informações do sistema/i);
    expect(errorMessage).toBeInTheDocument();

    const retryButton = screen.getByRole('button', { name: /Tentar novamente/i });
    expect(retryButton).toBeInTheDocument();

    // Mock segunda chamada como sucesso ao clicar em tentar novamente
    const recoveredVersion = '0.1.0-recovered';
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(JSON.stringify({ version: recoveredVersion, commit: 'recovered1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    );

    await user.click(retryButton);

    await waitFor(() => {
      expect(screen.getByText(`v${recoveredVersion}`)).toBeInTheDocument();
    });
  });
});
