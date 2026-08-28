import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Avatar, Button, Flex, Menu, Navbar, Text } from 'rich-react-component'
import { useAuth } from '@/auth/AuthContext'
import LanguageSwitcher from '@/components/LanguageSwitcher'

/**
 * Top bar, drawn by the library's `Navbar`.
 *
 * The user menu is the library's `Menu` rather than Bootstrap's `data-bs-toggle="dropdown"`: this
 * application loads Bootstrap's stylesheet and none of its JavaScript, so that attribute never had
 * anything listening to it — the menu could not open at all. `Menu` brings its own open state and
 * keyboard handling, and takes the sign-out action as a plain callback.
 */
export default function Header({ onMenuToggle }: { onMenuToggle: () => void }) {
  const { t } = useTranslation()
  const { user, signOut } = useAuth()
  const navigate = useNavigate()

  function handleSignOut() {
    signOut()
    navigate('/login', { replace: true })
  }

  return (
    <Navbar
      className="sticky-top border-bottom"
      brand={
        <Button variant="light" size="sm" onClick={onMenuToggle} aria-label={t('nav.toggleMenu')}>
          ☰
        </Button>
      }
      end={
        <Flex gap={3} align="center">
          <LanguageSwitcher />

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
