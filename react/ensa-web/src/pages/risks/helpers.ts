import {
  HazardClass,
  RiskAssessmentMethod,
  type RiskLevel,
} from '@/api/endpoints'

/**
 * Small conversions shared by the screens of this module.
 *
 * They are deliberately local: `@/utils/format` renders values for the reader, while these turn
 * API values into the shapes `<input type="date">` and `<select>` need, which is a form concern.
 */

/** ISO date-time from the API -> the `yyyy-MM-dd` an `<input type="date">` accepts. */
export function toDateInput(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  // Local parts, so a date near midnight does not shift a day under a UTC conversion.
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** Today in the `yyyy-MM-dd` form, used as the default of a new record. */
export function todayInput(): string {
  return toDateInput(new Date().toISOString())
}

/** Empty date input -> `null`, so an optional date is omitted rather than sent as `""`. */
export function fromDateInput(value: string): string | null {
  return value ? value : null
}

/** Enum members of a numeric enum object, without the reverse-mapping keys. */
export function enumValues(enumObject: Record<string, string | number>): number[] {
  return Object.values(enumObject).filter((value): value is number => typeof value === 'number')
}

/** Hazard classes that may be chosen — `Unspecified` is rejected by the domain manager. */
export const SELECTABLE_HAZARD_CLASSES: HazardClass[] = [
  HazardClass.LowHazard,
  HazardClass.Hazardous,
  HazardClass.VeryHazardous,
]

/** Assessment methods offered on the create form; `Unspecified` is not one of them. */
export const SELECTABLE_METHODS: RiskAssessmentMethod[] = [
  RiskAssessmentMethod.LMatrixThreeByThree,
  RiskAssessmentMethod.LMatrixFiveByFive,
  RiskAssessmentMethod.FineKinney,
  RiskAssessmentMethod.Fmea,
  RiskAssessmentMethod.Checklist,
]

/**
 * The rating values each method works with.
 *
 * Fine-Kinney uses the classic likelihood / frequency / severity scales; the L-matrices use a
 * plain 1..n scale on likelihood and severity. Anything else is scored freely, so the form falls
 * back to a number input. The score itself is always computed by the backend — the values here
 * only decide what the user is offered.
 */
export interface RatingScale {
  likelihood: number[]
  frequency: number[]
  severity: number[]
  /** Whether the method multiplies frequency into the score. */
  usesFrequency: boolean
}

const FINE_KINNEY: RatingScale = {
  likelihood: [10, 6, 3, 1, 0.5, 0.2],
  frequency: [10, 6, 3, 2, 1, 0.5],
  severity: [100, 40, 15, 7, 3, 1],
  usesFrequency: true,
}

function linearScale(max: number): RatingScale {
  const values = Array.from({ length: max }, (_, index) => index + 1)
  return { likelihood: values, frequency: [], severity: values, usesFrequency: false }
}

/** Rating scale of a method, or `null` when the method is scored with free numbers. */
export function ratingScale(method: RiskAssessmentMethod): RatingScale | null {
  switch (method) {
    case RiskAssessmentMethod.FineKinney:
      return FINE_KINNEY
    case RiskAssessmentMethod.LMatrixFiveByFive:
      return linearScale(5)
    case RiskAssessmentMethod.LMatrixThreeByThree:
      return linearScale(3)
    default:
      return null
  }
}

/**
 * Client-side preview of the risk score.
 *
 * The authoritative value comes back from `IRiskAssessmentManager`; this only lets the user see
 * where a rating is heading before saving, and is always labelled as a preview.
 */
export function previewScore(
  method: RiskAssessmentMethod,
  likelihood: number,
  frequency: number,
  severity: number,
): number | null {
  if (!likelihood || !severity) return null
  const scale = ratingScale(method)
  if (!scale) return null
  const value = scale.usesFrequency ? likelihood * frequency * severity : likelihood * severity
  return Number.isFinite(value) ? Math.round(value * 100) / 100 : null
}

/** Sorts hazard lines so the most severe risk is read first. */
export function byRiskDescending(left: { riskScore: number }, right: { riskScore: number }) {
  return right.riskScore - left.riskScore
}

/** Risk levels in ascending severity, used for the legend and the filter. */
export const RISK_LEVELS: RiskLevel[] = [1, 2, 3, 4, 5]
