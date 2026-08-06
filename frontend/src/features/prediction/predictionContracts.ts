export interface CalculatePredictionRequest {
  orderReference: string;
}

export interface MaterialShortage {
  productReference: string;
  shortageQuantity: number;
}

export interface TimelineItem {
  operationRef: string;
  estimatedStart: string;
  estimatedEnd: string;
  isCritical: boolean;
}

export interface RuleBasedPredictionResult {
  orderReference: string;
  estimatedStart: string;
  estimatedEnd: string;
  estimatedDelivery: string;
  criticalPathOperations: string[];
  appliedFallbackReasons: string[];
  shortages: MaterialShortage[];
  timeline: TimelineItem[];
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errorCode?: string;
}
