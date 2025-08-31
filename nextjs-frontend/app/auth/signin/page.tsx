"use client"

import { signIn, getProviders } from "next-auth/react"
import { useEffect, useState, Suspense } from "react"
import { useSearchParams } from "next/navigation"

function SignInForm() {
  const [providers, setProviders] = useState<any>(null)
  const searchParams = useSearchParams()
  const callbackUrl = searchParams.get('callbackUrl') || '/'

  useEffect(() => {
    ;(async () => {
      const res = await getProviders()
      setProviders(res)
    })()
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to your account
          </h2>
        </div>
        <div className="mt-8 space-y-6">
          {providers &&
            Object.values(providers).map((provider: any) => (
              <div key={provider.name}>
                <button
                  onClick={() => signIn(provider.id, { callbackUrl })}
                  className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition duration-150 ease-in-out"
                >
                  <svg className="w-5 h-5 mr-2" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M23.5 12c0-.8-.1-1.6-.2-2.4H12v4.5h6.4c-.3 1.6-1.2 3-2.5 4v3.4h4c2.4-2.2 3.8-5.5 3.8-9.4z" fill="#4285F4"/>
                    <path d="M12 24c3.2 0 5.9-1.1 7.9-2.9l-4-3.1c-1.1.7-2.5 1.1-3.9 1.1-3 0-5.5-2-6.4-4.8H1.5v3.3C3.6 21.4 7.5 24 12 24z" fill="#34A853"/>
                    <path d="M5.6 14.3c-.2-.7-.4-1.4-.4-2.1s.1-1.4.4-2.1V6.8H1.5C.5 8.7 0 10.3 0 12s.5 3.3 1.5 5.2l4.1-3z" fill="#FBBC05"/>
                    <path d="M12 4.8c1.7 0 3.2.6 4.4 1.8l3.3-3.3C17.9 1.4 15.2 0 12 0 7.5 0 3.6 2.6 1.5 6.8l4.1 3.1C6.5 6.8 9 4.8 12 4.8z" fill="#EA4335"/>
                  </svg>
                  Sign in with {provider.name}
                </button>
              </div>
            ))}
        </div>
      </div>
    </div>
  )
}

export default function SignIn() {
  return (
    <Suspense fallback={
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
          <p className="mt-4 text-gray-600">Loading sign in...</p>
        </div>
      </div>
    }>
      <SignInForm />
    </Suspense>
  )
}
