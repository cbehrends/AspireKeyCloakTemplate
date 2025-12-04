import React, { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { keycloak } from './keycloak'

type AuthContextType = {
  initialized: boolean
  isAuthenticated: boolean
  token?: string | undefined
  login: () => Promise<void>
  logout: () => Promise<void>
  user?: { preferred_username?: string; name?: string; email?: string } | undefined
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [initialized, setInitialized] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [token, setToken] = useState<string | undefined>(undefined)
  const [user, setUser] = useState<AuthContextType['user']>(undefined)

  useEffect(() => {
    let canceled = false

    async function init() {
      try {
        const authenticated = await keycloak.init({
          checkLoginIframe: false,
          onLoad: 'check-sso',
          silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
          pkceMethod: 'S256',
        })
        if (canceled) return
        setIsAuthenticated(Boolean(authenticated))
        setToken(keycloak.token ?? undefined)

        if (authenticated) {
          try {
            const profile = await keycloak.loadUserInfo()
            if (!canceled) setUser(profile as any)
          } catch {
            // ignore
          }
        }

        // Set up token refresh
        const refresh = async () => {
          try {
            const refreshed = await keycloak.updateToken(30)
            if (refreshed && !canceled) {
              setToken(keycloak.token ?? undefined)
            }
          } catch {
            // token likely expired
            if (!canceled) {
              setIsAuthenticated(false)
              setToken(undefined)
              setUser(undefined)
            }
          }
        }

        const interval = window.setInterval(refresh, 20_000)
        return () => window.clearInterval(interval)
      } finally {
        if (!canceled) setInitialized(true)
      }
    }

    const cleanup = init()
    return () => {
      canceled = true
      // eslint-disable-next-line @typescript-eslint/no-floating-promises
      Promise.resolve(cleanup)
    }
  }, [])

  const value = useMemo<AuthContextType>(
    () => ({
      initialized,
      isAuthenticated,
      token,
      user,
      login: async () => {
        await keycloak.login({ redirectUri: window.location.href })
      },
      logout: async () => {
        await keycloak.logout({ redirectUri: window.location.origin })
      },
    }),
    [initialized, isAuthenticated, token, user]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
