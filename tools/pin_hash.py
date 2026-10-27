#!/usr/bin/env python3
# (c) Ahmed Alamrani. All rights reserved.
"""Prints the certificate pin the game expects for a server certificate (PEM file or host:port).

The pin is base64(SHA-256(public key bytes)), the same value CertPins.PinOf computes from X509Certificate2.GetPublicKey().
Needs the openssl command line tool.   Usage:  pin_hash.py cert.pem   |   pin_hash.py api.example.com:443
"""
import base64, hashlib, subprocess, sys


def run(cmd, data=None):
    return subprocess.run(cmd, input=data, stdout=subprocess.PIPE, check=True).stdout


def der_len(b, i):
    n = b[i]
    if n < 0x80:
        return n, i + 1
    k = n & 0x7F
    return int.from_bytes(b[i + 1:i + 1 + k], "big"), i + 1 + k


def raw_key_from_spki(spki):
    # SubjectPublicKeyInfo ::= SEQUENCE { algorithm SEQUENCE, subjectPublicKey BIT STRING }
    _, i = der_len(spki, 1)
    alg_len, j = der_len(spki, i + 1)
    k = j + alg_len
    assert spki[k] == 0x03, "unexpected key structure"
    bit_len, m = der_len(spki, k + 1)
    return spki[m + 1:m + bit_len]   # skip the "unused bits" byte


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    src = sys.argv[1]
    if ":" in src and not src.endswith(".pem"):
        pem = run(["openssl", "s_client", "-connect", src, "-servername", src.split(":")[0]], b"")
        pem = run(["openssl", "x509"], pem)
    else:
        pem = open(src, "rb").read()
    pub = run(["openssl", "x509", "-noout", "-pubkey"], pem)
    spki = run(["openssl", "pkey", "-pubin", "-outform", "DER"], pub)
    print(base64.b64encode(hashlib.sha256(raw_key_from_spki(spki)).digest()).decode())


main()
