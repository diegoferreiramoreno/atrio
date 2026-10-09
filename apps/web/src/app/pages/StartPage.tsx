import { useSystemVersion } from '@/shared/api/systemApi';

export function StartPage() {
  const { data, error, isLoading, isError, refetch } = useSystemVersion();

  if (isLoading) {
    return (
      <main style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
        <h1>Átrio</h1>
        <p aria-live="polite">Carregando informações da plataforma...</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
        <h1>Átrio</h1>
        <div
          role="alert"
          style={{
            border: '1px solid #f87171',
            borderRadius: '8px',
            padding: '1.5rem',
            maxWidth: '480px',
            backgroundColor: '#fef2f2',
            color: '#991b1b',
          }}
        >
          <h2 style={{ margin: '0 0 0.5rem 0', fontSize: '1.25rem' }}>Erro de comunicação</h2>
          <p style={{ margin: '0 0 1rem 0' }}>
            Não foi possível carregar as informações do sistema. Verifique a conexão com a API e tente novamente.
          </p>
          {error instanceof Error && error.message && (
            <p style={{ fontSize: '0.875rem', opacity: 0.85, margin: '0 0 1rem 0' }}>
              Detalhes: {error.message}
            </p>
          )}
          <button
            type="button"
            onClick={() => void refetch()}
            style={{
              padding: '0.5rem 1rem',
              backgroundColor: '#b91c1c',
              color: '#ffffff',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 500,
            }}
          >
            Tentar novamente
          </button>
        </div>
      </main>
    );
  }

  return (
    <main style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Átrio</h1>
      <p style={{ color: '#4b5563', fontSize: '1rem', marginTop: '0.5rem' }}>
        Plataforma Oficial de Comunicação — Centro Cultural São José Sanchez Del Rio
      </p>
      {data && (
        <div style={{ marginTop: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span
            style={{
              display: 'inline-block',
              padding: '0.25rem 0.625rem',
              backgroundColor: '#e5e7eb',
              borderRadius: '9999px',
              fontSize: '0.875rem',
              fontWeight: 600,
              color: '#1f2937',
            }}
          >
            v{data.version}
          </span>
          <span style={{ fontSize: '0.8125rem', color: '#6b7280' }}>
            commit: <code>{data.commit.slice(0, 8)}</code>
          </span>
        </div>
      )}
    </main>
  );
}

export default StartPage;
