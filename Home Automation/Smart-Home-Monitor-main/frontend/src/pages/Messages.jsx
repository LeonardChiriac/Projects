import React, { useEffect, useRef, useState } from 'react';
import { ref, onValue, push, set, get } from 'firebase/database';
import { auth, db } from '../firebase';
import { apiFetch } from '../api';
import toast from 'react-hot-toast';

export default function Messages() {
  const [messages, setMessages] = useState([]);
  const [input,    setInput]    = useState('');
  const [sending,  setSending]  = useState(false);
  const [loading,  setLoading]  = useState(true);
  const bottomRef = useRef(null);

  const uid = auth.currentUser?.uid;

  // Live message list from Firebase
  useEffect(() => {
    if (!uid) return;
    const unsub = onValue(ref(db, `users/${uid}/messages`), (snap) => {
      const data = snap.val();
      if (data) {
        const arr = Object.entries(data)
          .map(([id, m]) => ({ id, ...m }))
          .sort((a, b) => a.timestamp - b.timestamp)
          .slice(-10);
        setMessages(arr);
      } else {
        setMessages([]);
      }
      setLoading(false);
    });
    return unsub;
  }, [uid]);

  // Auto-scroll to latest message
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  async function handleSend(e) {
    e.preventDefault();
    const cmd = input.trim();
    if (!cmd || !uid) return;
    setSending(true);
    const ts = Math.floor(Date.now() / 1000);

    // Parse A<pin> = Activate  /  S<pin> = Stop
    const match = cmd.match(/^([AaSs])(\d+)$/);
    if (match) {
      const action   = match[1].toUpperCase();
      const pin      = Number(match[2]);
      const newState = action === 'A';
      try {
        // First record the outgoing command as a 'web' message
        await push(ref(db, `users/${uid}/messages`), { text: cmd, timestamp: ts, source: 'web' });

        // Look up the LED with this pin number
        const ledsSnap = await get(ref(db, `users/${uid}/leds`));
        const ledsData = ledsSnap.val();
        const entry = ledsData
          ? Object.entries(ledsData).find(([, led]) => led.pin === pin)
          : null;

        if (entry) {
          const [key] = entry;
          await set(ref(db, `users/${uid}/leds/${key}/state`), newState);
          await push(ref(db, `users/${uid}/messages`), {
            text: `${action}${pin}: GPIO${pin} ${newState ? 'ON' : 'OFF'}`,
            timestamp: Math.floor(Date.now() / 1000),
            source: 'esp32',
          });
          toast.success(`GPIO ${pin} ${newState ? 'turned ON' : 'turned OFF'}`);
        } else {
          await push(ref(db, `users/${uid}/messages`), {
            text: `ERR: pin ${pin} not found in /leds`,
            timestamp: Math.floor(Date.now() / 1000),
            source: 'esp32',
          });
          toast.error(`No LED on GPIO ${pin}. Add it in LED Control first.`);
        }
        setInput('');
      } catch (err) {
        toast.error(err.message || 'Command failed.');
      } finally {
        setSending(false);
      }
      return;
    }

    // Plain text → send to backend (writes to pendingMessage for real ESP32 to poll)
    try {
      const res = await apiFetch('/api/messages', {
        method: 'POST',
        body:   JSON.stringify({ text: cmd }),
      });
      if (!res.ok) throw new Error((await res.json()).error);
      setInput('');
      toast.success('Message sent.');
    } catch (err) {
      toast.error(err.message || 'Failed to send message.');
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Messages</h1>
        <p className="text-gray-500 text-sm mt-1">
          Commands you send appear on the right. Responses and status updates from the ESP32 appear on the left.
        </p>
      </div>

      {/* Chat-style message list */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 h-96 overflow-y-auto flex flex-col gap-2.5">
        {loading ? (
          <div className="flex items-center justify-center h-full">
            <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : messages.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-500 text-sm">
            No messages yet. Send a command below.
          </div>
        ) : (
          messages.map((msg) => {
            const isWeb   = msg.source === 'web';
            const isESP32 = msg.source === 'esp32';
            // web = sent by user → right side blue
            // esp32 = firmware status update → left side, amber tint
            // serial = raw serial echo → left side, neutral
            return (
              <div key={msg.id} className={`flex ${isWeb ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-xs px-3.5 py-2 rounded-xl text-sm ${
                    isWeb
                      ? 'bg-blue-600 text-white rounded-br-sm'
                      : isESP32
                        ? 'bg-amber-900/40 border border-amber-800/50 text-amber-200 rounded-bl-sm'
                        : 'bg-gray-800 border border-gray-700 text-gray-200 rounded-bl-sm'
                  }`}
                >
                  <p className="font-mono">{msg.text}</p>
                  <p className={`text-xs mt-0.5 ${isWeb ? 'text-blue-200' : 'text-gray-500'}`}>
                    {new Date(msg.timestamp * 1000).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})}
                    {' · '}
                    {isESP32 ? 'ESP32' : isWeb ? 'you' : 'serial'}
                  </p>
                </div>
              </div>
            );
          })
        )}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSend} className="flex gap-3">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          maxLength={32}
          placeholder='Type a command, e.g. "A2" or "S18"'
          className="flex-1 bg-gray-900 border border-gray-700 rounded-lg px-4 py-2.5 text-white
                     placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
        />
        <button
          type="submit"
          disabled={sending || !input.trim()}
          className="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white
                     px-6 py-2.5 rounded-lg font-medium transition-colors whitespace-nowrap"
        >
          {sending ? 'Sending…' : 'Send'}
        </button>
      </form>

      <p className="text-gray-500 text-xs">
        Commands: <code className="bg-gray-800 px-1.5 py-0.5 rounded">A&lt;pin&gt;</code> → turn ON &nbsp;|&nbsp;
        <code className="bg-gray-800 px-1.5 py-0.5 rounded">S&lt;pin&gt;</code> → turn OFF &nbsp;|&nbsp;
        Max 32 characters.
      </p>
    </div>
  );
}
