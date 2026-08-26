import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import tr from './locales/tr.json'
import en from './locales/en.json'

/**
 * Module translations.
 *
 * Every `src/pages/<module>/locales/<lang>.json` is merged onto the core bundle at build time.
 * Modules keep their own files instead of appending to one shared bundle, because a single
 * 200-key JSON edited by everyone is the classic merge-conflict hotspot — and a lost key there
 * surfaces as raw `some.key` text in the UI rather than as a build error.
 */
const moduleBundles = import.meta.glob<Record<string, unknown>>(
  '../pages/*/locales/*.json',
  { eager: true, import: 'default' },
)

/** Merges module bundles onto a copy of the core bundle. Later keys never overwrite core ones. */
function withModuleBundles(
  language: SupportedLanguage,
  core: Record<string, unknown>,
): Record<string, unknown> {
  const merged: Record<string, unknown> = { ...core }

  for (const [path, bundle] of Object.entries(moduleBundles)) {
    if (!path.endsWith(`/${language}.json`)) continue

    for (const [section, values] of Object.entries(bundle)) {
      const existing = merged[section]
      merged[section] =
        isPlainObject(existing) && isPlainObject(values)
          ? { ...existing, ...values }
          : (merged[section] ?? values)
    }
  }

  return merged
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

export const SUPPORTED_LANGUAGES = ['tr', 'en'] as const

export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number]

/** localStorage key the detector caches the active language under. */
export const LANGUAGE_STORAGE_KEY = 'ensa.lang'

/**
 * Language code -> .NET culture name.
 *
 * The API declares its supported cultures as `tr-TR` / `en-US`
 * (see `EnsaHttpApiHostModule.SupportedCultures`). ASP.NET Core falls back from a
 * specific culture to its parent, but not the other way round, so the full culture
 * name is sent rather than the bare two-letter code.
 */
const API_CULTURES: Record<SupportedLanguage, string> = {
  tr: 'tr-TR',
  en: 'en-US',
}

/** Language code -> BCP 47 tag used by `Intl` for date and number formatting. */
const FORMAT_LOCALES: Record<SupportedLanguage, string> = {
  tr: 'tr-TR',
  en: 'en-GB',
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      tr: { translation: withModuleBundles('tr', tr) },
      en: { translation: withModuleBundles('en', en) },
    },
    supportedLngs: [...SUPPORTED_LANGUAGES],
    fallbackLng: 'tr',
    // `tr-TR` reported by the browser resolves to the `tr` bundle.
    nonExplicitSupportedLngs: true,
    load: 'languageOnly',
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
    },
  })

/** Active language, narrowed to one of the supported codes. */
export function currentLanguage(): SupportedLanguage {
  const code = (i18n.resolvedLanguage ?? i18n.language ?? 'tr').split('-')[0]
  return (SUPPORTED_LANGUAGES as readonly string[]).includes(code)
    ? (code as SupportedLanguage)
    : 'tr'
}

/** Culture name for the `Accept-Language` request header. */
export function apiCulture(): string {
  return API_CULTURES[currentLanguage()]
}

/** Locale tag for `Intl.DateTimeFormat` / `Intl.NumberFormat`. */
export function formatLocale(): string {
  return FORMAT_LOCALES[currentLanguage()]
}

export default i18n
