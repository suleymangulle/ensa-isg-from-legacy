#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Draws the Ensa database as entity-relationship diagrams in SVG.

Reads the live schema of a SQL Server database through ``sqlcmd`` and writes:

* ``diagrams/ensa-database.svg`` -- every table, every column, every relationship
* ``diagrams/ensa-modules.svg``  -- one card per module, with the traffic between them
* ``diagrams/modules/<module>.svg`` -- a module and the tables it points at

**Most relationships are inferred rather than read.** The architecture forbids navigation
properties, so the database declares only nine foreign key constraints -- all of them brought
in by Identity and OpenIddict. Everywhere else a relationship is an ``int`` column named after
the table it points at. ``infer_relationships`` sets out the three sources it uses and their
order of authority.

Two columns are deliberately *not* drawn as edges: ``TenantId`` and ``CompanyId`` appear on
123 and 37 tables, and that many lines converging on one box would hide the schema instead of
showing it. They are header badges instead.

The connection string is never stored here. It comes from the same place the application
reads it: ``appsettings.Development.local.json``, which is git-ignored.

Usage::

    python tools/gen-diagram/gen_schema_diagram.py
    python tools/gen-diagram/gen_schema_diagram.py --connection "Server=...;Database=..."
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
from collections import defaultdict
from dataclasses import dataclass, field

from table_notes_tr import MODULE_NOTES, NOTES

REPOSITORY = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CONFIGURATION_DIRECTORY = os.path.join(REPOSITORY, "src", "Ensa.HttpApi.Host")
CONFIGURATION_FILES = ["appsettings.Development.local.json", "appsettings.Development.json"]
ENTITY_CONFIGURATIONS = os.path.join(REPOSITORY, "src", "Ensa.EntityFrameworkCore", "Configurations")
OUTPUT_DIRECTORY = os.path.join(REPOSITORY, "diagrams")
SCHEMA = "ensa"

# ---------------------------------------------------------------------------
# What the diagram deliberately does not draw as an edge
# ---------------------------------------------------------------------------

# Present on nearly every table and identical everywhere. Collapsed into one footer line.
AUDIT_COLUMNS = [
    "CreationTime",
    "CreatorId",
    "LastModificationTime",
    "LastModifierId",
    "DeletionTime",
    "DeleterId",
    "IsDeleted",
]

# The two ambient scopes. Both are real relationships; both are drawn as a header badge,
# because an edge from every table to the same box is a scribble, not information.
SCOPE_COLUMNS = {"TenantId": "T", "CompanyId": "C"}

# Ends in "Id" and is not a key: a citizen's identity number.
NOT_A_KEY = {"NationalId", "Id"}

# Columns whose target the naming rule cannot reach, each read out of the XML documentation
# on the property itself rather than guessed.
EXPLICIT_REFERENCES = {
    ("CompanyEmployee", "AssignedDepartmentId"): "WorkplaceDepartment",
    ("FieldObservationReport", "DepartmentId"): "WorkplaceDepartment",
    ("Incident", "DepartmentId"): "WorkplaceDepartment",
    ("CashTransaction", "ExitItemId"): "ExpenseCategory",
    ("Form", "CategoryId"): "FormCategory",
    ("WorkPlan", "PreviousPlanId"): "WorkPlan",
    ("WorkPlanLine", "PreviousLineId"): "WorkPlanLine",
    ("TrainingPlanLine", "PreviousLineId"): "TrainingPlanLine",
    ("YearEndReviewLine", "ParentLineId"): "YearEndReviewLine",
    ("IbysWorkEquipment", "ParentCategoryId"): "IbysEquipmentTopCategory",
    ("EPrescriptionMedication", "UsageMethodId"): "MedicationRoute",
    ("EPrescriptionMedication", "UsageDoseUnitId"): "MedicationDoseUnit",
    ("EPrescriptionMedication", "UsagePeriodUnitId"): "MedicationFrequencyUnit",
}

# References with no fixed table: which one they point at is decided at runtime by a sibling
# discriminator column. Drawn as a hollow marker and no edge, because an edge would be a lie.
POLYMORPHIC_REFERENCES = {
    ("Activity", "RelationId"): "RelatedTable",
    ("Archive", "LineId"): "the archived module",
    ("CashTransaction", "SourceRecordId"): "SourceModule",
    ("CompanyLedgerEntry", "OperationId"): "SourceModule",
    ("Document", "OwnerRecordId"): "OwnerType",
    ("IdentifiedHazard", "SourceId"): "SourceType",
    ("Message", "RecipientId"): "user or employee",
    ("Message", "SenderId"): "user or employee",
    ("NumberSequence", "ScopeId"): "Type",
    ("PermissionScope", "LinkTargetId"): "LinkType",
    ("RouteOriginDistance", "OriginId"): "city or district",
}

# Modules the configuration folders do not name, because these tables come from
# ASP.NET Core Identity and OpenIddict rather than from the domain.
FRAMEWORK_MODULES = {
    "User": "Membership",
    "UserClaim": "Membership",
    "UserLogin": "Membership",
    "UserRole": "Membership",
    "UserToken": "Membership",
    "Role": "Membership",
    "RoleClaim": "Membership",
    "OpenIddictApplications": "Identity",
    "OpenIddictAuthorizations": "Identity",
    "OpenIddictScopes": "Identity",
    "OpenIddictTokens": "Identity",
    "__EnsaMigrationsHistory": "Infrastructure",
}

# Reading order: who owns the system, then who uses it, then what it records.
MODULE_ORDER = [
    "Tenancy",
    "Membership",
    "Identity",
    "Menus",
    "Companies",
    "Lookups",
    "Risks",
    "Plans",
    "Trainings",
    "Health",
    "Documents",
    "Finance",
    "Ibys",
    "Communication",
    "Reports",
    "Infrastructure",
]

MODULE_COLOURS = {
    "Tenancy": "#4338ca",
    "Membership": "#7c3aed",
    "Identity": "#a21caf",
    "Menus": "#0369a1",
    "Companies": "#0f766e",
    "Lookups": "#0e7490",
    "Risks": "#b91c1c",
    "Plans": "#c2410c",
    "Trainings": "#a16207",
    "Health": "#15803d",
    "Documents": "#475569",
    "Finance": "#9f1239",
    "Ibys": "#1d4ed8",
    "Communication": "#6d28d9",
    "Reports": "#854d0e",
    "Infrastructure": "#6b7280",
}

MODULE_CAPTIONS = {
    "Tenancy": "the provider organisations that run the system, their offices and contracts",
    "Membership": "users, roles and the permission catalogue",
    "Identity": "OpenIddict token, authorisation and client storage",
    "Menus": "the navigation tree and per-tenant menu overrides",
    "Companies": "customer companies, their departments, employees and equipment",
    "Lookups": "provinces, districts, activities, hazard classes and other reference data",
    "Risks": "risk assessments, identified hazards, corrective actions and incidents",
    "Plans": "annual work plans and the lines that are ticked off against them",
    "Trainings": "training catalogue, plans, attendance and certificates",
    "Health": "medical examinations, prescriptions, medications and diagnoses",
    "Documents": "stored files and everything that attaches one to a record",
    "Finance": "invoices, cash registers, ledgers and penalties",
    "Ibys": "submissions to the ministry notification service",
    "Communication": "site visits, outbound mail, notifications and support requests",
    "Reports": "generated report definitions and their saved output",
    "Infrastructure": "Entity Framework bookkeeping",
}

# ---------------------------------------------------------------------------
# Geometry
# ---------------------------------------------------------------------------

CELL_WIDTH = 302
COLUMN_GAP = 24
TABLE_GAP = 24
ROW_HEIGHT = 15
TABLE_HEADER = 30
TABLE_FOOTER = 21
BAND_HEADER = 46
BAND_GAP = 34
BAND_PADDING = 18
MARGIN = 34
TITLE_HEIGHT = 132


@dataclass
class Column:
    name: str
    type_name: str
    nullable: bool
    is_key: bool
    references: str | None = None
    polymorphic: str | None = None


@dataclass
class Table:
    name: str
    module: str
    rows: int
    columns: list[Column] = field(default_factory=list)
    audit: list[str] = field(default_factory=list)
    scopes: list[str] = field(default_factory=list)
    # The legacy table this one came from, read from the entity's own XML documentation.
    legacy: str = ""
    # How many relationships leave this table, and how many arrive at it.
    outgoing: int = 0
    incoming: int = 0
    # Filled in by the layout pass.
    x: float = 0.0
    y: float = 0.0

    @property
    def height(self) -> float:
        footer = TABLE_FOOTER if (self.audit or self.rows) else 0
        return TABLE_HEADER + ROW_HEIGHT * len(self.columns) + footer + 6

    def row_centre(self, column_name: str) -> float:
        """Vertical centre of one column's row, so an edge can leave from the field itself."""
        for index, column in enumerate(self.columns):
            if column.name == column_name:
                return self.y + TABLE_HEADER + ROW_HEIGHT * index + ROW_HEIGHT / 2
        return self.y + TABLE_HEADER


@dataclass
class Relationship:
    source: str
    column: str
    target: str


# ---------------------------------------------------------------------------
# Reading the schema
# ---------------------------------------------------------------------------


def find_sqlcmd() -> str:
    found = shutil.which("sqlcmd") or shutil.which("SQLCMD.EXE")
    if found:
        return found

    roots = [
        r"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC",
        r"C:\Program Files (x86)\Microsoft SQL Server\Client SDK\ODBC",
    ]
    candidates = []
    for root in roots:
        if not os.path.isdir(root):
            continue
        for version in os.listdir(root):
            path = os.path.join(root, version, "Tools", "Binn", "SQLCMD.EXE")
            if os.path.isfile(path):
                candidates.append((version, path))

    if not candidates:
        sys.exit("sqlcmd was not found. Install the SQL Server command line tools, or put sqlcmd on PATH.")

    return sorted(candidates)[-1][1]


def read_connection_string(explicit: str | None) -> str:
    if explicit:
        return explicit

    from_environment = os.environ.get("ENSA_CONNECTION")
    if from_environment:
        return from_environment

    for name in CONFIGURATION_FILES:
        path = os.path.join(CONFIGURATION_DIRECTORY, name)
        if not os.path.isfile(path):
            continue
        with open(path, encoding="utf-8-sig") as handle:
            settings = json.load(handle)
        value = settings.get("ConnectionStrings", {}).get("Default")
        if value:
            print(f"connection string taken from {name}")
            return value

    sys.exit(
        "No connection string. Pass --connection, set ENSA_CONNECTION, or add one to "
        "src/Ensa.HttpApi.Host/appsettings.Development.local.json."
    )


def parse_connection_string(value: str) -> dict[str, str]:
    parts: dict[str, str] = {}
    for piece in value.split(";"):
        if "=" not in piece:
            continue
        key, _, item = piece.partition("=")
        parts[key.strip().lower().replace(" ", "")] = item.strip()
    return parts


def run_query(sqlcmd: str, connection: dict[str, str], sql: str) -> list[list[str]]:
    """Runs one query and returns its rows split on the field separator."""
    server = connection.get("server") or connection.get("datasource") or "localhost"
    database = connection.get("database") or connection.get("initialcatalog")
    user = connection.get("userid") or connection.get("uid")
    password = connection.get("password") or connection.get("pwd")

    with tempfile.TemporaryDirectory() as workspace:
        query_file = os.path.join(workspace, "query.sql")
        output_file = os.path.join(workspace, "output.txt")
        with open(query_file, "w", encoding="utf-8", newline="\r\n") as handle:
            handle.write("SET NOCOUNT ON;\n" + sql)

        command = [sqlcmd, "-S", server, "-C", "-l", "60", "-h", "-1", "-W", "-s", "~"]
        if database:
            command += ["-d", database]
        if user:
            command += ["-U", user, "-P", password or ""]
        else:
            command += ["-E"]
        command += ["-i", query_file, "-o", output_file]

        completed = subprocess.run(command, capture_output=True, text=True)
        if not os.path.isfile(output_file):
            sys.exit(f"sqlcmd failed: {completed.stdout}{completed.stderr}")

        with open(output_file, encoding="utf-8", errors="replace") as handle:
            text = handle.read()

    if "Msg " in text and "Level " in text:
        sys.exit(f"the query failed:\n{text}")

    rows = []
    for line in text.splitlines():
        line = line.strip()
        if not line or "~" not in line:
            continue
        rows.append([field.strip() for field in line.split("~")])
    return rows


COLUMN_QUERY = f"""
SELECT t.name, c.name, TYPE_NAME(c.user_type_id), c.max_length, c.precision, c.scale,
       c.is_nullable,
       CASE WHEN EXISTS (SELECT 1 FROM sys.index_columns ic
                         JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                         WHERE i.is_primary_key = 1
                           AND ic.object_id = c.object_id AND ic.column_id = c.column_id)
            THEN 1 ELSE 0 END
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
WHERE SCHEMA_NAME(t.schema_id) = '{SCHEMA}'
ORDER BY t.name, c.column_id;
"""

FOREIGN_KEY_QUERY = f"""
SELECT OBJECT_NAME(fk.parent_object_id), pc.name, OBJECT_NAME(fk.referenced_object_id)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
WHERE SCHEMA_NAME(fk.schema_id) = '{SCHEMA}';
"""

ROW_COUNT_QUERY = f"""
SELECT t.name, SUM(CASE WHEN p.index_id IN (0, 1) THEN p.rows ELSE 0 END)
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id
WHERE SCHEMA_NAME(t.schema_id) = '{SCHEMA}'
GROUP BY t.name
ORDER BY t.name;
"""


def format_type(type_name: str, max_length: int, precision: int, scale: int) -> str:
    if type_name in ("nvarchar", "nchar"):
        size = "max" if max_length == -1 else str(max_length // 2)
        return f"{type_name}({size})"
    if type_name in ("varchar", "char", "varbinary", "binary"):
        size = "max" if max_length == -1 else str(max_length)
        return f"{type_name}({size})"
    if type_name in ("decimal", "numeric"):
        return f"{type_name}({precision},{scale})"
    return type_name


def read_module_map() -> dict[str, str]:
    """Which module a table belongs to, taken from the folder its EF configuration lives in."""
    mapping = dict(FRAMEWORK_MODULES)
    if not os.path.isdir(ENTITY_CONFIGURATIONS):
        return mapping

    for module in sorted(os.listdir(ENTITY_CONFIGURATIONS)):
        folder = os.path.join(ENTITY_CONFIGURATIONS, module)
        if not os.path.isdir(folder):
            continue
        for name in os.listdir(folder):
            if not name.endswith("Configuration.cs"):
                continue
            with open(os.path.join(folder, name), encoding="utf-8-sig") as handle:
                source = handle.read()
            match = re.search(r'ToTable\("([^"]+)"', source)
            if match:
                mapping[match.group(1)] = module
    return mapping


def read_legacy_names() -> dict[str, str]:
    """The legacy table each modern table came from.

    Taken from the entity's own XML documentation -- every entity records its origin as
    ``Legacy equivalent: <c>Xxx_T</c>`` -- rather than from a list kept alongside the notes,
    so it cannot drift out of step with the code.
    """
    legacy: dict[str, str] = {}
    if not os.path.isdir(ENTITY_CONFIGURATIONS):
        return legacy

    domain = os.path.join(REPOSITORY, "src", "Ensa.Domain")
    entities: dict[str, str] = {}
    for root, _, files in os.walk(domain):
        for name in files:
            if name.endswith(".cs"):
                entities.setdefault(name[:-3], os.path.join(root, name))

    for module in sorted(os.listdir(ENTITY_CONFIGURATIONS)):
        folder = os.path.join(ENTITY_CONFIGURATIONS, module)
        if not os.path.isdir(folder):
            continue
        for name in os.listdir(folder):
            if not name.endswith("Configuration.cs"):
                continue
            with open(os.path.join(folder, name), encoding="utf-8-sig") as handle:
                source = handle.read()
            table = re.search(r'ToTable\("([^"]+)"', source)
            entity = entities.get(name[: -len("Configuration.cs")])
            if not table or not entity:
                continue
            with open(entity, encoding="utf-8-sig") as handle:
                text = handle.read()

            # Only the class-level documentation. Searching the whole file picks up a legacy
            # *column* mentioned further down -- which is how Document once came out as
            # "DosyaTuru" when its own summary says Document_T.
            block = re.search(
                r"((?:^[ \t]*///.*\n)+)[ \t]*public (?:sealed |abstract )?class ",
                text,
                re.M,
            )
            if not block:
                continue

            # "Legacy equivalent: X" is the deliberate statement of origin; a bare "Legacy: X"
            # is often an aside about one column, so it is only the fallback.
            for pattern in (
                r"legacy equivalent:?\s*<c>([^<]+)</c>",
                r"legacy:?\s*<c>([^<]+)</c>",
            ):
                found = re.search(pattern, block.group(1), re.IGNORECASE)
                if found:
                    legacy[table.group(1)] = found.group(1).strip()
                    break

    return legacy


def read_schema(sqlcmd: str, connection: dict[str, str]) -> dict[str, Table]:
    modules = read_module_map()
    legacy = read_legacy_names()
    counts = {name: int(value) for name, value in run_query(sqlcmd, connection, ROW_COUNT_QUERY)}

    tables: dict[str, Table] = {}
    for row in run_query(sqlcmd, connection, COLUMN_QUERY):
        table_name, column_name, type_name, max_length, precision, scale, nullable, is_key = row

        table = tables.get(table_name)
        if table is None:
            table = Table(
                name=table_name,
                module=modules.get(table_name, "Infrastructure"),
                rows=counts.get(table_name, 0),
                legacy=legacy.get(table_name, ""),
            )
            tables[table_name] = table

        if column_name in AUDIT_COLUMNS:
            table.audit.append(column_name)
            continue

        if column_name in SCOPE_COLUMNS:
            table.scopes.append(SCOPE_COLUMNS[column_name])
            continue

        table.columns.append(
            Column(
                name=column_name,
                type_name=format_type(type_name, int(max_length), int(precision), int(scale)),
                nullable=nullable == "1",
                is_key=is_key == "1",
            )
        )

    return tables


# ---------------------------------------------------------------------------
# Inferring the relationships
# ---------------------------------------------------------------------------


def infer_relationships(
    tables: dict[str, Table],
    declared: list[tuple[str, str, str]],
) -> list[Relationship]:
    """Works out which column points at which table.

    Three sources, in descending order of authority:

    1. **The constraints the database declares.** There are only nine of them -- Identity and
       OpenIddict brought their own -- but where one exists it is the truth.
    2. **The property documentation**, for the handful of columns whose name does not say where
       they point (``Incident.DepartmentId`` goes to ``WorkplaceDepartment``). Read out of the
       entity source, never guessed; see ``EXPLICIT_REFERENCES``.
    3. **The naming rule.** Exact first: ``CityId`` points at ``City``. Then by suffix, because a
       column often says which *role* the row plays -- ``PhysicianUserId`` and ``ApproverUserId``
       both point at ``User``, and ``LogoDocumentId`` at ``Document``. The longest matching table
       name wins, and a match shorter than four characters is refused as coincidence.

    Columns in ``POLYMORPHIC_REFERENCES`` are excluded from all three: their target is chosen at
    runtime by a sibling discriminator, so any single edge would be wrong.
    """
    by_name = {name: name for name in tables}
    longest_first = sorted(tables, key=len, reverse=True)
    from_constraints = {(source, column): target for source, column, target in declared}

    relationships: list[Relationship] = []
    for table in tables.values():
        for column in table.columns:
            key = (table.name, column.name)

            if key in POLYMORPHIC_REFERENCES:
                column.polymorphic = POLYMORPHIC_REFERENCES[key]
                continue

            target = from_constraints.get(key) or EXPLICIT_REFERENCES.get(key)

            if target is None:
                if not column.name.endswith("Id") or column.name in NOT_A_KEY or column.is_key:
                    continue

                stem = column.name[:-2]
                target = by_name.get(stem)
                if target is None:
                    for candidate in longest_first:
                        if len(candidate) >= 4 and stem.endswith(candidate):
                            target = candidate
                            break

            if target is None or target not in tables:
                continue

            column.references = target
            relationships.append(Relationship(table.name, column.name, target))

    return relationships


# ---------------------------------------------------------------------------
# Layout
# ---------------------------------------------------------------------------


@dataclass
class Band:
    module: str
    y: float
    height: float
    tables: list[Table]


def lay_out(tables: list[Table], columns: int, top: float) -> tuple[list[Band], float, float]:
    """Packs tables into bands, one band per module, masonry inside the band."""
    width = columns * CELL_WIDTH + (columns - 1) * COLUMN_GAP
    grouped: dict[str, list[Table]] = defaultdict(list)
    for table in tables:
        grouped[table.module].append(table)

    bands: list[Band] = []
    y = top
    for module in MODULE_ORDER:
        members = sorted(grouped.get(module, []), key=lambda item: item.name)
        if not members:
            continue

        content_top = y + BAND_HEADER
        column_bottoms = [content_top] * columns
        for table in members:
            index = min(range(columns), key=lambda i: (column_bottoms[i], i))
            table.x = MARGIN + BAND_PADDING + index * (CELL_WIDTH + COLUMN_GAP)
            table.y = column_bottoms[index]
            column_bottoms[index] += table.height + TABLE_GAP

        bottom = max(column_bottoms) - TABLE_GAP + BAND_PADDING
        bands.append(Band(module, y, bottom - y, members))
        y = bottom + BAND_GAP

    return bands, MARGIN * 2 + width + BAND_PADDING * 2, y


# ---------------------------------------------------------------------------
# Drawing
# ---------------------------------------------------------------------------


def escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def thousands(value: int) -> str:
    return f"{value:,}".replace(",", " ")


def shorten(text: str, limit: int) -> str:
    return text if len(text) <= limit else text[: limit - 1] + "\u2026"


def draw_defs() -> list[str]:
    parts = [
        "<defs>",
        '<style>'
        ".t{font-family:Segoe UI,Inter,Helvetica,Arial,sans-serif}"
        ".m{font-family:Consolas,Menlo,DejaVu Sans Mono,monospace}"
        "</style>",
        '<filter id="drop" x="-12%" y="-12%" width="130%" height="130%">',
        '<feDropShadow dx="0" dy="1.5" stdDeviation="2.2" flood-color="#0f172a" flood-opacity="0.16"/>',
        "</filter>",
    ]
    for module, colour in MODULE_COLOURS.items():
        key = module.lower()
        parts.append(
            f'<marker id="a-{key}" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="6" '
            f'markerHeight="6" orient="auto-start-reverse">'
            f'<path d="M0 0.6 L8 4 L0 7.4 z" fill="{colour}" opacity="0.85"/></marker>'
        )
    parts.append("</defs>")
    return parts


def turkish_report(table: Table) -> str:
    """The hover report for one table, in Turkish.

    Everything drawn on the page is English, because the identifiers are English. This is the
    one place the reader is spoken to in their own language, so it answers what the picture
    cannot: what a row is, and why the table is shaped the way it is.

    Native SVG tooltips do not reflow reliably, so the prose is wrapped here.
    """
    what, why = NOTES.get(table.name, ("", None))

    lines = [f"{table.name}  ·  {table.module} modülü", ""]

    def block(label: str, text: str) -> None:
        wrapped = wrap(text, 74)
        lines.append(f"{label:<8}{wrapped[0]}")
        lines.extend(" " * 8 + piece for piece in wrapped[1:])

    if what:
        block("NE", what)
    if why:
        block("NİÇİN", why)
    if table.legacy:
        lines.append(f"{'ESKİ':<8}{table.legacy}")

    scope = []
    if "T" in table.scopes:
        scope.append("kiracı (TenantId)")
    if "C" in table.scopes:
        scope.append("firma (CompanyId)")
    lines.append(f"{'KAPSAM':<8}{' · '.join(scope) if scope else 'ortak (host) — kiracıdan bağımsız'}")

    # The box only draws the columns that carry information; the audit and scope columns are
    # collapsed. The total says so, otherwise the tooltip quietly contradicts the schema.
    total = len(table.columns) + len(table.audit) + len(table.scopes)
    hidden = []
    if table.audit:
        hidden.append(f"{len(table.audit)} denetim")
    if table.scopes:
        hidden.append(f"{len(table.scopes)} kapsam")
    breakdown = f" ({len(table.columns)} çizili, {', '.join(hidden)})" if hidden else ""

    lines.append(f"{'BOYUT':<8}{thousands(table.rows)} satır · {total} sütun{breakdown}")
    lines.append(
        f"{'BAĞ':<8}{table.outgoing} ilişki çıkıyor · {table.incoming} ilişki geliyor"
    )

    return "\n".join(lines)


def draw_table(table: Table, dimmed: bool = False) -> list[str]:
    colour = MODULE_COLOURS.get(table.module, "#64748b")
    height = table.height
    opacity = "0.45" if dimmed else "1"

    parts = [f'<g opacity="{opacity}">']
    parts.append(f"<title>{escape(turkish_report(table))}</title>")
    parts.append(
        f'<rect x="{table.x}" y="{table.y}" width="{CELL_WIDTH}" height="{height:.0f}" rx="7" '
        f'fill="#ffffff" stroke="{colour}" stroke-width="1.1" filter="url(#drop)"/>'
    )
    parts.append(
        f'<path d="M{table.x} {table.y + 7} a7 7 0 0 1 7 -7 h{CELL_WIDTH - 14} a7 7 0 0 1 7 7 '
        f'v{TABLE_HEADER - 7} h-{CELL_WIDTH} z" fill="{colour}"/>'
    )
    parts.append(
        f'<text class="t" x="{table.x + 10}" y="{table.y + 20}" font-size="12.5" '
        f'font-weight="600" fill="#ffffff">{escape(shorten(table.name, 32))}</text>'
    )

    badge_x = table.x + CELL_WIDTH - 10
    for badge in reversed(table.scopes):
        badge_x -= 17
        parts.append(
            f'<rect x="{badge_x}" y="{table.y + 8}" width="15" height="14" rx="3.5" '
            f'fill="#ffffff" fill-opacity="0.24"/>'
            f'<text class="m" x="{badge_x + 7.5}" y="{table.y + 18.5}" font-size="9.5" '
            f'font-weight="700" fill="#ffffff" text-anchor="middle">{badge}</text>'
        )

    for index, column in enumerate(table.columns):
        y = table.y + TABLE_HEADER + ROW_HEIGHT * index
        if index % 2 == 1:
            parts.append(
                f'<rect x="{table.x + 1}" y="{y}" width="{CELL_WIDTH - 2}" height="{ROW_HEIGHT}" '
                f'fill="#f1f5f9" opacity="0.7"/>'
            )

        baseline = y + 11
        if column.is_key:
            glyph, glyph_colour, weight = "\u25c6", "#b45309", "600"
        elif column.references:
            glyph, glyph_colour, weight = "\u25b8", MODULE_COLOURS.get(table.module, "#334155"), "500"
        elif column.polymorphic:
            glyph, glyph_colour, weight = "\u25b9", "#64748b", "500"
        else:
            glyph, glyph_colour, weight = "", "#94a3b8", "400"

        if glyph:
            parts.append(
                f'<text class="m" x="{table.x + 9}" y="{baseline}" font-size="8.5" '
                f'fill="{glyph_colour}">{glyph}</text>'
            )

        name = column.name + ("" if not column.nullable else "\u00b7")
        label = shorten(name, 30)

        # The tooltip belongs on the text element itself. A bare <title> dropped into the group
        # would be ignored -- only the first child <title> of a group is ever shown, and that
        # one is the table's own report.
        if column.polymorphic:
            explanation = (
                f"{column.name}\n\n"
                f"Polimorfik anahtar: sabit bir hedef tablosu yok. Hangi tabloya bakt\u0131\u011f\u0131n\u0131 "
                f"{column.polymorphic} belirler,\nbu y\u00fczden diyagramda ok \u00e7izilmiyor \u2014 tek bir "
                f"ok yanl\u0131\u015f olurdu."
            )
        elif column.references:
            explanation = f"{column.name}\n\n\u2192 {column.references} tablosuna i\u015faret ediyor."
        elif column.is_key:
            explanation = f"{column.name}\n\nBirincil anahtar."
        elif label != name:
            explanation = column.name
        else:
            explanation = ""

        hover = f"<title>{escape(explanation)}</title>" if explanation else ""
        parts.append(
            f'<text class="t" x="{table.x + 20}" y="{baseline}" font-size="10" '
            f'font-weight="{weight}" fill="#1e293b">{hover}{escape(label)}</text>'
        )
        parts.append(
            f'<text class="m" x="{table.x + CELL_WIDTH - 9}" y="{baseline}" font-size="8.8" '
            f'fill="#94a3b8" text-anchor="end">{escape(shorten(column.type_name, 16))}</text>'
        )

    if table.audit or table.rows:
        y = table.y + TABLE_HEADER + ROW_HEIGHT * len(table.columns)
        parts.append(
            f'<path d="M{table.x + 1} {y} h{CELL_WIDTH - 2} v{TABLE_FOOTER - 7} '
            f'a6 6 0 0 1 -6 6 h-{CELL_WIDTH - 14} a6 6 0 0 1 -6 -6 z" fill="#f8fafc"/>'
        )
        parts.append(
            f'<line x1="{table.x + 1}" y1="{y}" x2="{table.x + CELL_WIDTH - 1}" y2="{y}" '
            f'stroke="#e2e8f0" stroke-width="1"/>'
        )

        facets = []
        if "CreatorId" in table.audit:
            facets.append("created")
        if "LastModifierId" in table.audit:
            facets.append("modified")
        if "IsDeleted" in table.audit:
            facets.append("soft-delete")
        note = " \u00b7 ".join(facets) if facets else "no audit"

        parts.append(
            f'<text class="t" x="{table.x + 10}" y="{y + 14}" font-size="8.8" '
            f'fill="#94a3b8">{note}</text>'
        )
        parts.append(
            f'<text class="m" x="{table.x + CELL_WIDTH - 9}" y="{y + 14}" font-size="8.8" '
            f'fill="#64748b" text-anchor="end">{thousands(table.rows)} rows</text>'
        )

    parts.append("</g>")
    return parts


def draw_edge(source: Table, column: str, target: Table, bounds: tuple[float, float]) -> str:
    """One relationship, leaving the key column itself and arriving at the target's header.

    ``bounds`` is the drawable width. Two tables in the same column are only 302px apart
    horizontally, which sends the control points a long way sideways -- off the canvas
    entirely when the pair sits in the leftmost column. So the side is chosen by which one
    has room, and the control points are clamped to stay on the page.
    """
    colour = MODULE_COLOURS.get(target.module, "#64748b")
    key = target.module.lower()
    left_bound, right_bound = bounds

    if source.name == target.name:
        # A self reference: a small loop off the right edge, so it reads as recursion.
        y = source.row_centre(column)
        x = source.x + CELL_WIDTH
        return (
            f'<path d="M{x} {y:.1f} c 26 0 26 -17 0 -17" fill="none" stroke="{colour}" '
            f'stroke-width="1.1" opacity="0.55" marker-end="url(#a-{key})"/>'
        )

    start_y = source.row_centre(column)
    end_y = target.y + TABLE_HEADER / 2

    source_centre = source.x + CELL_WIDTH / 2
    target_centre = target.x + CELL_WIDTH / 2

    if abs(target_centre - source_centre) < CELL_WIDTH / 2:
        # Stacked in the same column: leave on whichever side has more free space.
        go_right = (right_bound - (source.x + CELL_WIDTH)) >= (source.x - left_bound)
    else:
        go_right = target_centre >= source_centre

    if go_right:
        start_x, end_x = source.x + CELL_WIDTH, target.x
        reach = max(30.0, min(150.0, abs(end_x - start_x) / 2))
        control = (start_x + reach, end_x - reach)
    else:
        start_x, end_x = source.x, target.x + CELL_WIDTH
        reach = max(30.0, min(150.0, abs(start_x - end_x) / 2))
        control = (start_x - reach, end_x + reach)

    clamp = lambda value: min(max(value, left_bound + 6), right_bound - 6)

    return (
        f'<path d="M{start_x} {start_y:.1f} C{clamp(control[0]):.1f} {start_y:.1f} '
        f'{clamp(control[1]):.1f} {end_y:.1f} {end_x} {end_y:.1f}" fill="none" stroke="{colour}" '
        f'stroke-width="1" opacity="0.34" marker-end="url(#a-{key})"/>'
    )


def draw_band(band: Band, width: float, table_count: int, row_count: int) -> list[str]:
    colour = MODULE_COLOURS[band.module]
    x = MARGIN
    inner = width - MARGIN * 2

    note = MODULE_NOTES.get(band.module, "")
    tooltip = "\n".join(
        [f"{band.module} modülü", ""]
        + wrap(note, 74)
        + ["", f"{table_count} tablo · {thousands(row_count)} satır"]
    )

    return [
        f'<rect x="{x}" y="{band.y}" width="{inner}" height="{band.height:.0f}" rx="12" '
        f'fill="{colour}" fill-opacity="0.045" stroke="{colour}" stroke-opacity="0.22" '
        f'stroke-width="1"><title>{escape(tooltip)}</title></rect>',
        f'<rect x="{x}" y="{band.y}" width="5" height="{band.height:.0f}" rx="2.5" fill="{colour}"/>',
        f'<text class="t" x="{x + 18}" y="{band.y + 26}" font-size="17" font-weight="700" '
        f'fill="{colour}">{escape(band.module)}</text>',
        f'<text class="t" x="{x + 18 + len(band.module) * 10.5 + 14}" y="{band.y + 26}" '
        f'font-size="11" fill="#64748b">{escape(MODULE_CAPTIONS.get(band.module, ""))}</text>',
        f'<text class="t" x="{x + inner - 18}" y="{band.y + 26}" font-size="11" fill="#94a3b8" '
        f'text-anchor="end">{table_count} tables \u00b7 {thousands(row_count)} rows</text>',
    ]


def draw_title(width: float, tables: list[Table], relationships: list[Relationship], database: str) -> list[str]:
    row_total = sum(table.rows for table in tables)
    column_total = sum(len(table.columns) + len(table.audit) + len(table.scopes) for table in tables)

    parts = [
        f'<text class="t" x="{MARGIN + 4}" y="52" font-size="30" font-weight="700" '
        f'fill="#0f172a">Ensa \u2014 database schema</text>',
        f'<text class="t" x="{MARGIN + 4}" y="76" font-size="12.5" fill="#64748b">'
        f'{len(tables)} tables \u00b7 {column_total} columns \u00b7 {len(relationships)} relationships '
        f'\u00b7 {thousands(row_total)} rows \u00b7 schema <tspan class="m">{escape(SCHEMA)}</tspan> '
        f'of <tspan class="m">{escape(database)}</tspan></text>',
    ]

    legend = [
        ("\u25c6", "#b45309", "primary key"),
        ("\u25b8", "#334155", "foreign key, drawn as an edge"),
        ("\u00b7", "#94a3b8", "nullable"),
        ("T", "#334155", "tenant scoped (TenantId)"),
        ("C", "#334155", "company scoped (CompanyId)"),
    ]
    x = MARGIN + 4
    for glyph, colour, label in legend:
        parts.append(
            f'<text class="m" x="{x}" y="103" font-size="10.5" fill="{colour}" '
            f'font-weight="700">{glyph}</text>'
        )
        parts.append(f'<text class="t" x="{x + 13}" y="103" font-size="10.5" fill="#64748b">{label}</text>')
        x += 22 + len(label) * 5.6

    parts.append(
        f'<text class="t" x="{MARGIN + 4}" y="121" font-size="10.5" fill="#94a3b8">'
        f'Audit columns (creation, modification, soft delete) are summarised in each footer. '
        f'TenantId and CompanyId are shown as badges instead of edges: 123 and 37 lines into a '
        f'single box would hide the schema rather than show it.</text>'
    )
    parts.append(
        f'<line x1="{MARGIN}" y1="{TITLE_HEIGHT - 6}" x2="{width - MARGIN}" y2="{TITLE_HEIGHT - 6}" '
        f'stroke="#e2e8f0" stroke-width="1"/>'
    )
    return parts


def document(width: float, height: float, body: list[str], title: str) -> str:
    head = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width:.0f}" height="{height:.0f}" '
        f'viewBox="0 0 {width:.0f} {height:.0f}" font-family="Segoe UI,Helvetica,Arial,sans-serif">',
        f"<title>{escape(title)}</title>",
        f'<rect width="{width:.0f}" height="{height:.0f}" fill="#fbfcfe"/>',
    ]
    return "\n".join(head + draw_defs() + body + ["</svg>"]) + "\n"


# ---------------------------------------------------------------------------
# The three outputs
# ---------------------------------------------------------------------------


def write_full_diagram(tables: dict[str, Table], relationships: list[Relationship], database: str) -> str:
    ordered = list(tables.values())
    bands, width, bottom = lay_out(ordered, columns=7, top=TITLE_HEIGHT)
    height = bottom + MARGIN

    body: list[str] = []
    body += draw_title(width, ordered, relationships, database)

    for band in bands:
        body += draw_band(band, width, len(band.tables), sum(t.rows for t in band.tables))

    body.append('<g id="relationships">')
    for relationship in relationships:
        body.append(
            draw_edge(
                tables[relationship.source],
                relationship.column,
                tables[relationship.target],
                (MARGIN, width - MARGIN),
            )
        )
    body.append("</g>")

    body.append('<g id="tables">')
    for table in ordered:
        body += draw_table(table)
    body.append("</g>")

    path = os.path.join(OUTPUT_DIRECTORY, "ensa-database.svg")
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(document(width, height, body, "Ensa database schema"))
    return path


def write_module_diagram(
    module: str,
    tables: dict[str, Table],
    relationships: list[Relationship],
) -> str | None:
    members = [table for table in tables.values() if table.module == module]
    if not members:
        return None

    names = {table.name for table in members}
    touching = [r for r in relationships if r.source in names or r.target in names]
    neighbours = {r.target for r in touching if r.target not in names}
    neighbours |= {r.source for r in touching if r.source not in names}

    # The module's own tables first, then everything they touch, so a reader sees the module
    # in one place and its dependencies gathered underneath it.
    outside = [tables[name] for name in sorted(neighbours)]

    columns = max(3, min(6, math.ceil(math.sqrt(len(members) + len(outside)))))
    width = MARGIN * 2 + BAND_PADDING * 2 + columns * CELL_WIDTH + (columns - 1) * COLUMN_GAP

    top = 96.0
    positioned: dict[str, Table] = {}
    body: list[str] = []
    colour = MODULE_COLOURS[module]

    for group_name, group, own in (
        (module, sorted(members, key=lambda t: t.name), True),
        ("referenced from other modules", outside, False),
    ):
        if not group:
            continue

        content_top = top + BAND_HEADER
        bottoms = [content_top] * columns
        for table in group:
            index = min(range(columns), key=lambda i: (bottoms[i], i))
            placed = Table(
                name=table.name,
                module=table.module,
                rows=table.rows,
                columns=table.columns,
                audit=table.audit,
                scopes=table.scopes,
                legacy=table.legacy,
                outgoing=table.outgoing,
                incoming=table.incoming,
            )
            placed.x = MARGIN + BAND_PADDING + index * (CELL_WIDTH + COLUMN_GAP)
            placed.y = bottoms[index]
            bottoms[index] += placed.height + TABLE_GAP
            positioned[table.name] = placed

        bottom = max(bottoms) - TABLE_GAP + BAND_PADDING
        band_colour = colour if own else "#94a3b8"

        if own:
            tooltip = "\n".join(
                [f"{module} modülü", ""]
                + wrap(MODULE_NOTES.get(module, ""), 74)
                + ["", f"{len(members)} tablo · {thousands(sum(t.rows for t in members))} satır"]
            )
        else:
            tooltip = (
                "Bu modülün dışındaki tablolar.\n\n"
                "Buradaki kutular başka modüllere ait; yalnızca yukarıdaki tabloların işaret\n"
                "ettiği için çizildiler ve soluk gösteriliyorlar."
            )

        body.append(
            f'<rect x="{MARGIN}" y="{top}" width="{width - MARGIN * 2}" height="{bottom - top:.0f}" '
            f'rx="12" fill="{band_colour}" fill-opacity="0.045" stroke="{band_colour}" '
            f'stroke-opacity="0.22"><title>{escape(tooltip)}</title></rect>'
        )
        body.append(
            f'<text class="t" x="{MARGIN + 18}" y="{top + 26}" font-size="16" font-weight="700" '
            f'fill="{band_colour}">{escape(group_name)}</text>'
        )
        top = bottom + BAND_GAP

    header = [
        f'<text class="t" x="{MARGIN + 4}" y="46" font-size="26" font-weight="700" '
        f'fill="{colour}">{escape(module)}</text>',
        f'<text class="t" x="{MARGIN + 4}" y="68" font-size="12" fill="#64748b">'
        f'{escape(MODULE_CAPTIONS.get(module, ""))} \u2014 {len(members)} tables, '
        f'{thousands(sum(t.rows for t in members))} rows</text>',
        f'<line x1="{MARGIN}" y1="82" x2="{width - MARGIN}" y2="82" stroke="#e2e8f0"/>',
    ]

    edges = ['<g id="relationships">']
    for relationship in touching:
        source = positioned.get(relationship.source)
        target = positioned.get(relationship.target)
        if source and target:
            edges.append(draw_edge(source, relationship.column, target, (MARGIN, width - MARGIN)))
    edges.append("</g>")

    boxes = ['<g id="tables">']
    for name, table in positioned.items():
        boxes += draw_table(table, dimmed=name not in names)
    boxes.append("</g>")

    path = os.path.join(OUTPUT_DIRECTORY, "modules", f"{module.lower()}.svg")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(document(width, top + MARGIN, header + body + edges + boxes, f"Ensa \u2014 {module}"))
    return path


def write_overview(tables: dict[str, Table], relationships: list[Relationship], database: str) -> str:
    traffic: dict[tuple[str, str], int] = defaultdict(int)
    for relationship in relationships:
        source = tables[relationship.source].module
        target = tables[relationship.target].module
        if source != target:
            traffic[(source, target)] += 1

    present = [m for m in MODULE_ORDER if any(t.module == m for t in tables.values())]

    card_width, card_height, gap = 300.0, 118.0, 26.0
    columns = 4
    width = MARGIN * 2 + columns * card_width + (columns - 1) * gap
    rows = math.ceil(len(present) / columns)
    height = 150 + rows * (card_height + gap) + MARGIN

    centres: dict[str, tuple[float, float]] = {}
    boxes: list[str] = []
    for index, module in enumerate(present):
        column, row = index % columns, index // columns
        x = MARGIN + column * (card_width + gap)
        y = 150.0 + row * (card_height + gap)
        centres[module] = (x + card_width / 2, y + card_height / 2)

        members = [t for t in tables.values() if t.module == module]
        colour = MODULE_COLOURS[module]
        biggest = max(members, key=lambda item: item.rows, default=None)
        card_tooltip = "\n".join(
            [f"{module} modülü", ""]
            + wrap(MODULE_NOTES.get(module, ""), 74)
            + [
                "",
                f"{len(members)} tablo · {thousands(sum(t.rows for t in members))} satır",
            ]
            + (
                [f"En kalabalık tablo: {biggest.name} ({thousands(biggest.rows)} satır)"]
                if biggest is not None and biggest.rows
                else []
            )
        )

        boxes += [
            f'<rect x="{x}" y="{y}" width="{card_width}" height="{card_height}" rx="10" '
            f'fill="#ffffff" stroke="{colour}" stroke-width="1.2" filter="url(#drop)">'
            f"<title>{escape(card_tooltip)}</title></rect>",
            f'<rect x="{x}" y="{y}" width="{card_width}" height="5" rx="2.5" fill="{colour}"/>',
            f'<text class="t" x="{x + 16}" y="{y + 34}" font-size="17" font-weight="700" '
            f'fill="{colour}">{escape(module)}</text>',
            f'<text class="m" x="{x + card_width - 16}" y="{y + 34}" font-size="12" '
            f'fill="#94a3b8" text-anchor="end">{len(members)} '
            f'{"table" if len(members) == 1 else "tables"}</text>',
            f'<text class="m" x="{x + 16}" y="{y + 54}" font-size="11" fill="#64748b">'
            f'{thousands(sum(t.rows for t in members))} rows</text>',
        ]

        caption = MODULE_CAPTIONS.get(module, "")
        for line_index, line in enumerate(wrap(caption, 44)[:3]):
            boxes.append(
                f'<text class="t" x="{x + 16}" y="{y + 74 + line_index * 13}" font-size="10.5" '
                f'fill="#94a3b8">{escape(line)}</text>'
            )

    edges = ['<g id="traffic">']
    for (source, target), count in sorted(traffic.items(), key=lambda item: -item[1]):
        if source not in centres or target not in centres:
            continue
        (x1, y1), (x2, y2) = centres[source], centres[target]
        colour = MODULE_COLOURS[target]
        curve = 0.16 * math.hypot(x2 - x1, y2 - y1)
        midpoint = ((x1 + x2) / 2 + curve * 0.35, (y1 + y2) / 2 - curve * 0.35)

        start = clip_to_card((x1, y1), midpoint, card_width, card_height)
        end = clip_to_card((x2, y2), midpoint, card_width, card_height)

        edges.append(
            f'<path d="M{start[0]:.0f} {start[1]:.0f} Q{midpoint[0]:.0f} {midpoint[1]:.0f} '
            f'{end[0]:.0f} {end[1]:.0f}" '
            f'fill="none" stroke="{colour}" stroke-width="{min(5.0, 0.7 + count * 0.28):.1f}" '
            f'opacity="0.30" marker-end="url(#a-{target.lower()})">'
            f"<title>{escape(source)} \u2192 {escape(target)}\n\n"
            f"{escape(source)} mod\u00fcl\u00fcnde {count} s\u00fctun, {escape(target)} mod\u00fcl\u00fcndeki bir "
            f"tabloya i\u015faret ediyor.</title></path>"
        )
    edges.append("</g>")

    header = [
        f'<text class="t" x="{MARGIN + 4}" y="54" font-size="30" font-weight="700" '
        f'fill="#0f172a">Ensa \u2014 modules</text>',
        f'<text class="t" x="{MARGIN + 4}" y="80" font-size="12.5" fill="#64748b">'
        f'{len(present)} modules \u00b7 {len(tables)} tables \u00b7 '
        f'{thousands(sum(t.rows for t in tables.values()))} rows in '
        f'<tspan class="m">{escape(database)}</tspan></text>',
        f'<text class="t" x="{MARGIN + 4}" y="104" font-size="11" fill="#94a3b8">'
        f'A line runs from the module that holds the key column to the module it points at; '
        f'the thicker the line, the more columns cross that boundary. '
        f'Relationships inside a module are not drawn here.</text>',
        f'<line x1="{MARGIN}" y1="124" x2="{width - MARGIN}" y2="124" stroke="#e2e8f0"/>',
    ]

    path = os.path.join(OUTPUT_DIRECTORY, "ensa-modules.svg")
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(document(width, height, header + edges + boxes, "Ensa modules"))
    return path


def clip_to_card(
    centre: tuple[float, float],
    towards: tuple[float, float],
    width: float,
    height: float,
) -> tuple[float, float]:
    """Walks from a card's centre towards a point and stops at the card's border."""
    dx, dy = towards[0] - centre[0], towards[1] - centre[1]
    if dx == 0 and dy == 0:
        return centre

    scale = min(
        (width / 2) / abs(dx) if dx else float("inf"),
        (height / 2) / abs(dy) if dy else float("inf"),
    )
    return centre[0] + dx * scale, centre[1] + dy * scale


def wrap(text: str, limit: int) -> list[str]:
    lines: list[str] = []
    current = ""
    for word in text.split():
        if len(current) + len(word) + 1 > limit:
            lines.append(current)
            current = word
        else:
            current = f"{current} {word}".strip()
    if current:
        lines.append(current)
    return lines


# ---------------------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser(description="Draws the Ensa database as SVG diagrams.")
    parser.add_argument("--connection", help="connection string; defaults to the application's own")
    arguments = parser.parse_args()

    connection_string = read_connection_string(arguments.connection)
    connection = parse_connection_string(connection_string)
    database = connection.get("database") or connection.get("initialcatalog") or "(unknown)"

    sqlcmd = find_sqlcmd()
    print(f"reading {SCHEMA} of {database} on {connection.get('server')}")

    tables = read_schema(sqlcmd, connection)
    if not tables:
        sys.exit(f"no tables found in schema {SCHEMA}.")

    declared = [tuple(row) for row in run_query(sqlcmd, connection, FOREIGN_KEY_QUERY)]
    relationships = infer_relationships(tables, declared)

    for relationship in relationships:
        tables[relationship.source].outgoing += 1
        tables[relationship.target].incoming += 1

    # A table with no Turkish note would publish an empty tooltip, which is worse than no
    # tooltip: the reader hovers, gets nothing, and stops trusting the rest. Fail instead.
    unexplained = sorted(name for name in tables if name not in NOTES)
    if unexplained:
        sys.exit(
            "these tables have no note in tools/gen-diagram/table_notes_tr.py:\n  "
            + "\n  ".join(unexplained)
        )
    os.makedirs(OUTPUT_DIRECTORY, exist_ok=True)

    written = [
        write_full_diagram(tables, relationships, database),
        write_overview(tables, relationships, database),
    ]
    for module in MODULE_ORDER:
        path = write_module_diagram(module, tables, relationships)
        if path:
            written.append(path)

    print(f"{len(tables)} tables, {len(relationships)} inferred relationships")
    for path in written:
        size = os.path.getsize(path) / 1024
        print(f"  {os.path.relpath(path, REPOSITORY)}  ({size:.0f} KB)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
