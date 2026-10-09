import { useQuery } from '@tanstack/react-query';
import { apiClient } from './apiClient';

export interface BuildInfo {
  version: string;
  commit: string;
}

export function fetchSystemVersion(signal?: AbortSignal): Promise<BuildInfo> {
  return apiClient<BuildInfo>('/system/version', { signal });
}

export function useSystemVersion() {
  return useQuery({
    queryKey: ['system', 'version'],
    queryFn: ({ signal }) => fetchSystemVersion(signal),
    staleTime: 1000 * 60 * 5, // 5 minutos
  });
}
