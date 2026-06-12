import React, { useState } from 'react';
import { signInWithEmailAndPassword } from 'firebase/auth';
import { auth } from '../firebase';
import toast from 'react-hot-toast';

export default function Login() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleEmailSignIn(e) {
        e.preventDefault(); // Prevent page reload on form submission

        if (!email || !password) {
            return toast.error('Please fill in all fields.');
        }

        setLoading(true);
        try {
            await signInWithEmailAndPassword(auth, email, password);
            toast.success('Welcome back!');
        } catch (err) {
            // Friendly error mapping for common Firebase auth issues
            let friendlyMessage = 'Sign-in failed.';
            if (err.code === 'auth/invalid-credential') {
                friendlyMessage = 'Invalid email or password.';
            } else if (err.code === 'auth/user-not-found') {
                friendlyMessage = 'No account found with this email.';
            } else if (err.code === 'auth/wrong-password') {
                friendlyMessage = 'Incorrect password.';
            } else if (err.code === 'auth/invalid-email') {
                friendlyMessage = 'Please enter a valid email address.';
            }

            toast.error(friendlyMessage);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen bg-gray-950 flex items-center justify-center px-4">
            <div className="w-full max-w-sm bg-gray-900 border border-gray-800 rounded-2xl shadow-2xl p-8">
                {/* Header */}
                <div className="text-center mb-8">
                    <h1 className="text-2xl font-bold text-white">Home Monitor</h1>
                    <p className="text-gray-400 text-sm mt-1">Sign in to continue</p>
                </div>

                {/* Credentials Form */}
                <form onSubmit={handleEmailSignIn} className="space-y-5">
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1.5">
                            Email Address
                        </label>
                        <input
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="you@example.com"
                            disabled={loading}
                            className="w-full bg-gray-950 border border-gray-800 rounded-xl px-4 py-3
                         text-white placeholder-gray-600 focus:outline-none focus:border-blue-500
                         transition-colors disabled:opacity-50"
                            required
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1.5">
                            Password
                        </label>
                        <input
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                            disabled={loading}
                            className="w-full bg-gray-950 border border-gray-800 rounded-xl px-4 py-3
                         text-white placeholder-gray-600 focus:outline-none focus:border-blue-500
                         transition-colors disabled:opacity-50"
                            required
                        />
                    </div>

                    {/* Submit Button */}
                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full flex items-center justify-center bg-blue-600 hover:bg-blue-500
                       disabled:opacity-50 text-white font-semibold py-3 rounded-xl
                       transition-colors shadow-md mt-2"
                    >
                        {loading ? 'Signing in…' : 'Sign In'}
                    </button>
                </form>
            </div>
        </div>
    );
}