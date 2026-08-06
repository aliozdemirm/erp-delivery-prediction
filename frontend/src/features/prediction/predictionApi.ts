import type {
  CalculatePredictionRequest,
  MaterialShortage,
  ProblemDetails,
  RuleBasedPredictionResult,
  TimelineItem,
} from './predictionContracts';
import { classifyProblem, PredictionApiError, toPredictionApiError } from './predictionErrors';

export const PREDICTION_ENDPOINT = '/api/predictions/calculate';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}

function isShortage(value: unknown): value is MaterialShortage {
  return isRecord(value)
    && typeof value.productReference === 'string'
    && typeof value.shortageQuantity === 'number'
    && Number.isFinite(value.shortageQuantity);
}

function isTimelineItem(value: unknown): value is TimelineItem {
  return isRecord(value)
    && typeof value.operationRef === 'string'
    && typeof value.estimatedStart === 'string'
    && typeof value.estimatedEnd === 'string'
    && typeof value.isCritical === 'boolean';
}

export function isRuleBasedPredictionResult(value: unknown): value is RuleBasedPredictionResult {
  return isRecord(value)
    && typeof value.orderReference === 'string'
    && typeof value.estimatedStart === 'string'
    && typeof value.estimatedEnd === 'string'
    && typeof value.estimatedDelivery === 'string'
    && isStringArray(value.criticalPathOperations)
    && isStringArray(value.appliedFallbackReasons)
    && Array.isArray(value.shortages)
    && value.shortages.every(isShortage)
    && Array.isArray(value.timeline)
    && value.timeline.every(isTimelineItem);
}

function parseProblemDetails(value: unknown): ProblemDetails {
  if (!isRecord(value)) return {};
  return {
    status: typeof value.status === 'number' ? value.status : undefined,
    title: typeof value.title === 'string' ? value.title : undefined,
    detail: typeof value.detail === 'string' ? value.detail : undefined,
    errorCode: typeof value.errorCode === 'string' ? value.errorCode : undefined,
  };
}

async function readBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) return null;
  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

export async function calculatePrediction(orderReference: string): Promise<RuleBasedPredictionResult> {
  const request: CalculatePredictionRequest = { orderReference: orderReference.trim() };

  try {
    const response = await fetch(PREDICTION_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    const body = await readBody(response);

    if (!response.ok) {
      const problem = typeof body === 'string' ? { detail: body } : parseProblemDetails(body);
      throw classifyProblem(problem, response.status);
    }
    if (!isRuleBasedPredictionResult(body)) {
      throw new PredictionApiError('The prediction service returned an unexpected response.', 'calculationFailure');
    }
    return body;
  } catch (error: unknown) {
    throw toPredictionApiError(error);
  }
}
