import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES, currentLanguage, type SupportedLanguage } from '@/i18n'

const LONG_NAME_KEYS: Record<SupportedLanguage, string> = {
  tr: 'language.trLong',
  en: 'language.enLong',
}

/** TR / EN toggle. The choice is cached in localStorage by the language detector. */
export default function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  const active = currentLanguage()

  return (
    <div
      className="btn-group btn-group-sm"
      role="group"
      aria-label={t('language.label')}
    >
      {SUPPORTED_LANGUAGES.map((language) => {
        const isActive = language === active
        return (
          <button
            key={language}
            type="button"
            className={`btn ${isActive ? 'btn-light-primary' : 'btn-light'} fw-semibold`}
            aria-pressed={isActive}
            aria-label={t('language.switchTo', { language: t(LONG_NAME_KEYS[language]) })}
            onClick={() => void i18n.changeLanguage(language)}
          >
            {t(`language.${language}`)}
          </button>
        )
      })}
    </div>
  )
}
