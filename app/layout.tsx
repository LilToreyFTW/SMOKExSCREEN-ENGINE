import type { Metadata } from "next";
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
    <html lang="en">
      <body style={{margin:0, fontFamily:"Arial, sans-serif", background:"#0b0f14", color:"white"}}>
        <header style={{
          display: "flex",
          justifyContent: "space-between",
          padding: "20px 40px",
          borderBottom: "1px solid #222"
        }}>
          <div style={{fontWeight: "bold"}}>SmokeScreen</div>
          <div style={{display:"flex", gap:"20px"}}>
            <a href="/download" style={{color:"white", textDecoration:"none"}}>Download</a>
            <a href="/login" style={{color:"white", textDecoration:"none"}}>Login</a>
          </div>
        </header>
        {children}
      </body>
    </html>
  );
}
