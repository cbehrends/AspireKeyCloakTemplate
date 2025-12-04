import Keycloak from 'keycloak-js'

const kcUrl = import.meta.env.VITE_KEYCLOAK_URL as string | undefined
const kcRealm = import.meta.env.VITE_KEYCLOAK_REALM as string | undefined
const kcClientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID as string | undefined

if (!kcUrl || !kcRealm || !kcClientId) {
  // eslint-disable-next-line no-console
  console.warn(
    '[Keycloak] Missing configuration. Ensure VITE_KEYCLOAK_URL, VITE_KEYCLOAK_REALM and VITE_KEYCLOAK_CLIENT_ID are set.'
  )
}

export const keycloak: Keycloak = new Keycloak({
  url: kcUrl ?? 'https://localhost:8080',
  realm: kcRealm ?? 'Test',
  clientId: kcClientId ?? 'test',
})
