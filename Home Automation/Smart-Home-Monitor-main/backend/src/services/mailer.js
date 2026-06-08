/**
 * Nodemailer transporter – Gmail SMTP via App Password.
 *
 * To obtain a Gmail App Password:
 *   1. Enable 2-Step Verification on your Google account.
 *   2. Visit: myaccount.google.com/apppasswords
 *   3. Generate a password for "Mail" and paste it in GMAIL_APP_PASSWORD.
 */
const nodemailer = require('nodemailer');

let transporter = null;

function getTransporter() {
  if (transporter) return transporter;
  transporter = nodemailer.createTransport({
    service: 'gmail',
    auth: {
      user: process.env.GMAIL_USER,
      pass: process.env.GMAIL_APP_PASSWORD,
    },
  });
  return transporter;
}

/**
 * Send a flood-alert email.
 * @param {{ id: string, timestamp: number, sensorValue: number }} event
 * @param {string} to  Recipient email address (read from Firebase at alert time)
 */
async function sendFloodAlert(event, to) {
  const from    = process.env.GMAIL_USER;
  const dateStr = new Date(event.timestamp * 1000).toLocaleString();

  await getTransporter().sendMail({
    from,
    to,
    subject: '⚠️ Flood Detected – Smart Home Monitor',
    html: `
      <div style="font-family:sans-serif;max-width:480px">
        <h2 style="color:#dc2626">⚠️ Flood Alert</h2>
        <p>A flood event was detected by your Smart Home Monitor.</p>
        <table style="border-collapse:collapse;width:100%">
          <tr style="background:#f3f4f6">
            <td style="padding:8px 12px;font-weight:bold">Time</td>
            <td style="padding:8px 12px">${dateStr}</td>
          </tr>
          <tr>
            <td style="padding:8px 12px;font-weight:bold">Sensor Value</td>
            <td style="padding:8px 12px">${event.sensorValue}</td>
          </tr>
          <tr style="background:#f3f4f6">
            <td style="padding:8px 12px;font-weight:bold">Event ID</td>
            <td style="padding:8px 12px">${event.id || 'N/A'}</td>
          </tr>
        </table>
        <p style="margin-top:16px;color:#6b7280;font-size:12px">
          This alert was sent automatically by your Smart Home Monitor backend.
        </p>
      </div>
    `,
  });

  console.log(`[Mailer] Flood alert sent to ${to} (event ts=${event.timestamp})`);
}

module.exports = { sendFloodAlert };
