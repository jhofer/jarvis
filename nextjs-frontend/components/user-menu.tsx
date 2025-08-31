"use client"

import { useSession, signIn, signOut } from "next-auth/react"
import { useState } from "react"

export function UserMenu() {
  const { data: session, status } = useSession()
  const [isOpen, setIsOpen] = useState(false)

  if (status === "loading") {
    return <div className="h-8 w-8 rounded-full bg-gray-200 animate-pulse"></div>
  }

  if (status === "unauthenticated") {
    return (
      <button
        onClick={() => signIn()}
        className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded transition duration-150 ease-in-out"
      >
        Sign In
      </button>
    )
  }

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center space-x-2 text-gray-700 hover:text-gray-900 transition duration-150 ease-in-out"
      >
        {session?.user?.image ? (
          <img
            src={session.user.image}
            alt="Profile"
            className="h-8 w-8 rounded-full"
          />
        ) : (
          <div className="h-8 w-8 rounded-full bg-gray-300 flex items-center justify-center">
            <span className="text-sm font-medium text-gray-700">
              {session?.user?.name?.charAt(0) || session?.user?.email?.charAt(0)}
            </span>
          </div>
        )}
        <span className="text-sm font-medium">
          {session?.user?.name || session?.user?.email}
        </span>
        <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd" />
        </svg>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg py-1 z-10">
          <div className="px-4 py-2 text-sm text-gray-700 border-b">
            <div className="font-medium">{session?.user?.name}</div>
            <div className="text-gray-500">{session?.user?.email}</div>
          </div>
          <button
            onClick={() => signOut()}
            className="block w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 transition duration-150 ease-in-out"
          >
            Sign Out
          </button>
        </div>
      )}
    </div>
  )
}
