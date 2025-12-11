# Features - Vertical Slice Architecture

This directory contains the application's features organized using a **Vertical Slice Architecture** pattern. Each feature encapsulates all the code needed for a specific domain concern, from UI components to API calls to state management.

## What is Vertical Slice Architecture?

Vertical Slice Architecture organizes code by **feature** rather than by technical layer (components, hooks, services, etc.). Each feature is a complete vertical slice through the application stack, containing everything needed for that feature to function independently.

### Benefits

- **Cohesion**: Related code lives together, making features easier to understand and modify
- **Reduced coupling**: Features are self-contained with clear boundaries
- **Easier maintenance**: Changes to a feature are localized to its directory
- **Team scalability**: Multiple developers can work on different features with minimal conflicts
- **Alignment with backend**: Mirrors the backend's feature structure (Users, Core, etc.)

## Directory Structure

```
features/
├── authentication/          # Authentication and authorization
│   ├── api/                # API calls (user, csrf, fetch-with-xsrf)
│   ├── hooks/              # React hooks (useBffUser)
│   ├── store/              # State management (auth-store)
│   ├── types/              # TypeScript types and interfaces
│   └── index.ts            # Barrel export
├── navigation/             # App navigation and header
│   ├── components/         # UI components (NavigationHeader)
│   ├── hooks/              # Feature-specific hooks
│   ├── types/              # TypeScript types
│   └── index.ts            # Barrel export
├── shared/                 # Cross-cutting concerns
│   ├── demo/               # Demo store example
│   └── index.ts            # Barrel export
└── index.ts                # Main barrel export
```

## Feature Structure Convention

Each feature should follow this structure:

```
feature-name/
├── api/                    # API calls and data fetching
│   ├── *.ts               # Individual API modules
│   └── index.ts           # (optional) Barrel export
├── components/             # React components
│   ├── *.tsx              # Individual components
│   └── index.ts           # (optional) Barrel export
├── hooks/                  # Custom React hooks
│   ├── *.ts               # Individual hooks
│   └── index.ts           # (optional) Barrel export
├── store/                  # State management (TanStack Store, Zustand, etc.)
│   ├── *.ts               # Store definitions
│   └── index.ts           # (optional) Barrel export
├── types/                  # TypeScript types and interfaces
│   └── index.ts           # Type definitions
├── utils/                  # Feature-specific utilities
│   └── *.ts               # Utility functions
└── index.ts                # Main barrel export for the feature
```

### Not all folders are required

Only create directories that your feature needs. A simple feature might only have:
```
feature-name/
├── components/
│   └── MyComponent.tsx
└── index.ts
```

## Barrel Exports (index.ts)

Each feature should have an `index.ts` that exports the public API of that feature:

```typescript
// features/my-feature/index.ts
export { MyComponent } from './components/MyComponent'
export { useMyFeature } from './hooks/use-my-feature'
export { myFeatureApi } from './api/my-feature-api'
export type { MyFeatureData } from './types'
```

This allows clean imports from other parts of the app:
```typescript
import { MyComponent, useMyFeature } from '@/features/my-feature'
```

## When to Create a New Feature

Create a new feature when:

1. **Domain boundary**: The functionality represents a distinct domain concept (e.g., `users`, `products`, `orders`)
2. **Backend alignment**: It maps to a backend feature/module (e.g., backend has `Features/Users`, frontend has `features/users`)
3. **Size**: The feature is substantial enough to warrant its own directory (more than just a single component)
4. **Independence**: The functionality could reasonably be developed and tested independently

## When to Extend an Existing Feature

Add to an existing feature when:

1. The functionality is directly related to the existing feature's domain
2. It shares types, state, or API calls with the existing feature
3. It would be awkward to separate (high coupling)

## Frontend vs Backend Feature Alignment

### Current Approach: Frontend-Centric Features

The initial structure uses **frontend-centric** feature names:
- `authentication` - handles login, tokens, session (maps to backend BFF endpoints)
- `navigation` - handles app navigation and header UI

### Alternative: Backend-Aligned Features

As the app grows, you may want to align more closely with backend features:
- `users` - maps to `Gateway/Features/Users` and `API/Features/Users`
- `products` - maps to backend product features
- `orders` - maps to backend order features

Choose the approach that makes most sense for your team and domain.

## Shared Code

### `features/shared/`

Use the `shared` feature for:
- Cross-cutting concerns that multiple features use
- Utility functions that don't belong to a specific domain
- Demo or example code

### `lib/` (outside features)

The `lib/` directory (parallel to `features/`) should contain:
- Framework integrations (TanStack Query setup, router config)
- Design system primitives (if not using a component library)
- Global utilities that are truly application-wide

**Guideline**: If it's domain-specific, it belongs in a feature. If it's technical infrastructure, it belongs in `lib/`.

## Route Organization

Routes remain in the top-level `routes/` directory due to **TanStack Router's file-based routing**. Route files should import from features:

```typescript
// routes/users.tsx
import { createFileRoute } from '@tanstack/react-router'
import { UserList } from '@/features/users'

export const Route = createFileRoute('/users')({
  component: () => <UserList />
})
```

## Migration Strategy

The old `lib/` files have been updated to re-export from the new feature locations for backwards compatibility. This allows for gradual migration:

1. ✅ New code should import from `features/`
2. ⚠️ Existing code can continue using `lib/` imports (they're aliased)
3. 🔄 Gradually update old imports to use `features/` directly
4. 🗑️ Remove the backwards compatibility exports from `lib/` once all imports are updated

## Example: Adding a New Feature

Let's say you want to add a "products" feature:

1. **Create the directory structure**:
   ```bash
   mkdir -p src/features/products/{api,components,hooks,types}
   ```

2. **Add types**:
   ```typescript
   // features/products/types/index.ts
   export interface Product {
     id: string
     name: string
     price: number
   }
   ```

3. **Add API calls**:
   ```typescript
   // features/products/api/products.ts
   import { fetchWithXsrf } from '@/features/authentication'
   
   export async function fetchProducts() {
     const response = await fetchWithXsrf('/api/products')
     return response.json()
   }
   ```

4. **Add hooks**:
   ```typescript
   // features/products/hooks/use-products.ts
   import { useQuery } from '@tanstack/react-query'
   import { fetchProducts } from '../api/products'
   
   export function useProducts() {
     return useQuery({
       queryKey: ['products'],
       queryFn: fetchProducts
     })
   }
   ```

5. **Add components**:
   ```typescript
   // features/products/components/ProductList.tsx
   import { useProducts } from '../hooks/use-products'
   
   export function ProductList() {
     const { data: products } = useProducts()
     // ... render logic
   }
   ```

6. **Create barrel export**:
   ```typescript
   // features/products/index.ts
   export { ProductList } from './components/ProductList'
   export { useProducts } from './hooks/use-products'
   export { fetchProducts } from './api/products'
   export type { Product } from './types'
   ```

7. **Update main features index**:
   ```typescript
   // features/index.ts
   export * from './authentication'
   export * from './navigation'
   export * from './products'  // Add this line
   export * from './shared'
   ```

## Best Practices

1. **Keep features focused**: Each feature should have a single, clear purpose
2. **Minimize cross-feature dependencies**: Features should be as independent as possible
3. **Use barrel exports**: Always export through `index.ts` for clean imports
4. **Colocate tests**: Place test files next to the code they test (e.g., `MyComponent.test.tsx` next to `MyComponent.tsx`)
5. **Document complex features**: Add a README in the feature directory if it has complex logic
6. **Consistent naming**: Use descriptive, domain-appropriate names for features

## Migration Checklist

- [x] Create `features/` directory structure
- [x] Move authentication logic to `features/authentication/`
- [x] Move navigation logic to `features/navigation/`
- [x] Move demo store to `features/shared/demo/`
- [x] Create barrel exports for all features
- [x] Update `routes/__root.tsx` to use new imports
- [x] Add backwards compatibility exports in `lib/`
- [x] Document the architecture in this README

### Next Steps

- [ ] Update remaining imports to use `features/` directly
- [ ] Remove old `components/Header.tsx` (replaced by `NavigationHeader`)
- [ ] Add new features as needed (e.g., `users`, `products`)
- [ ] Remove backwards compatibility exports from `lib/` once migration is complete
- [ ] Add feature-specific README files for complex features

## Questions?

If you're unsure whether something should be a feature, start by asking:
1. Does this represent a distinct domain concept?
2. Would it make sense to a non-technical stakeholder?
3. Does it align with how we talk about the system?

If yes to these questions, it's probably a good candidate for a feature.

