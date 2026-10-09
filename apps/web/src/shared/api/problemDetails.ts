export interface ApiProblem {
  status: number;
  code: string;
  title?: string;
  detail?: string;
  traceId?: string;
  validationErrors?: Record<string, string[]>;
  [key: string]: unknown;
}

export function isApiProblem(data: unknown): data is ApiProblem {
  if (typeof data !== 'object' || data === null) {
    return false;
  }
  const candidate = data as Record<string, unknown>;
  return typeof candidate.status === 'number' && typeof candidate.code === 'string';
}
