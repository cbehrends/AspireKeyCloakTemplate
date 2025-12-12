// Re-export the centralized authentication hook so the Header component can import it from the header feature
// without duplicating implementation or causing top-level export conflicts.
export { useBffUser } from "@/features/authentication";
