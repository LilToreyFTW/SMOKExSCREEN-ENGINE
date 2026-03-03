"use client";

import { useState } from "react";
import { authClient } from "@/lib/auth-client";
import Link from "next/link";

const DISCORD_CLIENT_ID = "1476913890620342444";
const BASE_URL = process.env.NEXT_PUBLIC_BASE_URL || "http://localhost:3000";
const REDIRECT_URI = encodeURIComponent(`${BASE_URL}/api/auth/discord/callback`);
const DISCORD_AUTH_URL = `https://discord.com/api/oauth2/authorize?client_id=${DISCORD_CLIENT_ID}&redirect_uri=${REDIRECT_URI}&response_type=code&scope=identify%20email`;

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSignUp, setIsSignUp] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      if (isSignUp) {
        const { data, error } = await authClient.signUp.email({
          email,
          password,
          name: email.split("@")[0],
        });
        if (error) {
          setError(error.message);
        } else {
          setIsSignUp(false);
          setError("Account created! Please sign in.");
        }
      } else {
        const { data, error } = await authClient.signIn.email({
          email,
          password,
        });
        if (error) {
          setError(error.message);
        } else {
          window.location.href = "/";
        }
      }
    } catch (err: any) {
      setError(err.message || "An error occurred");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-[#0b0f14] text-white flex items-center justify-center p-4">
      <div className="max-w-md w-full">
        <div className="text-center mb-8">
          <Link href="/" className="text-3xl font-bold text-[#FF3D00]">
            SmokeScreen
          </Link>
          <p className="text-gray-400 mt-2">ENGINE</p>
        </div>

        <div className="bg-[#11161c] p-8 rounded-2xl border border-gray-800">
          <h1 className="text-2xl font-bold mb-6 text-center">
            {isSignUp ? "Create Account" : "Welcome Back"}
          </h1>

          <a
            href={DISCORD_AUTH_URL}
            className="block w-full bg-[#5865F2] hover:bg-[#4752C4] text-white text-center py-4 rounded-lg font-bold text-lg transition-colors mb-4"
          >
            Continue with Discord
          </a>

          <div className="relative my-6">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-700"></div>
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-2 bg-[#11161c] text-gray-500">or</span>
            </div>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-400 mb-2">
                Email
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full bg-[#0b0f14] border border-gray-700 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#FF3D00]"
                placeholder="you@example.com"
                required
              />
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-400 mb-2">
                Password
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full bg-[#0b0f14] border border-gray-700 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#FF3D00]"
                placeholder="••••••••"
                required
                minLength={6}
              />
            </div>

            {error && (
              <p className="text-red-500 text-sm mb-4">{error}</p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-[#FF3D00] hover:bg-[#e03600] text-white font-bold py-3 rounded-lg transition-colors disabled:opacity-50"
            >
              {loading ? "Loading..." : isSignUp ? "Create Account" : "Sign In"}
            </button>
          </form>
        </div>

        <div className="text-center mt-6">
          <p className="text-gray-400">
            {isSignUp ? "Already have an account? " : "Don't have an account? "}
            <button
              onClick={() => setIsSignUp(!isSignUp)}
              className="text-[#FF3D00] hover:underline"
            >
              {isSignUp ? "Sign in" : "Sign up"}
            </button>
          </p>
        </div>
      </div>
    </main>
  );
}
