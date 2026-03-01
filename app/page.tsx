import { redirect } from 'next/navigation';

export default function Home() {
  // Serve the static HTML from public folder via redirect to the actual file
  // This works because Vercel serves static files from /public automatically
  redirect('/index.html');
}
