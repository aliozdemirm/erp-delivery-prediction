import type { ProblemDetails } from './predictionContracts';

export type PredictionErrorKind = 'validation' | 'calculationFailure';

export class PredictionApiError extends Error {
  constructor(
    message: string,
    readonly kind: PredictionErrorKind,
    readonly status?: number,
    readonly errorCode?: string,
  ) {
    super(message);
    this.name = 'PredictionApiError';
  }
}

const calculationErrorCodes = new Set([
  'CPM.CycleDetected',
  'CPM.MissingPredecessorReference',
]);

export function classifyProblem(problem: ProblemDetails, status: number): PredictionApiError {
  const message = problem.detail?.trim() || problem.title?.trim() || `Prediction request failed (${status}).`;
  const kind: PredictionErrorKind =
    calculationErrorCodes.has(problem.errorCode ?? '') || status !== 400
      ? 'calculationFailure'
      : 'validation';

  return new PredictionApiError(message, kind, status, problem.errorCode);
}

export function toPredictionApiError(error: unknown): PredictionApiError {
  if (error instanceof PredictionApiError) return error;
  if (error instanceof Error && error.name === 'AbortError') {
    return new PredictionApiError('The prediction request was cancelled.', 'calculationFailure');
  }
  return new PredictionApiError(
    error instanceof Error && error.message ? error.message : 'Unable to reach the prediction service.',
    'calculationFailure',
  );
}
