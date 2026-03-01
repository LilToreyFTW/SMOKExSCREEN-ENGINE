import type { Metadata } from "next";
import {
  ClerkProvider,
  SignInButton,
  SignUpButton,
  SignedIn,
  SignedOut,
  UserButton,
} from "@clerk/nextjs";
import "./globals.css";

export const metadata: Metadata = {
  title: "SmokeScreen SaaS",
  description: "Premium SaaS Infrastructure & Marketplace",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ClerkProvider publishableKey={process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY}>
      <html lang="en">
        <body style={{margin:0, fontFamily:"Arial, sans-serif", background:"#0b0f14", color:"white"}}>
          <header style={{
            display: "flex",
            justifyContent: "space-between",
            padding: "20px 40px",
            borderBottom: "1px solid #222"
          }}>
            <div style={{fontWeight: "bold"}}>SmokeScreen</div>
            <div>
              <SignedOut>
                <SignInButton />
                <SignUpButton />
              </SignedOut>
              <SignedIn>
                <UserButton />
              </SignedIn>
            </div>
          </header>
          {children}
        </body>
      </html>
    </ClerkProvider>
  );
}
