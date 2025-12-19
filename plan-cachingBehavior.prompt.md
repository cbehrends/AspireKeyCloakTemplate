# Caching Behavior Implementation Plan

This plan outlines the steps to implement a caching behavior for the Mediator pattern using `IDistributedCache`.

## 1. `ICacheableRequest` Interface

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Caching/ICacheableRequest.cs`
- **Purpose:** Defines the contract for requests that can be cached.
- **Contents:**
  ```csharp
  public interface ICacheableRequest<out TResponse> : IRequest<TResponse>
  {
      string CacheKey { get; }
      string? CacheGroupKey { get; }
      TimeSpan? AbsoluteExpirationRelativeToNow { get; }
  }
  ```

## 2. `CachingBehavior`

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Behaviors/CachingBehavior.cs`
- **Purpose:** Implements the pipeline behavior to cache request responses.
- **Logic:**
  - Checks if a response is present in the `IDistributedCache` using the `CacheKey`.
  - If a cached response exists, it's returned.
  - If not, the request is processed, and the response is cached with the specified expiration.
  - If a `CacheGroupKey` is provided, the `CacheKey` is added to a set associated with the group key.

## 3. Cache Invalidation Notifications

### Item-Level Invalidation

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Caching/CacheInvalidationNotification.cs`
- **Purpose:** A notification to invalidate a single cached item.
- **Contents:**
  ```csharp
  public record CacheInvalidationNotification(string CacheKey) : INotification;
  ```

### Group-Level Invalidation

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Caching/CacheGroupInvalidationNotification.cs`
- **Purpose:** A notification to invalidate a group of cached items.
- **Contents:**
  ```csharp
  public record CacheGroupInvalidationNotification(string CacheGroupKey) : INotification;
  ```

## 4. Notification Handlers

### `CacheInvalidationNotificationHandler`

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Caching/CacheInvalidationNotificationHandler.cs`
- **Purpose:** Handles `CacheInvalidationNotification` to remove a specific item from the cache.

### `CacheGroupInvalidationNotificationHandler`

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/Caching/CacheGroupInvalidationNotificationHandler.cs`
- **Purpose:** Handles `CacheGroupInvalidationNotification` to remove all items belonging to a cache group.

## 5. Dependency Injection Registration

- **File:** `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/MediatorServiceCollectionExtensions.cs`
- **Changes:**
  - A new `AddCachingBehavior` method is added to `MediatorConfiguration`.
  - `CachingBehavior<,>` is registered as a transient pipeline behavior.
  - `CacheInvalidationNotificationHandler` and `CacheGroupInvalidationNotificationHandler` are registered.
  - `AddCachingBehavior` is called within the `AddMediator` extension method to enable the caching behavior by default.

