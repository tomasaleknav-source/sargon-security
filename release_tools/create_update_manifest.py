#!/usr/bin/env python3
"""Create the SARGAS-UPDATE-1 RSA/SHA-256 envelope used by UpdateGate.
This tool signs metadata only. It never publishes, downloads or installs updates.
Keep the signing key outside the release package.
"""
import argparse
import base64
import hashlib
import os
from pathlib import Path
import subprocess
import sys
import time


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--installer', required=True, type=Path)
    p.add_argument('--private-key', required=True, type=Path)
    p.add_argument('--version', required=True, type=int)
    p.add_argument('--valid-days', type=int, default=30)
    p.add_argument('--output', required=True, type=Path)
    args = p.parse_args()
    if args.version < 1 or args.version > 2147483647 or not 1 <= args.valid_days <= 89:
        p.error('Version must be 1..2147483647 and valid-days 1..89')
    if not args.installer.is_file() or not args.private_key.is_file():
        p.error('Installer and signing key must be existing files')
    size = args.installer.stat().st_size
    if not 1 <= size <= 536870912:
        p.error('Installer size must be 1..536870912 bytes')
    digest = hashlib.sha256()
    with args.installer.open('rb') as source:
        while True:
            block = source.read(1024 * 1024)
            if not block:
                break
            digest.update(block)
    # Fail closed unless OpenSSL identifies a private RSA key of the required size.
    info = subprocess.run(['openssl', 'rsa', '-in', str(args.private_key), '-text', '-noout', '-check', '-passin', 'pass:'], input=b'\n', capture_output=True, check=True).stdout.decode('ascii', 'strict')
    import re
    match = re.search(r'Private-Key: \((\d+) bit', info)
    if not match or not 2048 <= int(match.group(1)) <= 8192:
        raise ValueError('RSA private key must be 2048–8192 bits')
    expires = int(time.time() * 1000) + args.valid_days * 86400000
    payload = ('platform=windows\nversion=%d\nexpires=%d\nbytes=%d\nsha256=%s\n' % (args.version, expires, size, digest.hexdigest())).encode('ascii')
    signature = subprocess.run(['openssl', 'dgst', '-sha256', '-sign', str(args.private_key), '-passin', 'pass:'], input=payload, capture_output=True, check=True).stdout
    envelope = b'SARGAS-UPDATE-1\n' + base64.b64encode(signature) + b'\n' + payload
    if len(envelope) > 4096:
        raise ValueError('Signed envelope exceeds the supported limit')
    # Exclusive creation prevents overwriting existing release artifacts.
    with args.output.open('xb') as target:
        target.write(envelope)
    print('Signed manifest created:', args.output)
    print('Version:', args.version, 'Bytes:', size, 'SHA256:', digest.hexdigest())
    print('This does not publish, install, or enable automatic updates.')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, subprocess.CalledProcessError) as exc:
        print('Manifest not created:', type(exc).__name__, file=sys.stderr)
        sys.exit(1)
