# -*- coding: utf-8 -*-
"""
Locates the pinned ASP.NET Core development certificate.

TLS verification stays ON in every script here. The development certificate is not in a public
trust store, so instead of disabling verification it is exported next to these scripts and
pinned as the certificate authority:

    dotnet dev-certs https --export-path tools/api-tests/ensa-dev-cert.pem --format PEM --no-password

Set ENSA_DEV_CERT to override the location.
"""
import os
import ssl

ENV_VARIABLE = "ENSA_DEV_CERT"
FILE_NAME = "ensa-dev-cert.pem"

EXPORT_HINT = (
    "The development certificate was not found. Export it with:\n"
    "  dotnet dev-certs https --export-path tools/api-tests/" + FILE_NAME +
    " --format PEM --no-password"
)


def certificate_path():
    """Returns the path of the pinned certificate, or exits with an actionable message."""
    from_environment = os.environ.get(ENV_VARIABLE)
    if from_environment:
        return from_environment

    local = os.path.join(os.path.dirname(os.path.abspath(__file__)), FILE_NAME)
    if os.path.exists(local):
        return local

    raise SystemExit(EXPORT_HINT)


def ssl_context():
    """An SSL context that trusts the pinned development certificate and nothing less."""
    return ssl.create_default_context(cafile=certificate_path())
