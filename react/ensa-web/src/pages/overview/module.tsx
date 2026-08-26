import type { ModuleDefinition } from '@/modules/registry'

/**
 * The dashboard route itself is the index route declared in `App.tsx`; this module only
 * contributes its sidebar entry, so the menu stays entirely registry-driven.
 */
const definition: ModuleDefinition = {
  routes: [],
  nav: [{ path: '', labelKey: 'nav.dashboard', icon: '▤', group: 'overview', order: 10 }],
}

export const { routes, nav } = definition
export default definition
