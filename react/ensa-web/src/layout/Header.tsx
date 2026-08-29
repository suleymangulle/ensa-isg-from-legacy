import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  AppearanceMenu,
  Avatar,
  Button,
  Flex,
  Menu,
  Navbar,
  Popover,
  Text,
  useAppearance,
} from 'rich-react-component'
import { useAuth } from '@/auth/AuthContext'
import LanguageSwitcher from '@/components/LanguageSwitcher'
import { offeredColorSchemes } from '@/styles/appearance'

/**
 * Top bar, drawn by the library's `Navbar`.
 *
 * The user menu is the library's `Menu` rather than Bootstrap's
 * `data-bs-toggle="dropdown"`: this application loads Bootstrap's stylesheet
 * and none of its JavaScript, so that attribute never had anything listening
 * to it — the menu could not open at all. `Menu` brings its own open state and
 * keyboard handling, and takes the sign-out action as a plain callback.
 *
 * Two menu controls rather than one, because the shell now has two navigation
 * states: the rail toggle belongs to the desktop aside, the drawer button to
 * screens too narrow to show it at all. Each is shown at exactly the width
 * where the thing it controls exists, so neither can act on a menu the user
 * cannot see.
 */
export interface HeaderProps {
  isSidebarCollapsed: boolean
  onSidebarCollapseToggle: () => void
  onMobileMenuOpen: () => void
}

export default function Header({
  isSidebarCollapsed,
  onSidebarCollapseToggle,
  onMobileMenuOpen,
}: HeaderProps) {
  const { t } = useTranslation()
  const { user, signOut } = useAuth()
  const { resolvedTheme } = useAppearance()
  const navigate = useNavigate()

  function handleSignOut() {
    signOut()
    navigate('/login', { replace: true })
  }

  return (
    <Navbar
      // The top bar carries a Bootstrap background utility, which is a
      // `!important` rule and therefore wins over any token this application
      // sets. So the theme is passed through the library's own prop instead of
      // being fought with CSS.
      variant={resolvedTheme}
      className="sticky-top border-bottom"
      brand={
        <Flex gap={2} align="center">
          <Button
            variant="light"
            size="sm"
            className="d-lg-none"
            onClick={onMobileMenuOpen}
            aria-label={t('nav.openMenu')}
          >
            ☰
          </Button>
          <Button
            variant="light"
            size="sm"
            className="d-none d-lg-inline-flex"
            onClick={onSidebarCollapseToggle}
            aria-label={isSidebarCollapsed ? t('nav.expandMenu') : t('nav.collapseMenu')}
            aria-expanded={!isSidebarCollapsed}
          >
            ☰
          </Button>
        </Flex>
      }
      end={
        <Flex gap={3} align="center">
          <LanguageSwitcher />

          {/* The library ships no copy in any language, so every visible
              string in the appearance menu is supplied here. */}
          <Popover
            placement="bottom"
            title={t('appearance.title')}
            content={
              <AppearanceMenu
                colorSchemes={offeredColorSchemes()}
                labels={{
                  presentation: {
                    title: t('appearance.presentation.title'),
                    compact: t('appearance.presentation.compact'),
                    grouped: t('appearance.presentation.grouped'),
                  },
                  mode: {
                    title: t('appearance.mode.title'),
                    light: t('appearance.mode.light'),
                    dark: t('appearance.mode.dark'),
                    system: t('appearance.mode.system'),
                  },
                  tone: {
                    title: t('appearance.tone.title'),
                    light: t('appearance.tone.light'),
                    dark: t('appearance.tone.dark'),
                    auto: t('appearance.tone.auto'),
                  },
                  colorScheme: {
                    title: t('appearance.colorScheme.title'),
                    options: {
                      ensa: t('appearance.colorScheme.ensa'),
                      indigo: t('appearance.colorScheme.indigo'),
                      teal: t('appearance.colorScheme.teal'),
                      green: t('appearance.colorScheme.green'),
                    },
                  },
                }}
              />
            }
          >
            <Button variant="light" size="sm" aria-label={t('appearance.title')}>
              ◐
            </Button>
          </Popover>

          <Menu
            placement="end"
            items={[
              { key: 'profile', label: t('common.profile'), onSelect: () => navigate('/') },
              { key: 'signOut', label: t('common.signOut'), danger: true, onSelect: handleSignOut },
            ]}
          >
            <Flex gap={2} align="center" aria-label={t('nav.userMenu')}>
              <Avatar name={user?.fullName ?? '?'} />
              <Flex direction="column" align="start" className="d-none d-sm-flex lh-sm">
                <Text weight="semibold">{user?.fullName}</Text>
                <Text size="sm" tone="muted">
                  {user?.email ?? user?.userName}
                </Text>
              </Flex>
            </Flex>
          </Menu>
        </Flex>
      }
    />
  )
}
