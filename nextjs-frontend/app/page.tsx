import Image from "next/image";
import { UserMenu } from "@/components/user-menu";
import styles from "./page.module.css";

export default function Home() {
  return (
    <div className={styles.page}>
      {/* Header with authentication */}
      <header style={{ 
        width: '100%', 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center',
        padding: '1rem 2rem',
        borderBottom: '1px solid #e0e0e0'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <Image
            src="/next.svg"
            alt="Next.js logo"
            width={120}
            height={24}
            priority
          />
          <span style={{ fontSize: '1.25rem', fontWeight: 600 }}>Jarvis AI Assistant</span>
        </div>
        <UserMenu />
      </header>

      <main className={styles.main}>
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <h1 style={{ fontSize: '2.5rem', fontWeight: 'bold', marginBottom: '1rem' }}>
            Welcome to Jarvis
          </h1>
          <p style={{ fontSize: '1.125rem', color: '#666', marginBottom: '2rem' }}>
            Your personal AI assistant powered by Azure
          </p>
        </div>

        <div className={styles.ctas}>
          <a
            className={styles.primary}
            href="/dashboard"
          >
            <Image
              className={styles.logo}
              src="/vercel.svg"
              alt="Vercel logomark"
              width={20}
              height={20}
            />
            Start Dashboard
          </a>
          <a
            href="/docs"
            className={styles.secondary}
          >
            View Documentation
          </a>
        </div>

        <div style={{ 
          display: 'grid', 
          gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
          gap: '1.5rem', 
          marginTop: '2rem',
          width: '100%'
        }}>
          <div style={{ padding: '1.5rem', border: '1px solid #e0e0e0', borderRadius: '8px' }}>
            <h3 style={{ fontSize: '1.125rem', fontWeight: 600, marginBottom: '0.5rem' }}>
              🤖 AI Powered
            </h3>
            <p style={{ color: '#666' }}>Advanced AI capabilities powered by Azure OpenAI</p>
          </div>
          <div style={{ padding: '1.5rem', border: '1px solid #e0e0e0', borderRadius: '8px' }}>
            <h3 style={{ fontSize: '1.125rem', fontWeight: 600, marginBottom: '0.5rem' }}>
              🔒 Secure
            </h3>
            <p style={{ color: '#666' }}>Enterprise-grade security with Azure AD authentication</p>
          </div>
          <div style={{ padding: '1.5rem', border: '1px solid #e0e0e0', borderRadius: '8px' }}>
            <h3 style={{ fontSize: '1.125rem', fontWeight: 600, marginBottom: '0.5rem' }}>
              ☁️ Cloud Native
            </h3>
            <p style={{ color: '#666' }}>Built for the cloud with Azure Container Apps</p>
          </div>
        </div>
      </main>

      <footer className={styles.footer}>
        <a
          href="https://nextjs.org/learn?utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          target="_blank"
          rel="noopener noreferrer"
        >
          <Image
            aria-hidden
            src="/file.svg"
            alt="File icon"
            width={16}
            height={16}
          />
          Learn
        </a>
        <a
          href="https://vercel.com/templates?framework=next.js&utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          target="_blank"
          rel="noopener noreferrer"
        >
          <Image
            aria-hidden
            src="/window.svg"
            alt="Window icon"
            width={16}
            height={16}
          />
          Examples
        </a>
        <a
          href="https://nextjs.org?utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          target="_blank"
          rel="noopener noreferrer"
        >
          <Image
            aria-hidden
            src="/globe.svg"
            alt="Globe icon"
            width={16}
            height={16}
          />
          Go to nextjs.org →
        </a>
      </footer>
    </div>
  );
}
