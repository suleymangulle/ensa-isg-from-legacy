# -*- coding: utf-8 -*-
"""
Minimal SMTP server for testing the delivery worker.

Speaks just enough of the protocol for System.Net.Mail.SmtpClient. Every line of the
conversation is logged so a handshake failure is visible, and accepted messages are appended to
a JSON lines file for the test to assert on.
"""
import json
import socketserver
import sys
import threading

HOST = "127.0.0.1"
PORT = 2525
OUTPUT = sys.argv[1] if len(sys.argv) > 1 else "received_mail.jsonl"

_lock = threading.Lock()


def log(direction, text):
    print("%s %s" % (direction, text[:120]), flush=True)


class Handler(socketserver.StreamRequestHandler):
    timeout = 30

    def send(self, line):
        log("S:", line)
        self.wfile.write((line + "\r\n").encode())
        self.wfile.flush()

    def handle(self):
        log("--", "connection from %s" % (self.client_address,))
        self.send("220 fake-smtp ready")

        sender = None
        recipients = []
        auth_step = 0

        while True:
            try:
                raw = self.rfile.readline()
            except OSError as error:
                log("--", "read failed: %s" % error)
                return

            if not raw:
                log("--", "client closed")
                return

            line = raw.decode("utf-8", "replace").rstrip("\r\n")
            upper = line.upper()

            if auth_step == 1:
                log("C:", "<username>")
                auth_step = 2
                self.send("334 UGFzc3dvcmQ6")
                continue

            if auth_step == 2:
                log("C:", "<password>")
                auth_step = 0
                self.send("235 2.7.0 Authentication successful")
                continue

            log("C:", line)

            if upper.startswith(("EHLO", "HELO")):
                self.send("250-fake-smtp Hello")
                self.send("250-AUTH LOGIN PLAIN")
                self.send("250-SIZE 52428800")
                self.send("250 8BITMIME")
            elif upper.startswith("AUTH LOGIN"):
                # RFC 4954 allows an initial response on the AUTH line, and .NET uses it:
                # "AUTH login <base64-username>". Prompting for the username again then puts
                # the exchange one step out of phase and the client gives up.
                if len(line.split()) > 2:
                    auth_step = 2
                    self.send("334 UGFzc3dvcmQ6")
                else:
                    auth_step = 1
                    self.send("334 VXNlcm5hbWU6")
            elif upper.startswith("AUTH PLAIN"):
                self.send("235 2.7.0 Authentication successful")
            elif upper.startswith("MAIL FROM"):
                sender = line.split(":", 1)[1].strip()
                self.send("250 2.1.0 Sender OK")
            elif upper.startswith("RCPT TO"):
                recipients.append(line.split(":", 1)[1].strip())
                self.send("250 2.1.5 Recipient OK")
            elif upper == "DATA":
                self.send("354 Start mail input; end with <CRLF>.<CRLF>")
                body = []
                while True:
                    chunk = self.rfile.readline()
                    if not chunk:
                        return
                    text = chunk.decode("utf-8", "replace").rstrip("\r\n")
                    if text == ".":
                        break
                    body.append(text)

                record = {"sender": sender, "recipients": recipients, "raw": "\n".join(body)}
                with _lock:
                    with open(OUTPUT, "a", encoding="utf-8") as handle:
                        handle.write(json.dumps(record, ensure_ascii=False) + "\n")

                log("--", "message stored (%d recipients)" % len(recipients))
                self.send("250 2.0.0 Message accepted")
                sender, recipients = None, []
            elif upper == "QUIT":
                self.send("221 2.0.0 Bye")
                return
            elif upper == "RSET":
                sender, recipients = None, []
                self.send("250 2.0.0 OK")
            elif upper == "NOOP":
                self.send("250 2.0.0 OK")
            else:
                self.send("250 2.0.0 OK")


class Server(socketserver.ThreadingTCPServer):
    allow_reuse_address = True
    daemon_threads = True

    def handle_error(self, request, client_address):
        # A client that resets the connection is normal here; the traceback is noise.
        log("--", "client %s disconnected abruptly" % (client_address,))


log("--", "fake SMTP listening on %s:%d -> %s" % (HOST, PORT, OUTPUT))
Server((HOST, PORT), Handler).serve_forever()
