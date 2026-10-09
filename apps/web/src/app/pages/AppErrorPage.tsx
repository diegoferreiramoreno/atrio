interface AppErrorPageProps {
  error?: Error;
  resetErrorBoundary?: () => void;
}

export function AppErrorPage({ error, resetErrorBoundary }: AppErrorPageProps) {
  return (
    <main style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Átrio</h1>
      <div
        role="alert"
        style={{
          border: '1px solid #ef4444',
          borderRadius: '8px',
          padding: '1.5rem',
          maxWidth: '500px',
          backgroundColor: '#fef2f2',
          color: '#991b1b',
        }}
      >
        <h2 style={{ margin: '0 0 0.5rem 0' }}>Ocorreu uma falha inesperada na aplicação</h2>
        <p style={{ margin: '0 0 1rem 0' }}>
          {error?.message || 'Erro inesperado na renderização da interface.'}
        </p>
        {resetErrorBoundary && (
          <button
            type="button"
            onClick={resetErrorBoundary}
            style={{
              padding: '0.5rem 1rem',
              backgroundColor: '#b91c1c',
              color: '#ffffff',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
            }}
          >
            Recarregar componente
          </button>
        )}
      </div>
    </main>
  );
}

export default AppErrorPage;
