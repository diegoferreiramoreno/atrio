import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        // Não faz retry para 401, 403, 404
        if (
          error instanceof Error &&
          'problem' in error &&
          typeof (error as { problem: { status: number } }).problem?.status === 'number'
        ) {
          const status = (error as { problem: { status: number } }).problem.status;
          if (status === 401 || status === 403 || status === 404) {
            return false;
          }
        }
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,
    },
  },
});
