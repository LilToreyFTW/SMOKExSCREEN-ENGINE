import { SignedIn, SignedOut, UserButton } from "@clerk/nextjs";

export default function HomePage() {
  return (
    <main>
      <SignedOut>
        <h2>Sign in to access SmokeScreen ENGINE</h2>
      </SignedOut>
      <SignedIn>
        <h2>Welcome to SmokeScreen ENGINE</h2>
        <UserButton />
      </SignedIn>
    </main>
  );
}
