## Plan: Implement BFF Login End-to-End (React + Gateway)

This plan will enable end-to-end BFF (Backend-for-Frontend) login for your React app, using the existing YARP Gateway as a proxy. The Gateway will handle all KeyCloak integration, exposing only generic BFF endpoints to the frontend. The React UI will interact solely with /bff/login, /bff/users, and /bff/logout, remaining agnostic of the underlying auth provider. The Gateway implementation will be modeled after the example in https://github.com/timdeschryver/Sandbox/tree/main.

### Steps
1. Review and document current Gateway YARP BFF transformer setup ([DotNetCleanTemplate.Gateway](src/DotNetCleanTemplate.Gateway/)), referencing the Sandbox repo for best practices.
2. Define and implement /bff/login, /bff/users, /bff/logout endpoints in Gateway ([Program.cs](src/DotNetCleanTemplate.Gateway/Program.cs), transformers), handling all KeyCloak logic server-side, following the Sandbox repo's approach.
3. Ensure Gateway manages session cookies, anti-CSRF, and user info securely, abstracting KeyCloak from the frontend.
4. Update React app to call only /bff/login, /bff/users, /bff/logout for authentication ([Header.tsx](src/react-app/src/components/Header.tsx), auth context/hooks).
5. Add UI for login/logout and user session state in React ([Header.tsx](src/react-app/src/components/Header.tsx)).
6. Test end-to-end: login, session persistence, logout, and error handling.

### Further Considerations
1. Ensure no KeyCloak details leak to the frontend; all logic is Gateway-side.
2. Should /bff/login redirect to KeyCloak or use an API-driven flow? (Option A: redirect, Option B: API + popup)
3. Confirm session cookie security (SameSite, HttpOnly, Secure) and CSRF protection.
4. Review the Sandbox repo for additional security and architectural recommendations.
