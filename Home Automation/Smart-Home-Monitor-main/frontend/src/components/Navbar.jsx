import React from 'react';
import { NavLink } from 'react-router-dom';
import { signOut } from 'firebase/auth';
import { auth } from '../firebase';
import toast from 'react-hot-toast';

const NAV_LINKS = [
  { to: '/',             label: 'Dashboard',    end: true  },
  { to: '/leds',         label: 'LEDs',         end: false },
  { to: '/messages',     label: 'Messages',     end: false },
  { to: '/flood-events', label: 'Flood Events', end: false },
  { to: '/simulator',    label: 'Simulator',    end: false },
];

export default function Navbar({ user }) {
  async function handleLogout() {
    await signOut(auth);
    toast.success('Logged out.');
  }

  return (
    <nav className="bg-gray-900 border-b border-gray-800 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 flex items-center h-14 gap-4">
        {/* Brand */}
        <span className="text-white font-bold text-base shrink-0 tracking-tight">Home Monitor</span>

        {/* Nav links */}
        <div className="flex gap-1 flex-1 overflow-x-auto scrollbar-none">
          {NAV_LINKS.map(({ to, label, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors ${
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800'
                }`
              }
            >
              {label}
            </NavLink>
          ))}
        </div>

        {/* User + logout */}
        <div className="flex items-center gap-3 shrink-0 ml-auto">
          <span className="text-gray-400 text-sm hidden sm:block truncate max-w-[180px]">
            {user?.email}
          </span>
          <button
            onClick={handleLogout}
            className="text-gray-400 hover:text-white text-sm px-3 py-1.5 rounded-lg
                       hover:bg-gray-800 transition-colors"
          >
            Logout
          </button>
        </div>
      </div>
    </nav>
  );
}
