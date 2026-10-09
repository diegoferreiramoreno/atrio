import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <main style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Página não encontrada (404)</h1>
      <p style={{ margin: '1rem 0' }}>O endereço solicitado não existe no sistema Átrio.</p>
      <Link to="/" style={{ color: '#2563eb', textDecoration: 'underline' }}>
        Voltar para a página inicial
      </Link>
    </main>
  );
}

export default NotFoundPage;
