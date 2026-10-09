import { ApiProblem, isApiProblem } from './problemDetails';

export class ApiError extends Error {
  constructor(public readonly problem: ApiProblem) {
    super(problem.detail || problem.title || `API Error (${problem.code})`);
    this.name = 'ApiError';
  }
}

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
}

const API_BASE = '/api/v1';

export async function apiClient<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, signal, ...customConfig } = options;

  const url = endpoint.startsWith('http')
    ? endpoint
    : `${API_BASE}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const requestHeaders = new Headers(headers);
  if (body !== undefined && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json');
  }

  const response = await fetch(url, {
    ...customConfig,
    headers: requestHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    credentials: 'include', // Same-origin cookie auth
    signal,
  });

  if (!response.ok) {
    let problem: ApiProblem;

    try {
      const errorJson = await response.json();
      if (isApiProblem(errorJson)) {
        problem = errorJson;
      } else {
        problem = {
          status: response.status,
          code: 'http_error',
          title: response.statusText,
          traceId: response.headers.get('X-Trace-Id') || undefined,
        };
      }
    } catch {
      problem = {
        status: response.status,
        code: 'unexpected_error',
        title: response.statusText,
        traceId: response.headers.get('X-Trace-Id') || undefined,
      };
    }

    throw new ApiError(problem);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return response.json();
}
